using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Refresh;

public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken
);

public sealed class RefreshValidator : AbstractValidator<RefreshRequest>
{
    public RefreshValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public static class RefreshEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/refresh", Handle)
             .WithName("Refresh")
             .WithSummary("Exchange a refresh token for a new access token")
             .Produces<RefreshResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        RefreshRequest request,
        IValidator<RefreshRequest> validator,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        IIdentityUnitOfWork unitOfWork,
        ILogger<RefreshRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var existingToken = await refreshTokenRepository.GetActiveByHashAsync(tokenHash, ct);

        if (existingToken is null)
        {
            logger.LogWarning("Refresh failed: token not found or inactive");

            return Results.Problem(
                detail: "Invalid or expired refresh token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await userRepository.GetByIdAsync(existingToken.UserId, ct);
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Refresh failed: user {UserId} not found or deactivated", existingToken.UserId);

            return Results.Problem(
                detail: "Invalid or expired refresh token.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        existingToken.Revoke();

        var roles = await userRepository.GetUserRolesAsync(user.Id, ct);
        var permissions = await userRepository.GetUserPermissionsAsync(user.Id, ct);

        var newAccessToken = tokenService.GenerateAccessToken(
            user.Id,
            user.Email!,
            roles.Select(r => r.Name).ToList(),
            permissions.Select(p => p.Name).ToList()
        );

        var newRefreshTokenValue = tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = tokenService.HashToken(newRefreshTokenValue);

        var newRefreshToken = RefreshToken.Create(
            user.Id,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(7)
        );

        await refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        var response = new RefreshResponse(newAccessToken, newRefreshTokenValue);

        return Results.Ok(response);
    }
}

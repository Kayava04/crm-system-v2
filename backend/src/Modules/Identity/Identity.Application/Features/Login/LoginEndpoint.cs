using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Login;

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    bool MustChangePassword
);

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public static class LoginEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/login", Handle)
             .WithName("Login")
             .WithSummary("Authenticate a user and issue an access token")
             .Produces<LoginResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status429TooManyRequests)
             .RequireRateLimiting("auth");
    }

    private static async Task<IResult> Handle(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IUserRepository userRepository,
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<LoginRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null)
        {
            logger.LogWarning("Login failed: user with email {Email} not found", request.Email);

            return Results.Problem(
                detail: "Invalid email or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // A locked account is refused before the password is even looked at, so it cannot be guessed while locked
        var lockedFor = await identityService.GetLockoutRemainingAsync(user, ct);
        if (lockedFor is { } remaining)
        {
            logger.LogWarning("Login refused: account {Email} is locked after too many failed attempts", request.Email);

            return Results.Problem(
                detail: $"Too many failed attempts. The account is locked, try again in {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} minute(s).",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var passwordValid = await identityService.CheckPasswordAsync(user, request.Password, ct);
        if (!passwordValid)
        {
            await identityService.RecordFailedLoginAsync(user, ct);
            logger.LogWarning("Login failed: invalid password for {Email}", request.Email);

            return Results.Problem(
                detail: "Invalid email or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        await identityService.RecordSuccessfulLoginAsync(user, ct);

        if (!user.IsActive)
        {
            logger.LogWarning("Login refused: account {Email} is deactivated", request.Email);

            return Results.Problem(
                detail: "Account is deactivated. Contact the administrator.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var roles = await userRepository.GetUserRolesAsync(user.Id, ct);
        var permissions = await userRepository.GetUserPermissionsAsync(user.Id, ct);

        var accessToken = tokenService.GenerateAccessToken(
            user.Id,
            user.Email!,
            roles.Select(r => r.Name).ToList(),
            permissions.Select(p => p.Name).ToList()
        );

        var refreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashToken(refreshTokenValue);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7)
        );

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User {Email} logged in successfully", request.Email);

        var response = new LoginResponse(
            accessToken,
            refreshTokenValue,
            user.MustChangePassword
        );

        return Results.Ok(response);
    }
}

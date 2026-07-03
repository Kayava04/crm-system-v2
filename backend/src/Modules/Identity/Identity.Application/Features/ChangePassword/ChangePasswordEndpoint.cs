using System.Security.Claims;
using FluentValidation;
using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.ChangePassword;

public sealed record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmNewPassword
);

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Old password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .NotEqual(x => x.OldPassword).WithMessage("New password must be different from the old password.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public static class ChangePasswordEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/change-password", Handle)
             .RequireAuthorization()
             .WithName("ChangePassword")
             .WithSummary("Change the current user's password")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Handle(
        ChangePasswordRequest request,
        ClaimsPrincipal user,
        IValidator<ChangePasswordRequest> validator,
        IUserRepository userRepository,
        IIdentityService identityService,
        IIdentityUnitOfWork unitOfWork,
        ILogger<ChangePasswordRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (userId is null || !Guid.TryParse(userId, out var parsedUserId))
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized);

        var existingUser = await userRepository.GetByIdAsync(parsedUserId, ct);
        if (existingUser is null)
            return Results.Problem(
                detail: "User not found.",
                statusCode: StatusCodes.Status401Unauthorized);

        var oldPasswordValid = await identityService.CheckPasswordAsync(
            existingUser,
            request.OldPassword,
            ct
        );

        if (!oldPasswordValid)
        {
            logger.LogWarning("Change password failed: invalid old password for user {UserId}", parsedUserId);

            return Results.Problem(
                detail: "Old password is incorrect.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await identityService.ChangePasswordAsync(
            existingUser,
            request.OldPassword,
            request.NewPassword,
            ct
        );

        existingUser.CompletePasswordChange();

        await userRepository.UpdateAsync(existingUser, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} changed their password", parsedUserId);

        return Results.NoContent();
    }
}

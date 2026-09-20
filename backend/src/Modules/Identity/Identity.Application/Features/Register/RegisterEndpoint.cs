using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Services;
using Identity.Contracts;
using Identity.Contracts.Enums;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notifications.Contracts;
using Shared.Kernel.Abstractions;

namespace Identity.Application.Features.Register;

public sealed record RegisterRequest(
    string Email,
    SystemRole Role,
    List<Guid>? PermissionIds,
    string? ProfileType,
    Guid? ProfileId
);

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string TemporaryPassword
);

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role.")
            .NotEqual(SystemRole.SuperAdmin)
            .WithMessage("SuperAdmin cannot be created through registration.");

        RuleFor(x => x.ProfileId)
            .NotNull()
            .WithMessage("ProfileId is required when ProfileType is specified.")
            .When(x => x.ProfileType is not null);

        RuleFor(x => x.ProfileType)
            .NotNull()
            .WithMessage("ProfileType is required when ProfileId is specified.")
            .When(x => x.ProfileId is not null);
    }
}

public static class RegisterEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/register", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageAdmins))
             .WithName("Register")
             .WithSummary("Create a new user account")
             .Produces<RegisterResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityService identityService,
        IEnumerable<IProfileLinker> profileLinkers,
        IIdentityUnitOfWork unitOfWork,
        ITransactionCoordinator transaction,
        INotificationSender notificationSender,
        ILogger<RegisterRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var emailExists = await userRepository.ExistsByEmailAsync(request.Email, ct);
        if (emailExists)
        {
            logger.LogWarning("Registration failed: user with email {Email} already exists", request.Email);

            return Results.Problem(
                detail: $"User with email '{request.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var role = await roleRepository.GetByNameAsync(request.Role.ToString(), ct);
        if (role is null)
            return Results.Problem(
                detail: $"Role '{request.Role}' not found.",
                statusCode: StatusCodes.Status409Conflict);

        // Resolve the profile linker first so an unknown profile type never leaves a half-created account
        IProfileLinker? linker = null;

        if (request.ProfileType is not null && request.ProfileId is not null)
        {
            linker = profileLinkers.FirstOrDefault(l => l.ProfileType == request.ProfileType);

            if (linker is null)
            {
                logger.LogWarning("No profile linker found for type {ProfileType}", request.ProfileType);

                return Results.Problem(
                    detail: $"Unknown profile type '{request.ProfileType}'.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        User user;

        try
        {
            // The account, its role, permissions and the link to the profile are saved together or not at all
            user = await transaction.ExecuteAsync(async token =>
            {
                var created = await identityService.CreateUserAsync(
                    request.Email, temporaryPassword, mustChangePassword: true, token);

                await userRepository.AssignRoleAsync(created.Id, role.Id, token);

                if (request.Role == SystemRole.Admin && request.PermissionIds is { Count: > 0 })
                {
                    foreach (var permissionId in request.PermissionIds)
                        await userRepository.AssignPermissionAsync(created.Id, permissionId, token);
                }

                if (linker is not null)
                    await linker.LinkAsync(request.ProfileId!.Value, created.Id, token);

                await unitOfWork.SaveChangesAsync(token);

                return created;
            }, ct);
        }
        catch (UserAlreadyExistsException)
        {
            // another request registered the same email at the same moment
            logger.LogWarning("Registration failed: user with email {Email} already exists", request.Email);

            return Results.Problem(
                detail: $"User with email '{request.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (ProfileNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (ProfileAlreadyLinkedException ex)
        {
            logger.LogWarning("Registration refused: {Message}", ex.Message);

            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }

        logger.LogInformation("User {Email} registered with role {Role}", request.Email, request.Role);

        // The account is committed; a failing in-app notification must not take it back
        await PasswordNotifications.SendChangeRequiredAsync(notificationSender, user.Id, logger, ct);

        var response = new RegisterResponse(
            user.Id,
            user.Email!,
            temporaryPassword
        );

        return Results.Created($"/api/users/{user.Id}", response);
    }
}

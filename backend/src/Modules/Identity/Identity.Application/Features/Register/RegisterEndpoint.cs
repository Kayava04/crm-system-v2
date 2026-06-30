using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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

        RuleFor(x => x.PermissionIds)
            .Empty()
            .WithMessage("Permissions can only be assigned when registering an Admin.")
            .When(x => x.Role != SystemRole.Admin);

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
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityService identityService,
        IEnumerable<IProfileLinker> profileLinkers,
        IUnitOfWork unitOfWork,
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

        var temporaryPassword = GenerateTemporaryPassword();

        var user = await identityService.CreateUserAsync(
            request.Email, temporaryPassword, mustChangePassword: true, ct);

        await userRepository.AssignRoleAsync(user.Id, role.Id, ct);

        if (request.Role == SystemRole.Admin && request.PermissionIds is { Count: > 0 })
        {
            foreach (var permissionId in request.PermissionIds)
                await userRepository.AssignPermissionAsync(user.Id, permissionId, ct);
        }

        if (request.ProfileType is not null && request.ProfileId is not null)
        {
            var linker = profileLinkers.FirstOrDefault(l => l.ProfileType == request.ProfileType);

            if (linker is null)
            {
                logger.LogWarning("No profile linker found for type {ProfileType}", request.ProfileType);

                return Results.Problem(
                    detail: $"Unknown profile type '{request.ProfileType}'.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            await linker.LinkAsync(request.ProfileId.Value, user.Id, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User {Email} registered with role {Role}", request.Email, request.Role);

        var response = new RegisterResponse(
            user.Id,
            user.Email!,
            temporaryPassword
        );

        return Results.Created($"/api/users/{user.Id}", response);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";

        var random = Random.Shared;

        var passwordChars = new List<char>
        {
            upper[random.Next(upper.Length)],
            lower[random.Next(lower.Length)],
            digits[random.Next(digits.Length)],
            special[random.Next(special.Length)]
        };

        const string allChars = upper + lower + digits + special;

        passwordChars.AddRange(Enumerable.Range(0, 8)
            .Select(_ => allChars[random.Next(allChars.Length)])
        );

        return new string(passwordChars
            .OrderBy(_ => random.Next())
            .ToArray()
        );
    }
}

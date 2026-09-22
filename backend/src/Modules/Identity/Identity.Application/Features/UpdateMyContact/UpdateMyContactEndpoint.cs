using System.Security.Claims;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Features.Me;
using Identity.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Application.Features.UpdateMyContact;

public sealed record UpdateMyContactRequest(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? MiddleName = null,
    DateOnly? DateOfBirth = null,
    string? City = null,
    string? Country = null
);

public sealed class UpdateMyContactValidator : AbstractValidator<UpdateMyContactRequest>
{
    public UpdateMyContactValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.MiddleName).MaximumLength(100).WithMessage("Middle name must not exceed 100 characters.");
        RuleFor(x => x.City).MaximumLength(100).WithMessage("City must not exceed 100 characters.");
        RuleFor(x => x.Country).MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").WithMessage("Invalid phone number format.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Invalid date of birth.")
            .When(x => x.DateOfBirth is not null);
    }
}

public static class UpdateMyContactEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/me/contact", Handle)
             .RequireAuthorization()
             .WithName("UpdateMyContact")
             .WithSummary("Set the name, birth date, city, country and phone of an administrator or manager (not for the SuperAdmin, students and teachers)")
             .Produces<MeContact>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        UpdateMyContactRequest request,
        ClaimsPrincipal principal,
        IValidator<UpdateMyContactRequest> validator,
        IUserRepository userRepository,
        IEnumerable<IProfileLinker> profileLinkers,
        IIdentityUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return Results.Problem(detail: "User not found or deactivated.", statusCode: StatusCodes.Status401Unauthorized);

        // The SuperAdmin is the single bootstrap account, not a person of the staff: it has no personal details
        var roles = await userRepository.GetUserRolesAsync(userId, ct);
        if (roles.Any(r => r.Name == nameof(SystemRole.SuperAdmin)))
            return Results.Problem(
                detail: "The SuperAdmin account is a system account and has no personal details.",
                statusCode: StatusCodes.Status403Forbidden);

        // A student or a teacher keeps the personal details in their own record; two copies would drift apart
        foreach (var linker in profileLinkers)
        {
            if (await linker.FindByUserAsync(userId, ct) is not null)
                return Results.Problem(
                    detail: $"Your personal details are kept in your {linker.ProfileType.ToLowerInvariant()} record and are changed there.",
                    statusCode: StatusCodes.Status409Conflict);
        }

        user.SetContact(
            request.FirstName, request.LastName, request.PhoneNumber,
            request.MiddleName, request.DateOfBirth, request.City, request.Country);
        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Results.Ok(MeEndpoint.ToContact(user));
    }
}

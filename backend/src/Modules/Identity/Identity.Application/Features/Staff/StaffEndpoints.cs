using System.Security.Claims;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Features.Me;
using Identity.Contracts.Enums;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;

namespace Identity.Application.Features.Staff;

public sealed record StaffMember(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? City,
    string? Country,
    decimal? Salary,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Permissions
);

public sealed record SetStaffStatusRequest(bool IsActive);

public sealed record SetStaffPermissionsRequest(List<Guid>? PermissionIds);

public sealed record SetStaffSalaryRequest(decimal? Salary);

public sealed class SetStaffPermissionsValidator : AbstractValidator<SetStaffPermissionsRequest>
{
    public SetStaffPermissionsValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("PermissionIds is required (an empty list removes all permissions).");
    }
}

public sealed class SetStaffSalaryValidator : AbstractValidator<SetStaffSalaryRequest>
{
    public SetStaffSalaryValidator()
    {
        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("Salary cannot be negative.")
            .When(x => x.Salary is not null);
    }
}

// Administrators and managers are the accounts with the Admin role. The SuperAdmin is the single bootstrap account and is
// never listed or changed here; students and teachers are managed through their own modules.
public static class StaffEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/users", ListStaff)
             .RequireAuthorization(nameof(SystemPermission.CanManageAdmins))
             .WithName("ListStaff")
             .WithSummary("List administrator and manager accounts (optionally only active or only deactivated ones)")
             .Produces<List<StaffMember>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/users/{id:guid}/status", SetStatus)
             .RequireAuthorization(nameof(SystemPermission.CanManageAdmins))
             .WithName("SetStaffStatus")
             .WithSummary("Deactivate or reactivate an administrator account; nothing is deleted")
             .Produces<StaffMember>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/users/{id:guid}/permissions", SetPermissions)
             .RequireAuthorization(nameof(SystemPermission.CanManageAdmins))
             .WithName("SetStaffPermissions")
             .WithSummary("Replace the permissions of an administrator account")
             .Produces<StaffMember>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/users/{id:guid}/salary", SetSalary)
             .RequireAuthorization(nameof(SystemPermission.CanManageAdmins))
             .WithName("SetStaffSalary")
             .WithSummary("Set or clear the salary of an administrator account; never self-service")
             .Produces<StaffMember>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<StaffMember> ToMemberAsync(User user, IUserRepository users, CancellationToken ct)
    {
        var permissions = await users.GetUserPermissionsAsync(user.Id, ct);
        var contact = MeEndpoint.ToContact(user);

        return new StaffMember(
            user.Id, user.Email!, contact.FirstName, contact.LastName, contact.MiddleName, contact.FullName, contact.PhoneNumber,
            contact.DateOfBirth, contact.City, contact.Country, contact.Salary,
            user.IsActive, user.CreatedAt, permissions.Select(p => p.Name).Order().ToList());
    }

    private static async Task<IResult> ListStaff(bool? isActive, IUserRepository users, CancellationToken ct)
    {
        var accounts = await users.ListByRoleAsync(nameof(SystemRole.Admin), ct);

        var result = new List<StaffMember>();
        foreach (var account in accounts.Where(a => isActive is null || a.IsActive == isActive))
            result.Add(await ToMemberAsync(account, users, ct));

        return Results.Ok(result);
    }

    // Returns the account to change, or the problem to answer with
    private static async Task<(User? User, IResult? Problem)> FindManageableAsync(
        Guid id, ClaimsPrincipal principal, IUserRepository users, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
            return (null, Results.Problem(detail: $"User with id '{id}' not found.", statusCode: StatusCodes.Status404NotFound));

        var roles = (await users.GetUserRolesAsync(id, ct)).Select(r => r.Name).ToList();

        if (roles.Contains(nameof(SystemRole.SuperAdmin)))
            return (null, Results.Problem(detail: "The SuperAdmin account cannot be changed.", statusCode: StatusCodes.Status403Forbidden));

        if (!roles.Contains(nameof(SystemRole.Admin)))
            return (null, Results.Problem(
                detail: "Only administrator accounts are managed here; students and teachers are managed through their own records.",
                statusCode: StatusCodes.Status409Conflict));

        var caller = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (Guid.TryParse(caller, out var callerId) && callerId == id)
            return (null, Results.Problem(detail: "You cannot change your own account here.", statusCode: StatusCodes.Status409Conflict));

        return (user, null);
    }

    private static async Task<IResult> SetStatus(
        Guid id,
        SetStaffStatusRequest request,
        ClaimsPrincipal principal,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IIdentityUnitOfWork unitOfWork,
        ITransactionCoordinator transaction,
        ILogger<SetStaffStatusRequest> logger,
        CancellationToken ct)
    {
        var (user, problem) = await FindManageableAsync(id, principal, users, ct);
        if (problem is not null)
            return problem;

        await transaction.ExecuteAsync(async token =>
        {
            user!.SetActive(request.IsActive);
            await users.UpdateAsync(user, token);

            // A deactivated account must not keep working through a refresh token
            if (!request.IsActive)
                await refreshTokens.RevokeAllForUserAsync(id, token);

            await unitOfWork.SaveChangesAsync(token);
        }, ct);

        logger.LogInformation("Administrator {UserId} {State}", id, request.IsActive ? "reactivated" : "deactivated");

        return Results.Ok(await ToMemberAsync(user!, users, ct));
    }

    private static async Task<IResult> SetPermissions(
        Guid id,
        SetStaffPermissionsRequest request,
        ClaimsPrincipal principal,
        IValidator<SetStaffPermissionsRequest> validator,
        IUserRepository users,
        IPermissionRepository permissions,
        IIdentityUnitOfWork unitOfWork,
        ILogger<SetStaffPermissionsRequest> logger,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var (user, problem) = await FindManageableAsync(id, principal, users, ct);
        if (problem is not null)
            return problem;

        var wanted = request.PermissionIds!.Distinct().ToList();
        var known = (await permissions.GetAllAsync(ct)).Select(p => p.Id).ToHashSet();
        var unknown = wanted.Where(w => !known.Contains(w)).ToList();
        if (unknown.Count > 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["permissionIds"] = [$"Unknown permission ids: {string.Join(", ", unknown)}."]
            });

        await users.ReplaceDirectPermissionsAsync(id, wanted, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Permissions of administrator {UserId} replaced", id);

        return Results.Ok(await ToMemberAsync(user!, users, ct));
    }

    private static async Task<IResult> SetSalary(
        Guid id,
        SetStaffSalaryRequest request,
        ClaimsPrincipal principal,
        IValidator<SetStaffSalaryRequest> validator,
        IUserRepository users,
        IIdentityUnitOfWork unitOfWork,
        ILogger<SetStaffSalaryRequest> logger,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var (user, problem) = await FindManageableAsync(id, principal, users, ct);
        if (problem is not null)
            return problem;

        user!.SetSalary(request.Salary);
        await users.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Salary of administrator {UserId} changed", id);

        return Results.Ok(await ToMemberAsync(user, users, ct));
    }
}

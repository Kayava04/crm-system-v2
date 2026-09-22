using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Contracts.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Seeding;

public sealed class IdentitySeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IIdentityUnitOfWork>();

        await SeedPermissionsAsync(permissionRepository, unitOfWork, ct);
        await SeedRolesAsync(roleRepository, permissionRepository, unitOfWork, ct);
        await SeedSuperAdminAsync(userRepository, roleRepository, identityService, unitOfWork, ct);

        logger.LogInformation("Identity seeding completed");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static async Task SeedPermissionsAsync(
        IPermissionRepository permissionRepository,
        IIdentityUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        var hasNewPermissions = false;

        foreach (var permission in Enum.GetValues<SystemPermission>())
        {
            var permissionName = permission.ToString();
            var existing = await permissionRepository.GetByNameAsync(permissionName, ct);

            if (existing is not null)
                continue;

            var entity = Permission.Create(permissionName);
            await permissionRepository.AddAsync(entity, ct);

            hasNewPermissions = true;
        }

        if (hasNewPermissions)
            await unitOfWork.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAsync(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IIdentityUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        foreach (var role in Enum.GetValues<SystemRole>())
        {
            var roleName = role.ToString();
            var existing = await roleRepository.GetByNameAsync(roleName, ct);

            if (existing is null)
            {
                var entity = Role.Create(roleName);
                await roleRepository.AddAsync(entity, ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        // SuperAdmin get all permissions
        var superAdminRole = await roleRepository.GetByNameAsync(nameof(SystemRole.SuperAdmin), ct);
        if (superAdminRole is not null)
        {
            var existingPermissionIds = (await roleRepository.GetRolePermissionIdsAsync(superAdminRole.Id, ct))
                .ToHashSet();

            foreach (var permission in Enum.GetValues<SystemPermission>())
            {
                var permissionEntity = await permissionRepository.GetByNameAsync(permission.ToString(), ct);

                if (permissionEntity is not null && !existingPermissionIds.Contains(permissionEntity.Id))
                    await roleRepository.AssignPermissionAsync(superAdminRole.Id, permissionEntity.Id, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }

        await SeedDefaultRolePermissionsAsync(roleRepository, permissionRepository, unitOfWork, ct);
    }

    // Permissions every user of a role gets by default (additive, safe to run on each start)
    private static readonly Dictionary<SystemRole, SystemPermission[]> DefaultRolePermissions = new()
    {
        // CanViewCourses: teachers pick the course when creating a material (see CreateMaterialDialog)
        [SystemRole.Teacher] = [SystemPermission.CanViewMaterials, SystemPermission.CanManageMaterials, SystemPermission.CanViewCourses],
        [SystemRole.Student] = [SystemPermission.CanViewMaterials]
    };

    private static async Task SeedDefaultRolePermissionsAsync(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IIdentityUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        foreach (var (systemRole, permissions) in DefaultRolePermissions)
        {
            var role = await roleRepository.GetByNameAsync(systemRole.ToString(), ct);
            if (role is null)
                continue;

            var existingPermissionIds = (await roleRepository.GetRolePermissionIdsAsync(role.Id, ct))
                .ToHashSet();

            foreach (var permission in permissions)
            {
                var permissionEntity = await permissionRepository.GetByNameAsync(permission.ToString(), ct);

                if (permissionEntity is not null && !existingPermissionIds.Contains(permissionEntity.Id))
                    await roleRepository.AssignPermissionAsync(role.Id, permissionEntity.Id, ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task SeedSuperAdminAsync(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IIdentityService identityService,
        IIdentityUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        var email = configuration["SuperAdmin:Email"]
            ?? throw new InvalidOperationException("SuperAdmin:Email not found in configuration.");
        var password = configuration["SuperAdmin:Password"]
            ?? throw new InvalidOperationException("SuperAdmin:Password not found in configuration.");

        var existingUser = await userRepository.GetByEmailAsync(email, ct);
        if (existingUser is not null)
            return;

        var createdUser = await identityService.CreateUserAsync(email, password, mustChangePassword: false, ct);

        var superAdminRole = await roleRepository.GetByNameAsync(nameof(SystemRole.SuperAdmin), ct);
        if (superAdminRole is null)
            throw new InvalidOperationException("SuperAdmin role not found. Seed roles first.");

        await userRepository.AssignRoleAsync(createdUser.Id, superAdminRole.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("SuperAdmin account created: {Email}", email);
    }
}

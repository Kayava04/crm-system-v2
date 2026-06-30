namespace Identity.Domain.Entities;

public class UserPermission
{
    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserPermission() { }

    public static UserPermission Create(Guid userId, Guid permissionId)
    {
        return new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow
        };
    }
}

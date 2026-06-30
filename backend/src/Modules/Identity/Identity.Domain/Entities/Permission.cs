using Shared.Kernel.Primitives;

namespace Identity.Domain.Entities;

public class Permission : Entity
{
    public string Name { get; private set; } = string.Empty;

    private Permission() { }

    public static Permission Create(string name)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}

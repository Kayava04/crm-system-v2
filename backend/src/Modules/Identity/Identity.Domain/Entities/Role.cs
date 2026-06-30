using Shared.Kernel.Primitives;

namespace Identity.Domain.Entities;

public sealed class Role : Entity
{
    public string Name { get; private set; } = string.Empty;

    private Role() { }

    public static Role Create(string name)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name
        };
    }
}

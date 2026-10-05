namespace Identity.Contracts;

public interface IProfileLinker
{
    string ProfileType { get; }
    Task LinkAsync(Guid profileId, Guid userId, CancellationToken ct = default);

    Task<LinkedProfile?> FindByUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed record LinkedProfile(string Type, Guid Id, string FullName);

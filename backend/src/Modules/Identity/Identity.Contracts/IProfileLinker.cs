namespace Identity.Contracts;

public interface IProfileLinker
{
    string ProfileType { get; }
    Task LinkAsync(Guid profileId, Guid userId, CancellationToken ct = default);

    // The profile this account belongs to, if it is a profile of this module's type
    Task<LinkedProfile?> FindByUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed record LinkedProfile(string Type, Guid Id, string FullName);

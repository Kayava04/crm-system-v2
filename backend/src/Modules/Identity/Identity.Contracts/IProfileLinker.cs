namespace Identity.Contracts;

public interface IProfileLinker
{
    string ProfileType { get; }
    Task LinkAsync(Guid profileId, Guid userId, CancellationToken ct = default);
}

namespace Identity.Contracts;

public interface IUserAccountManager
{
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}

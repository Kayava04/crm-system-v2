namespace Identity.Contracts;

// Lets other modules switch a person's login on or off without knowing anything about Identity internals
public interface IUserAccountManager
{
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}

using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IUserPhotoRepository
{
    Task<UserPhoto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserPhoto photo, CancellationToken ct = default);
    Task UpdateAsync(UserPhoto photo, CancellationToken ct = default);
    Task DeleteAsync(UserPhoto photo, CancellationToken ct = default);
}

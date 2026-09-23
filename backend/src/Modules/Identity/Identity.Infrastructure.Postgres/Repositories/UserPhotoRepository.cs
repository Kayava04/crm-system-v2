using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Postgres.Repositories;

internal sealed class UserPhotoRepository(IdentityDbContext context) : IUserPhotoRepository
{
    public async Task<UserPhoto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.UserPhotos.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.UserPhotos.AsNoTracking().AnyAsync(p => p.UserId == userId, ct);

    public async Task AddAsync(UserPhoto photo, CancellationToken ct = default) =>
        await context.UserPhotos.AddAsync(photo, ct);

    public Task UpdateAsync(UserPhoto photo, CancellationToken ct = default)
    {
        context.UserPhotos.Update(photo);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserPhoto photo, CancellationToken ct = default)
    {
        context.UserPhotos.Remove(photo);
        return Task.CompletedTask;
    }
}

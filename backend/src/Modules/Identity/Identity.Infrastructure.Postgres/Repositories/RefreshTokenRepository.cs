using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Postgres.Repositories;

internal sealed class RefreshTokenRepository(IdentityDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default) =>
        await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash
                && rt.RevokedAt == null
                && rt.ExpiresAt > DateTime.UtcNow, ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await context.RefreshTokens.AddAsync(refreshToken, ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.Revoke();
    }
}

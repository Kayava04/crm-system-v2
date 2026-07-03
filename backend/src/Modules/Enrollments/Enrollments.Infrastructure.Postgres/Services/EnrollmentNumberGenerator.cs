using Enrollments.Application.Abstractions;
using Enrollments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Postgres.Services;

internal sealed class EnrollmentNumberGenerator(EnrollmentsDbContext context)
    : IEnrollmentNumberGenerator
{
    public async Task<string> GenerateAsync(DateOnly signDate, CancellationToken ct = default)
    {
        var datePart = signDate.ToString("yyyyMMdd");
        var prefix = $"CTR-{datePart}";

        var existingNumbers = await context.Enrollments
            .AsNoTracking()
            .Where(e => e.EnrollmentNumber.StartsWith(prefix))
            .Select(e => e.EnrollmentNumber)
            .ToListAsync(ct);

        var lastSequence = existingNumbers
            .Select(n =>
            {
                var parts = n.Split('-');
                return parts.Length == 3 && int.TryParse(parts[2], out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}-{lastSequence + 1:D4}";
    }
}

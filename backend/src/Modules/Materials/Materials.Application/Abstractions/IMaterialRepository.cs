using Materials.Domain.Entities;
using Materials.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Materials.Application.Abstractions;

public interface IMaterialRepository : IRepository<Material>
{
    Task<(IReadOnlyList<Material> Materials, int TotalCount)> GetAllAsync(
        Guid? courseId,
        MaterialType? type,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}

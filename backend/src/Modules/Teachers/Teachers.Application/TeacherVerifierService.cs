using Teachers.Application.Abstractions;
using Teachers.Contracts;

namespace Teachers.Application;

internal sealed class TeacherVerifierService(ITeacherRepository repository) : ITeacherVerifier
{
    public async Task<bool> ExistsAsync(Guid teacherId, CancellationToken ct = default) =>
        await repository.ExistsByIdAsync(teacherId, ct);

    public async Task<bool> IsAvailableAsync(Guid teacherId, CancellationToken ct = default) =>
        await repository.IsAvailableByIdAsync(teacherId, ct);
}

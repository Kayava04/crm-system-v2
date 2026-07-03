using Students.Application.Abstractions;
using Students.Contracts;

namespace Students.Application;

internal sealed class StudentVerifierService(IStudentRepository repository) : IStudentVerifier
{
    public async Task<bool> ExistsAsync(Guid studentId, CancellationToken ct = default) =>
        await repository.ExistsByIdAsync(studentId, ct);
}

using Identity.Contracts;
using Students.Application.Abstractions;

namespace Students.Application;

internal sealed class StudentAccountLinker(
    IStudentRepository repository,
    IStudentUnitOfWork unitOfWork
) : IProfileLinker
{
    public string ProfileType => "Student";

    public async Task LinkAsync(Guid profileId, Guid userId, CancellationToken ct = default)
    {
        var student = await repository.GetByIdAsync(profileId, ct);

        if (student is null)
            throw new InvalidOperationException($"Student with id '{profileId}' not found.");

        student.LinkUserAccount(userId);

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

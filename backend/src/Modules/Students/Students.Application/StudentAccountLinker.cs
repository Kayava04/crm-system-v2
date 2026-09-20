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
            throw new ProfileNotFoundException($"Student with id '{profileId}' not found.");

        if (student.UserId is not null)
            throw new ProfileAlreadyLinkedException($"Student with id '{profileId}' already has an account.");

        student.LinkUserAccount(userId);

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

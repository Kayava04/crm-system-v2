using Identity.Contracts;
using Teachers.Application.Abstractions;

namespace Teachers.Application;

internal sealed class TeacherAccountLinker(
    ITeacherRepository repository,
    ITeacherUnitOfWork unitOfWork
) : IProfileLinker
{
    public string ProfileType => "Teacher";

    public async Task LinkAsync(Guid profileId, Guid userId, CancellationToken ct = default)
    {
        var teacher = await repository.GetByIdAsync(profileId, ct);

        if (teacher is null)
            throw new InvalidOperationException($"Teacher with id '{profileId}' not found.");

        teacher.LinkUserAccount(userId);

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

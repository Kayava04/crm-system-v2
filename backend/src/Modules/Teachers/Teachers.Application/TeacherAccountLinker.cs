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
            throw new ProfileNotFoundException($"Teacher with id '{profileId}' not found.");

        if (teacher.UserId is not null)
            throw new ProfileAlreadyLinkedException($"Teacher with id '{profileId}' already has an account.");

        teacher.LinkUserAccount(userId);

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

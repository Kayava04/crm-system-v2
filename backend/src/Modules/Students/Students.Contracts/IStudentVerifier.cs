namespace Students.Contracts;

public interface IStudentVerifier
{
    Task<bool> ExistsAsync(Guid studentId, CancellationToken ct = default);

    // Only an active student can be enrolled or have an enrollment activated
    Task<bool> IsActiveAsync(Guid studentId, CancellationToken ct = default);
}

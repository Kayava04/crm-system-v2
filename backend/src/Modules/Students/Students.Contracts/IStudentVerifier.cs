namespace Students.Contracts;

public interface IStudentVerifier
{
    Task<bool> ExistsAsync(Guid studentId, CancellationToken ct = default);

    Task<bool> IsActiveAsync(Guid studentId, CancellationToken ct = default);
}

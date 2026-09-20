namespace Teachers.Contracts;

public interface ITeacherVerifier
{
    Task<bool> ExistsAsync(Guid teacherId, CancellationToken ct = default);
}

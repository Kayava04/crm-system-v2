namespace Teachers.Contracts;

public interface ITeacherVerifier
{
    Task<bool> ExistsAsync(Guid teacherId, CancellationToken ct = default);

    // A teacher can be given lessons only while employed (or on probation); not on leave, resigned or dismissed
    Task<bool> IsAvailableAsync(Guid teacherId, CancellationToken ct = default);
}

namespace Enrollments.Application.Abstractions;

public interface IEnrollmentNumberGenerator
{
    Task<string> GenerateAsync(DateOnly signDate, CancellationToken ct = default);
}

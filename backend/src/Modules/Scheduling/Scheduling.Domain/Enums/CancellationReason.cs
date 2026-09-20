namespace Scheduling.Domain.Enums;

// Why a lesson is cancelled. Only system reasons can be undone automatically when the person comes back.
public enum CancellationReason
{
    None,
    Manual,
    TeacherUnavailable,
    EnrollmentInactive
}

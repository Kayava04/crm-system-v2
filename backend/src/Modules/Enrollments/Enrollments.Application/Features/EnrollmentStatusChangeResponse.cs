namespace Enrollments.Application.Features;

public sealed record EnrollmentStatusChangeResponse(
    int CancelledLessons,
    int RestoredLessons,
    int SkippedLessons,
    int LessonsLeftToSchedule
);

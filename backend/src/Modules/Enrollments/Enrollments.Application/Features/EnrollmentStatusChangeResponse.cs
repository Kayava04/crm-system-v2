namespace Enrollments.Application.Features;

// Tells the client what a status change did to the calendar
public sealed record EnrollmentStatusChangeResponse(
    int CancelledLessons,
    int RestoredLessons,
    int SkippedLessons,
    int LessonsLeftToSchedule
);

using Scheduling.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Scheduling.Domain.Entities;

public sealed class Schedule : AuditableEntity
{
    public Guid? EnrollmentId { get; private set; }
    public Guid? GroupId { get; private set; }
    public Guid TeacherId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public int DurationMinutes { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public CancellationReason CancellationReason { get; private set; }
    public string? Notes { get; private set; }

    public DateTime EndDate => ScheduledDate.AddMinutes(DurationMinutes);

    public bool IsOpen => Status is ScheduleStatus.Scheduled or ScheduleStatus.Rescheduled;

    private Schedule() { }

    public static Schedule CreateForEnrollment(
        Guid enrollmentId,
        Guid teacherId,
        DateTime scheduledDate,
        int durationMinutes,
        string? notes = null
    ) => Create(enrollmentId, null, teacherId, scheduledDate, durationMinutes, notes);

    public static Schedule CreateForGroup(
        Guid groupId,
        Guid teacherId,
        DateTime scheduledDate,
        int durationMinutes,
        string? notes = null
    ) => Create(null, groupId, teacherId, scheduledDate, durationMinutes, notes);

    private static Schedule Create(
        Guid? enrollmentId,
        Guid? groupId,
        Guid teacherId,
        DateTime scheduledDate,
        int durationMinutes,
        string? notes
    )
    {
        return new Schedule
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            GroupId = groupId,
            TeacherId = teacherId,
            ScheduledDate = NormalizeToUtc(scheduledDate),
            DurationMinutes = durationMinutes,
            Status = ScheduleStatus.Scheduled,
            CancellationReason = CancellationReason.None,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(Guid teacherId, int durationMinutes, string? notes)
    {
        TeacherId = teacherId;
        DurationMinutes = durationMinutes;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ScheduleStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(CancellationReason reason = CancellationReason.Manual)
    {
        Status = ScheduleStatus.Cancelled;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool RestoreIfSystemCancelled()
    {
        if (Status != ScheduleStatus.Cancelled
            || CancellationReason is not (CancellationReason.TeacherUnavailable or CancellationReason.EnrollmentInactive))
            return false;

        Status = ScheduleStatus.Scheduled;
        CancellationReason = CancellationReason.None;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    public void ReassignTeacher(Guid teacherId)
    {
        TeacherId = teacherId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newDate)
    {
        ScheduledDate = NormalizeToUtc(newDate);
        Status = ScheduleStatus.Rescheduled;
        UpdatedAt = DateTime.UtcNow;
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}

using Scheduling.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Scheduling.Domain.Entities;

public sealed class Schedule : AuditableEntity
{
    public Guid EnrollmentId { get; private set; }
    public Guid TeacherId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public int DurationMinutes { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public DateTime EndDate => ScheduledDate.AddMinutes(DurationMinutes);

    // Scheduled and Rescheduled lessons are still to be held; Completed and Cancelled are final
    public bool IsOpen => Status is ScheduleStatus.Scheduled or ScheduleStatus.Rescheduled;

    private Schedule() { }

    public static Schedule Create(
        Guid enrollmentId,
        Guid teacherId,
        DateTime scheduledDate,
        int durationMinutes,
        string? notes = null
    )
    {
        return new Schedule
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            TeacherId = teacherId,
            ScheduledDate = NormalizeToUtc(scheduledDate),
            DurationMinutes = durationMinutes,
            Status = ScheduleStatus.Scheduled,
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

    public void Cancel()
    {
        Status = ScheduleStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newDate)
    {
        ScheduledDate = NormalizeToUtc(newDate);
        Status = ScheduleStatus.Rescheduled;
        UpdatedAt = DateTime.UtcNow;
    }

    // Npgsql only accepts UTC values for timestamptz columns
    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}

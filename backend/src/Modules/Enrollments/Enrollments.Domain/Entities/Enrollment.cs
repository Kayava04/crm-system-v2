using Enrollments.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Enrollments.Domain.Entities;

public sealed class Enrollment : AuditableEntity
{
    public string EnrollmentNumber { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal CoursePrice { get; private set; }
    public decimal? DiscountedPrice { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public string? Comment { get; private set; }

    // Free text: when the student can attend (e.g. "Tue/Thu after 18:00"); used by an admin to build the schedule
    public string? PreferredSchedule { get; private set; }

    public decimal EffectivePrice => DiscountedPrice ?? CoursePrice;

    private Enrollment() { }

    public static Enrollment Create(
        string enrollmentNumber,
        Guid studentId,
        Guid courseId,
        DateOnly startDate,
        int durationMonths,
        decimal coursePrice,
        decimal? discountedPrice = null,
        string? comment = null,
        string? preferredSchedule = null
    )
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            EnrollmentNumber = enrollmentNumber,
            StudentId = studentId,
            CourseId = courseId,
            StartDate = startDate,
            EndDate = startDate.AddMonths(durationMonths),
            CoursePrice = coursePrice,
            DiscountedPrice = discountedPrice,
            Status = EnrollmentStatus.Draft,
            Comment = comment,
            PreferredSchedule = preferredSchedule,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        Status = EnrollmentStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = EnrollmentStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = EnrollmentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Terminate()
    {
        Status = EnrollmentStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDiscount(decimal discountedPrice)
    {
        DiscountedPrice = discountedPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDiscount()
    {
        DiscountedPrice = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateComment(string? comment)
    {
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferredSchedule(string? preferredSchedule)
    {
        PreferredSchedule = preferredSchedule;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newCoursePrice)
    {
        CoursePrice = newCoursePrice;
        DiscountedPrice = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

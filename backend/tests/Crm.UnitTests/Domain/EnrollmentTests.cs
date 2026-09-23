using Enrollments.Domain.Entities;
using Enrollments.Domain.Enums;

namespace Crm.UnitTests.Domain;

public class EnrollmentTests
{
    private static Enrollment NewEnrollment(decimal? discount = null) =>
        Enrollment.Create("CTR-20300101-0001", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2030, 1, 31), 3, 3000m, discount);

    [Fact]
    public void Create_starts_as_draft_and_calculates_the_end_date_from_the_course_duration()
    {
        var enrollment = NewEnrollment();

        Assert.Equal(EnrollmentStatus.Draft, enrollment.Status);
        Assert.Equal(new DateOnly(2030, 4, 30), enrollment.EndDate);   // 31 Jan + 3 months
        Assert.False(enrollment.AutoSuspended);
    }

    [Fact]
    public void Effective_price_is_the_discount_when_there_is_one()
    {
        Assert.Equal(3000m, NewEnrollment().EffectivePrice);
        Assert.Equal(2500m, NewEnrollment(2500m).EffectivePrice);
    }

    [Fact]
    public void ApplyDiscount_and_RemoveDiscount_change_the_effective_price()
    {
        var enrollment = NewEnrollment();

        enrollment.ApplyDiscount(2000m);
        Assert.Equal(2000m, enrollment.EffectivePrice);

        enrollment.RemoveDiscount();
        Assert.Equal(3000m, enrollment.EffectivePrice);
    }

    [Fact]
    public void UpdatePrice_drops_the_discount()
    {
        var enrollment = NewEnrollment(2500m);

        enrollment.UpdatePrice(3500m);

        Assert.Equal(3500m, enrollment.CoursePrice);
        Assert.Null(enrollment.DiscountedPrice);
    }

    [Fact]
    public void Suspend_by_the_system_is_remembered_and_cleared_on_activation()
    {
        var enrollment = NewEnrollment();
        enrollment.Activate();

        enrollment.Suspend(bySystem: true);
        Assert.Equal(EnrollmentStatus.Suspended, enrollment.Status);
        Assert.True(enrollment.AutoSuspended);

        enrollment.Activate();
        Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
        Assert.False(enrollment.AutoSuspended);
    }

    [Fact]
    public void Manual_suspend_is_not_marked_as_automatic()
    {
        var enrollment = NewEnrollment();
        enrollment.Activate();

        enrollment.Suspend();

        Assert.False(enrollment.AutoSuspended);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Completed)]
    [InlineData(EnrollmentStatus.Terminated)]
    public void Final_statuses_clear_the_automatic_flag(EnrollmentStatus status)
    {
        var enrollment = NewEnrollment();
        enrollment.Activate();
        enrollment.Suspend(bySystem: true);

        if (status == EnrollmentStatus.Completed) enrollment.Complete(); else enrollment.Terminate();

        Assert.Equal(status, enrollment.Status);
        Assert.False(enrollment.AutoSuspended);
    }

    [Fact]
    public void Preferred_schedule_can_be_set_and_changed()
    {
        var enrollment = Enrollment.Create("CTR-1", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2030, 1, 1), 1, 100m,
            preferredSchedule: "Tue/Thu after 18:00");
        Assert.Equal("Tue/Thu after 18:00", enrollment.PreferredSchedule);

        enrollment.UpdatePreferredSchedule("Mon 10:00");
        Assert.Equal("Mon 10:00", enrollment.PreferredSchedule);
    }
}

using Scheduling.Application.Features.CreateSchedule;
using Scheduling.Application.Features.GenerateSchedule;
using Scheduling.Application.Features.ReassignTeacher;

namespace Crm.UnitTests.Validators;

public class SchedulingValidatorTests
{
    private static readonly DateTime Future = DateTime.UtcNow.AddDays(3);

    private static CreateScheduleRequest Lesson(Guid? enrollment = null, Guid? group = null, DateTime? date = null, int minutes = 60, string? notes = null) =>
        new(enrollment, group, Guid.NewGuid(), date ?? Future, minutes, notes);

    // ---- create lesson
    [Fact]
    public void Lesson_for_an_enrollment_is_valid()
    {
        Assert.True(new CreateScheduleValidator().Validate(Lesson(enrollment: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Lesson_for_a_group_is_valid()
    {
        Assert.True(new CreateScheduleValidator().Validate(Lesson(group: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Lesson_needs_exactly_one_target()
    {
        var validator = new CreateScheduleValidator();

        Assert.False(validator.Validate(Lesson()).IsValid);
        Assert.False(validator.Validate(Lesson(enrollment: Guid.NewGuid(), group: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void An_empty_enrollment_or_group_id_is_rejected()
    {
        var validator = new CreateScheduleValidator();

        Assert.False(validator.Validate(Lesson(enrollment: Guid.Empty)).IsValid);
        Assert.False(validator.Validate(Lesson(group: Guid.Empty)).IsValid);
    }

    [Fact]
    public void Lesson_cannot_be_in_the_past()
    {
        var result = new CreateScheduleValidator().Validate(Lesson(enrollment: Guid.NewGuid(), date: DateTime.UtcNow.AddHours(-1)));

        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("future"));
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(480, true)]
    [InlineData(481, false)]
    public void Lesson_duration_is_between_15_and_480_minutes(int minutes, bool valid)
    {
        Assert.Equal(valid, new CreateScheduleValidator().Validate(Lesson(enrollment: Guid.NewGuid(), minutes: minutes)).IsValid);
    }

    [Fact]
    public void Lesson_notes_are_limited_to_500_characters()
    {
        var validator = new CreateScheduleValidator();

        Assert.True(validator.Validate(Lesson(enrollment: Guid.NewGuid(), notes: new string('x', 500))).IsValid);
        Assert.False(validator.Validate(Lesson(enrollment: Guid.NewGuid(), notes: new string('x', 501))).IsValid);
    }

    // ---- generate schedule
    private static readonly LessonSlot Tuesday = new(DayOfWeek.Tuesday, "18:00");

    private static GenerateScheduleRequest Generate(
        Guid? enrollment = null, Guid? group = null, Guid? teacher = null,
        List<LessonSlot>? slots = null, int minutes = 60, int? lessons = null) =>
        new(enrollment, group, teacher, slots ?? [Tuesday], minutes, null, lessons);

    [Fact]
    public void Generation_for_an_enrollment_needs_a_teacher()
    {
        var validator = new GenerateScheduleValidator();

        Assert.False(validator.Validate(Generate(enrollment: Guid.NewGuid())).IsValid);
        Assert.True(validator.Validate(Generate(enrollment: Guid.NewGuid(), teacher: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Generation_for_a_group_does_not_need_a_teacher()
    {
        Assert.True(new GenerateScheduleValidator().Validate(Generate(group: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Generation_for_an_enrollment_rejects_an_empty_teacher_id()
    {
        Assert.False(
            new GenerateScheduleValidator()
                .Validate(Generate(enrollment: Guid.NewGuid(), teacher: Guid.Empty))
                .IsValid
        );
    }

    [Fact]
    public void Generation_needs_exactly_one_target()
    {
        var validator = new GenerateScheduleValidator();

        Assert.False(validator.Validate(Generate(teacher: Guid.NewGuid())).IsValid);
        Assert.False(validator.Validate(Generate(enrollment: Guid.NewGuid(), group: Guid.NewGuid(), teacher: Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Generation_needs_at_least_one_slot()
    {
        Assert.False(new GenerateScheduleValidator().Validate(Generate(group: Guid.NewGuid(), slots: [])).IsValid);
    }

    [Fact]
    public void A_weekday_can_appear_only_once()
    {
        var slots = new List<LessonSlot> { new(DayOfWeek.Monday, "10:00"), new(DayOfWeek.Monday, "12:00") };

        Assert.False(new GenerateScheduleValidator().Validate(Generate(group: Guid.NewGuid(), slots: slots)).IsValid);
    }

    [Theory]
    [InlineData("18:00", true)]
    [InlineData("00:00", true)]
    [InlineData("23:59", true)]
    [InlineData("24:00", false)]
    [InlineData("25:99", false)]
    [InlineData("6pm", false)]
    [InlineData("18:00:00", false)]
    [InlineData("", false)]
    public void Slot_time_must_be_hh_mm(string time, bool valid)
    {
        var slots = new List<LessonSlot> { new(DayOfWeek.Friday, time) };

        Assert.Equal(valid, new GenerateScheduleValidator().Validate(Generate(group: Guid.NewGuid(), slots: slots)).IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Lessons_count_override_is_between_1_and_200(int count, bool valid)
    {
        Assert.Equal(valid, new GenerateScheduleValidator().Validate(Generate(group: Guid.NewGuid(), lessons: count)).IsValid);
    }

    // ---- reassign teacher
    [Fact]
    public void Reassignment_needs_two_different_teachers()
    {
        var validator = new ReassignTeacherValidator();
        var teacher = Guid.NewGuid();

        Assert.True(validator.Validate(new ReassignTeacherRequest(teacher, Guid.NewGuid())).IsValid);
        Assert.False(validator.Validate(new ReassignTeacherRequest(teacher, teacher)).IsValid);
        Assert.False(validator.Validate(new ReassignTeacherRequest(Guid.Empty, teacher)).IsValid);
        Assert.False(validator.Validate(new ReassignTeacherRequest(teacher, Guid.Empty)).IsValid);
    }
}

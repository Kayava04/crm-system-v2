namespace Crm.IntegrationTests.Tests;

public class SchedulingTests(CrmApiFactory factory) : ApiTest(factory)
{
    private async Task<(Guid Teacher, Guid Enrollment)> ActiveEnrollmentAsync(int lessons = 6)
    {
        var teacher = await Data.TeacherAsync();
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync(lessons: lessons));

        return (teacher, enrollment);
    }

    [Fact]
    public async Task Generation_creates_the_lessons_of_the_course_on_the_agreed_weekdays_at_school_time()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();

        var result = (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);

        Assert.Equal(6, result["createdCount"].GetValue<int>());
        Assert.Equal("Europe/Kyiv", result["timeZone"].GetValue<string>());

        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        Assert.Equal(6, lessons.Count);

        foreach (var lesson in lessons)
        {
            var start = lesson!["scheduledDate"]!.GetValue<DateTime>();

            // 18:00 in Kyiv is 15:00 UTC in summer and 16:00 UTC in winter
            var kyiv = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(start, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv"));
            Assert.Equal(new TimeOnly(18, 0), TimeOnly.FromDateTime(kyiv));
            Assert.Contains(kyiv.DayOfWeek, new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday });
            Assert.True(start > DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task Lessons_cross_the_autumn_clock_change_without_shifting_the_local_time()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();

        // Kyiv leaves summer time on Sunday 29 Oct 2034: Tuesday 24 Oct is UTC+3, Tuesday 31 Oct is UTC+2
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher,
            slots: [new { dayOfWeek = "Tuesday", startTime = "18:00" }], startDate: "2034-10-24", lessonsCount: 2)).Expect(201);

        var starts = (await Data.LessonsAsync(enrollmentId: enrollment))
            .Select(l => l!["scheduledDate"]!.GetValue<DateTime>()).Order().ToList();

        Assert.Equal([new DateTime(2034, 10, 24, 15, 0, 0), new DateTime(2034, 10, 31, 16, 0, 0)], starts);
    }

    [Fact]
    public async Task Generating_again_adds_nothing_because_every_lesson_is_already_scheduled()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);

        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(409);
    }

    [Fact]
    public async Task Generation_is_refused_for_a_draft_enrollment_a_busy_teacher_and_bad_input()
    {
        var teacher = await Data.TeacherAsync();
        var draft = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync(), activate: false);
        var (_, enrollment) = await ActiveEnrollmentAsync();

        (await Data.GenerateAsync(enrollmentId: draft, teacherId: teacher)).Expect(409);
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: Guid.NewGuid())).Expect(404);
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher, slots: [new { dayOfWeek = "Monday", startTime = "25:99" }])).Expect(400);
        (await Data.GenerateAsync(enrollmentId: enrollment, groupId: Guid.NewGuid(), teacherId: teacher)).Expect(400);
        (await Data.GenerateAsync(enrollmentId: Guid.NewGuid(), teacherId: teacher)).Expect(404);
    }

    [Fact]
    public async Task A_teacher_cannot_be_double_booked_and_a_refused_generation_creates_nothing()
    {
        var (teacher, first) = await ActiveEnrollmentAsync();
        var second = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        (await Data.GenerateAsync(enrollmentId: first, teacherId: teacher)).Expect(201);

        var refused = (await Data.GenerateAsync(enrollmentId: second, teacherId: teacher)).Expect(409);

        Assert.Contains("already has lessons", refused["detail"].GetValue<string>());
        Assert.Empty(await Data.LessonsAsync(enrollmentId: second));
    }

    [Fact]
    public async Task Overlaps_are_detected_but_back_to_back_lessons_are_fine()
    {
        var (teacher, first) = await ActiveEnrollmentAsync();
        var second = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        (await Data.GenerateAsync(enrollmentId: first, teacherId: teacher, lessonsCount: 1)).Expect(201);
        var start = (await Data.LessonsAsync(enrollmentId: first)).Single()!["scheduledDate"]!.GetValue<DateTime>();

        object Lesson(DateTime at, int minutes) => new { enrollmentId = second, groupId = (Guid?)null, teacherId = teacher, scheduledDate = at, durationMinutes = minutes, notes = (string?)null };

        (await Api.PostAsync("/api/schedules", Lesson(start, 30), Admin)).Expect(409);                       // same start
        (await Api.PostAsync("/api/schedules", Lesson(start.AddMinutes(30), 60), Admin)).Expect(409);       // starts inside
        (await Api.PostAsync("/api/schedules", Lesson(start.AddMinutes(-30), 60), Admin)).Expect(409);      // ends inside
        (await Api.PostAsync("/api/schedules", Lesson(start.AddMinutes(60), 30), Admin)).Expect(201);        // starts when the other ends
        (await Api.PostAsync("/api/schedules", Lesson(start.AddMinutes(-30), 30), Admin)).Expect(201);       // ends when the other starts
    }

    [Fact]
    public async Task A_single_lesson_needs_a_future_date_a_target_and_a_sensible_duration()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();
        object Lesson(DateTime at, int minutes = 60) => new { enrollmentId = enrollment, groupId = (Guid?)null, teacherId = teacher, scheduledDate = at, durationMinutes = minutes, notes = (string?)null };

        (await Api.PostAsync("/api/schedules", Lesson(DateTime.UtcNow.AddDays(-1)), Admin)).Expect(400);
        (await Api.PostAsync("/api/schedules", Lesson(DateTime.UtcNow.AddDays(9), 5), Admin)).Expect(400);
        (await Api.PostAsync("/api/schedules", new { enrollmentId = (Guid?)null, groupId = (Guid?)null, teacherId = teacher, scheduledDate = DateTime.UtcNow.AddDays(9), durationMinutes = 60 }, Admin)).Expect(400);
        (await Api.PostAsync("/api/schedules", Lesson(DateTime.UtcNow.AddDays(9)), Admin)).Expect(201);
    }

    [Fact]
    public async Task Complete_cancel_and_update_follow_the_lesson_lifecycle()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        var completed = lessons[0]!["id"]!.GetValue<Guid>();
        var cancelled = lessons[1]!["id"]!.GetValue<Guid>();

        (await Api.PutAsync($"/api/schedules/{completed}/complete", null, Admin)).Expect(204);
        (await Api.PutAsync($"/api/schedules/{completed}/complete", null, Admin)).Expect(409);
        (await Api.PutAsync($"/api/schedules/{completed}/cancel", null, Admin)).Expect(409);
        (await Api.PutAsync($"/api/schedules/{completed}", new { teacherId = teacher, durationMinutes = 45, notes = (string?)null }, Admin)).Expect(409);

        (await Api.PutAsync($"/api/schedules/{cancelled}/cancel", null, Admin)).Expect(204);
        (await Api.PutAsync($"/api/schedules/{cancelled}/complete", null, Admin)).Expect(409);
        Assert.Equal("Manual", await Sql($"select \"CancellationReason\" from scheduling.schedules where \"Id\" = '{cancelled}'"));

        (await Api.PutAsync($"/api/schedules/{lessons[2]!["id"]}", new { teacherId = teacher, durationMinutes = 45, notes = "note" }, Admin)).Expect(204);
        var updated = (await Api.GetAsync($"/api/schedules/{lessons[2]!["id"]}", Admin)).Expect(200);
        Assert.Equal(45, updated["durationMinutes"].GetValue<int>());
        Assert.Equal("note", updated["notes"].GetValue<string>());

        (await Api.PutAsync($"/api/schedules/{lessons[3]!["id"]}", new { teacherId = Guid.NewGuid(), durationMinutes = 45, notes = (string?)null }, Admin)).Expect(404);
        (await Api.GetAsync($"/api/schedules/{Guid.NewGuid()}", Admin)).Expect(404);
    }

    [Fact]
    public async Task Reschedule_moves_a_lesson_and_refuses_a_taken_time()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        var moved = lessons[0]!["id"]!.GetValue<Guid>();
        var free = lessons[^1]!["scheduledDate"]!.GetValue<DateTime>().AddDays(7);

        (await Api.PutAsync($"/api/schedules/{moved}/reschedule", new { scheduledDate = lessons[1]!["scheduledDate"] }, Admin)).Expect(409);
        (await Api.PutAsync($"/api/schedules/{moved}/reschedule", new { scheduledDate = DateTime.UtcNow.AddDays(-1) }, Admin)).Expect(400);
        (await Api.PutAsync($"/api/schedules/{moved}/reschedule", new { scheduledDate = free }, Admin)).Expect(204);

        var lesson = (await Api.GetAsync($"/api/schedules/{moved}", Admin)).Expect(200);
        Assert.Equal("Rescheduled", lesson["status"].GetValue<string>());
        Assert.Equal(free, lesson["scheduledDate"].GetValue<DateTime>());

        // a rescheduled lesson is still open: it can be completed
        (await Api.PutAsync($"/api/schedules/{moved}/complete", null, Admin)).Expect(204);
    }

    [Fact]
    public async Task Cancel_future_and_regenerate_replaces_the_schedule()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        var first = (await Data.LessonsAsync(enrollmentId: enrollment))[0]!["id"]!.GetValue<Guid>();
        (await Api.PutAsync($"/api/schedules/{first}/complete", null, Admin)).Expect(204);

        var cancelled = (await Api.PutAsync("/api/schedules/cancel-future", new { enrollmentId = enrollment, groupId = (Guid?)null }, Admin)).Expect(200);
        Assert.Equal(5, cancelled["cancelledCount"].GetValue<int>());

        // new agreement: Mondays and Wednesdays; only the 5 missing lessons are created
        var regenerated = (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher, slots: TestData.MonWed)).Expect(201);
        Assert.Equal(5, regenerated["createdCount"].GetValue<int>());

        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        Assert.Equal(1, TestData.Count(lessons, "Completed"));
        Assert.Equal(5, TestData.Count(lessons, "Cancelled"));
        Assert.Equal(5, TestData.Count(lessons, "Scheduled"));

        (await Api.PutAsync("/api/schedules/cancel-future", new { enrollmentId = (Guid?)null, groupId = (Guid?)null }, Admin)).Expect(400);
    }

    [Fact]
    public async Task List_filters_by_status_teacher_and_date_range_and_pages()
    {
        var (teacher, enrollment) = await ActiveEnrollmentAsync();
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        var second = lessons[1]!["scheduledDate"]!.GetValue<DateTime>();
        (await Api.PutAsync($"/api/schedules/{lessons[0]!["id"]}/complete", null, Admin)).Expect(204);

        Assert.Equal(1, (await Api.GetAsync($"/api/schedules?teacherId={teacher}&status=Completed", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(6, (await Api.GetAsync($"/api/schedules?teacherId={teacher}", Admin))["totalCount"].GetValue<int>());

        var exact = (await Api.GetAsync($"/api/schedules?teacherId={teacher}&dateFrom={second:yyyy-MM-ddTHH:mm:ssZ}&dateTo={second:yyyy-MM-ddTHH:mm:ssZ}", Admin)).Expect(200);
        Assert.Equal(1, exact["totalCount"].GetValue<int>());

        var paged = (await Api.GetAsync($"/api/schedules?teacherId={teacher}&pageSize=4&page=2", Admin)).Expect(200);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(2, paged["totalPages"].GetValue<int>());
    }

    [Fact]
    public async Task Scheduling_needs_permissions()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        (await Api.GetAsync("/api/schedules", student.Token)).Expect(403);
        (await Api.PostAsync("/api/schedules", new { }, student.Token)).Expect(403);
        (await Api.PostAsync("/api/schedules/generate", new { }, student.Token)).Expect(403);
        (await Api.GetAsync("/api/schedules")).Expect(401);
    }
}

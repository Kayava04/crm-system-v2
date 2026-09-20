namespace Crm.IntegrationTests.Tests;

// Deactivating instead of deleting: what happens to enrollments, lessons and accounts, and how it is undone
public class LifecycleTests(CrmApiFactory factory) : ApiTest(factory)
{
    private Task<ApiResponse> SetStudent(Guid id, string status) => Api.PutAsync($"/api/students/{id}/status", new { status }, Admin);
    private Task<ApiResponse> SetTeacher(Guid id, string status) => Api.PutAsync($"/api/teachers/{id}/status", new { status }, Admin);

    private async Task<string?> Reasons(Guid enrollmentId) =>
        await Sql($"select string_agg(distinct \"Status\" || '/' || \"CancellationReason\", ',' order by \"Status\" || '/' || \"CancellationReason\") from scheduling.schedules where \"EnrollmentId\" = '{enrollmentId}'");

    private async Task<(Guid Student, Guid Teacher, Guid Enrollment)> ScheduledStudentAsync(int lessons = 6)
    {
        var student = await Data.StudentAsync();
        var teacher = await Data.TeacherAsync();
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync(lessons: lessons));
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);

        return (student, teacher, enrollment);
    }

    // ------------------------------------------------------------------ students
    [Fact]
    public async Task A_paused_student_loses_the_lessons_but_keeps_the_account()
    {
        var (student, _, enrollment) = await ScheduledStudentAsync();
        var user = await Data.StudentUserAsync(student);

        var result = (await SetStudent(student, "Suspended")).Expect(200);

        Assert.Equal(1, result["suspendedEnrollments"].GetValue<int>());
        Assert.Equal(6, result["cancelledLessons"].GetValue<int>());
        Assert.True(result["accountActive"].GetValue<bool>());
        Assert.Equal("Cancelled/EnrollmentInactive", await Reasons(enrollment));
        Assert.Equal("Suspended", (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin))["status"].GetValue<string>());
        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
    }

    [Fact]
    public async Task A_returning_student_gets_the_upcoming_lessons_back()
    {
        var (student, _, enrollment) = await ScheduledStudentAsync();
        await SetStudent(student, "Suspended");

        var result = (await SetStudent(student, "Active")).Expect(200);

        Assert.Equal(1, result["resumedEnrollments"].GetValue<int>());
        Assert.Equal(6, result["restoredLessons"].GetValue<int>());
        Assert.Equal(0, result["lessonsLeftToSchedule"].GetValue<int>());
        Assert.Equal("Scheduled/None", await Reasons(enrollment));
        Assert.Equal("Active", (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin))["status"].GetValue<string>());
    }

    [Fact]
    public async Task Lessons_that_passed_during_a_long_pause_are_left_to_be_scheduled_again()
    {
        var (student, teacher, enrollment) = await ScheduledStudentAsync();
        await SetStudent(student, "Suspended");
        await Factory.Database.ExecuteAsync($@"update scheduling.schedules set ""ScheduledDate"" = now() - interval '3 days'
            where ""Id"" in (select ""Id"" from scheduling.schedules where ""EnrollmentId"" = '{enrollment}' order by ""ScheduledDate"" limit 2)");

        var result = (await SetStudent(student, "Active")).Expect(200);

        Assert.Equal(4, result["restoredLessons"].GetValue<int>());
        Assert.Equal(2, result["lessonsLeftToSchedule"].GetValue<int>());

        var regenerated = (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        Assert.Equal(2, regenerated["createdCount"].GetValue<int>());
    }

    [Fact]
    public async Task A_lesson_cancelled_by_hand_stays_cancelled_when_the_student_returns()
    {
        var (student, _, enrollment) = await ScheduledStudentAsync();
        var first = (await Data.LessonsAsync(enrollmentId: enrollment))[0]!["id"]!.GetValue<Guid>();
        (await Api.PutAsync($"/api/schedules/{first}/cancel", null, Admin)).Expect(204);
        await SetStudent(student, "Suspended");

        var result = (await SetStudent(student, "Active")).Expect(200);

        Assert.Equal(5, result["restoredLessons"].GetValue<int>());
        Assert.Equal(1, result["lessonsLeftToSchedule"].GetValue<int>());
        Assert.Equal("Cancelled/Manual,Scheduled/None", await Reasons(enrollment));
    }

    [Fact]
    public async Task An_enrollment_paused_by_hand_is_not_resumed_automatically()
    {
        var (student, _, enrollment) = await ScheduledStudentAsync();
        (await Api.PutAsync($"/api/enrollments/{enrollment}/suspend", null, Admin)).Expect(200);
        await SetStudent(student, "Suspended");

        var result = (await SetStudent(student, "Active")).Expect(200);

        Assert.Equal(0, result["resumedEnrollments"].GetValue<int>());
        Assert.Equal("Suspended", (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin))["status"].GetValue<string>());

        var manual = (await Api.PutAsync($"/api/enrollments/{enrollment}/activate", null, Admin)).Expect(200);
        Assert.Equal(6, manual["restoredLessons"].GetValue<int>());
    }

    [Fact]
    public async Task A_withdrawn_student_cannot_log_in_and_all_data_is_kept_and_it_can_be_undone()
    {
        var (student, _, enrollment) = await ScheduledStudentAsync();
        var user = await Data.StudentUserAsync(student);
        var refresh = (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200)["refreshToken"].GetValue<string>();

        var result = (await SetStudent(student, "Withdrawn")).Expect(200);

        Assert.False(result["accountActive"].GetValue<bool>());
        Assert.Equal(403, (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Code);
        (await Api.PostAsync("/api/auth/refresh", new { refreshToken = refresh })).Expect(401);
        (await Api.GetAsync($"/api/students/{student}", Admin)).Expect(200);
        (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin)).Expect(200);

        var back = (await SetStudent(student, "Active")).Expect(200);
        Assert.True(back["accountActive"].GetValue<bool>());
        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
        Assert.Equal("Scheduled/None", await Reasons(enrollment));
    }

    [Fact]
    public async Task An_inactive_student_cannot_be_enrolled_or_have_an_enrollment_activated()
    {
        var student = await Data.StudentAsync();
        var draft = await Data.EnrollmentAsync(student, await Data.CourseAsync(), activate: false);
        await SetStudent(student, "Suspended");

        (await Api.PutAsync($"/api/enrollments/{draft}/activate", null, Admin)).Expect(409);
        (await Api.PostAsync("/api/enrollments", new
        {
            studentId = student, courseId = await Data.CourseAsync(), startDate = Today,
            discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null
        }, Admin)).Expect(409);
    }

    [Fact]
    public async Task Setting_the_same_status_twice_is_a_conflict()
    {
        var student = await Data.StudentAsync();

        (await SetStudent(student, "Suspended")).Expect(200);
        (await SetStudent(student, "Suspended")).Expect(409);
        (await SetStudent(Guid.NewGuid(), "Suspended")).Expect(404);
    }

    [Fact]
    public async Task A_paused_student_and_their_group_lessons_disappear_from_each_others_calendars()
    {
        var teacherId = await Data.TeacherAsync();
        var course = await Data.CourseAsync("Group", lessons: 4);
        var pausedStudent = await Data.StudentAsync();
        var activeStudent = await Data.StudentAsync();
        var group = await Data.GroupAsync(course, teacherId,
            await Data.EnrollmentAsync(pausedStudent, course), await Data.EnrollmentAsync(activeStudent, course));
        (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);
        var teacher = await Data.TeacherUserAsync(teacherId);
        var paused = await Data.StudentUserAsync(pausedStudent);
        var range = $"from={DateTime.UtcNow:yyyy-MM-dd}&to={DateTime.UtcNow.AddDays(120):yyyy-MM-dd}";

        await SetStudent(pausedStudent, "Suspended");

        // the group lessons themselves stay, for everyone still attending
        Assert.Equal(4, TestData.Count(await Data.LessonsAsync(groupId: group), "Scheduled"));
        Assert.Empty((await Api.GetAsync($"/api/calendar/my?{range}", paused.Token)).Expect(200)["items"].AsArray());
        var teacherItems = (await Api.GetAsync($"/api/calendar/my?{range}", teacher.Token)).Expect(200)["items"].AsArray();
        Assert.All(teacherItems, i => Assert.Single(i!["students"]!.AsArray()));

        var members = (await Api.GetAsync($"/api/study-groups/{group}", Admin))["members"].AsArray();
        Assert.Single(members, m => !m!["isActive"]!.GetValue<bool>());
    }

    // ------------------------------------------------------------------ teachers
    [Fact]
    public async Task A_teacher_on_leave_loses_the_lessons_and_gets_them_back()
    {
        var (_, teacher, enrollment) = await ScheduledStudentAsync();
        var user = await Data.TeacherUserAsync(teacher);

        var leave = (await SetTeacher(teacher, "OnLeave")).Expect(200);

        Assert.Equal(6, leave["cancelledLessons"].GetValue<int>());
        Assert.True(leave["accountActive"].GetValue<bool>());
        Assert.False(string.IsNullOrEmpty(leave["hint"].GetValue<string>()));
        Assert.Equal("Cancelled/TeacherUnavailable", await Reasons(enrollment));
        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);

        var back = (await SetTeacher(teacher, "Employed")).Expect(200);
        Assert.Equal(6, back["restoredLessons"].GetValue<int>());
        Assert.Equal("Scheduled/None", await Reasons(enrollment));
    }

    [Fact]
    public async Task An_unavailable_teacher_cannot_be_given_lessons_or_groups()
    {
        var teacher = await Data.TeacherAsync();
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        await SetTeacher(teacher, "OnLeave");

        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(409);
        (await Api.PostAsync("/api/schedules", new { enrollmentId = enrollment, groupId = (Guid?)null, teacherId = teacher, scheduledDate = DateTime.UtcNow.AddDays(9), durationMinutes = 60, notes = (string?)null }, Admin)).Expect(409);
        (await Api.PostAsync("/api/study-groups", new { courseId = await Data.CourseAsync("Group"), teacherId = teacher, name = "G" }, Admin)).Expect(409);
    }

    [Fact]
    public async Task A_dismissed_teacher_cannot_log_in_and_the_lessons_can_be_handed_to_another_teacher()
    {
        var (_, teacher, enrollment) = await ScheduledStudentAsync();
        var replacement = await Data.TeacherAsync();
        var user = await Data.TeacherUserAsync(teacher);

        var dismissed = (await SetTeacher(teacher, "Dismissed")).Expect(200);
        Assert.False(dismissed["accountActive"].GetValue<bool>());
        Assert.Equal(403, (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Code);

        var handed = (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = teacher, toTeacherId = replacement }, Admin)).Expect(200);

        Assert.Equal(6, handed["reassignedCount"].GetValue<int>());
        Assert.Equal(6, handed["restoredCount"].GetValue<int>());
        Assert.Equal("Scheduled/None", await Reasons(enrollment));
        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        Assert.All(lessons, l => Assert.Equal(replacement, l!["teacherId"]!.GetValue<Guid>()));

        // the teacher can come back later, but the lessons stay with the replacement
        var back = (await SetTeacher(teacher, "Employed")).Expect(200);
        Assert.Equal(0, back["restoredLessons"].GetValue<int>());
        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
    }

    [Fact]
    public async Task Reassignment_also_moves_the_groups_and_is_all_or_nothing_on_conflicts()
    {
        var (_, teacher, enrollment) = await ScheduledStudentAsync();
        var groupCourse = await Data.CourseAsync("Group", lessons: 4);
        var group = await Data.GroupAsync(groupCourse, teacher, await Data.EnrollmentAsync(await Data.StudentAsync(), groupCourse));
        (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);
        var replacement = await Data.TeacherAsync();
        await SetTeacher(teacher, "Dismissed");

        // the replacement is already busy at the first lesson's time
        var first = (await Data.LessonsAsync(enrollmentId: enrollment)).OrderBy(l => l!["scheduledDate"]!.GetValue<DateTime>()).First()!;
        var blocker = (await Api.PostAsync("/api/schedules", new
        {
            enrollmentId = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync()), groupId = (Guid?)null, teacherId = replacement,
            scheduledDate = first["scheduledDate"], durationMinutes = 60, notes = (string?)null
        }, Admin)).Expect(201);

        (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = teacher, toTeacherId = replacement }, Admin)).Expect(409);
        Assert.Equal("Cancelled/TeacherUnavailable", await Reasons(enrollment));   // nothing changed

        (await Api.PutAsync($"/api/schedules/{blocker.Id}/cancel", null, Admin)).Expect(204);
        var handed = (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = teacher, toTeacherId = replacement }, Admin)).Expect(200);

        Assert.Equal(10, handed["reassignedCount"].GetValue<int>());   // 6 individual + 4 group lessons
        Assert.Equal(1, handed["updatedGroupsCount"].GetValue<int>());
        Assert.Equal(replacement, (await Api.GetAsync($"/api/study-groups/{group}", Admin))["teacherId"].GetValue<Guid>());
    }

    [Fact]
    public async Task Reassignment_rejects_bad_targets()
    {
        var teacher = await Data.TeacherAsync();
        var away = await Data.TeacherAsync();
        await SetTeacher(away, "OnLeave");

        (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = teacher, toTeacherId = teacher }, Admin)).Expect(400);
        (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = teacher, toTeacherId = Guid.NewGuid() }, Admin)).Expect(404);
        (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = Guid.NewGuid(), toTeacherId = teacher }, Admin)).Expect(404);
        (await Api.PutAsync("/api/schedules/reassign-teacher", new { fromTeacherId = teacher, toTeacherId = away }, Admin)).Expect(409);
    }

    [Fact]
    public async Task A_lesson_waits_for_both_the_teacher_and_the_student()
    {
        var (student, teacher, enrollment) = await ScheduledStudentAsync();

        await SetTeacher(teacher, "OnLeave");
        await SetStudent(student, "Suspended");
        await SetTeacher(teacher, "Employed");

        // the teacher is back but the student is still away: the lessons stay cancelled
        Assert.Equal("Cancelled/EnrollmentInactive", await Reasons(enrollment));

        await SetStudent(student, "Active");
        Assert.Equal("Scheduled/None", await Reasons(enrollment));
    }

    // ------------------------------------------------------------------ enrollments
    [Theory]
    [InlineData("suspend")]
    [InlineData("complete")]
    [InlineData("terminate")]
    public async Task Ending_or_pausing_an_enrollment_cancels_its_upcoming_lessons(string action)
    {
        var (_, _, enrollment) = await ScheduledStudentAsync();

        var result = (await Api.PutAsync($"/api/enrollments/{enrollment}/{action}", null, Admin)).Expect(200);

        Assert.Equal(6, result["cancelledLessons"].GetValue<int>());
        Assert.Equal("Cancelled/EnrollmentInactive", await Reasons(enrollment));
    }

    // ------------------------------------------------------------------ nothing is erased
    [Fact]
    public async Task People_with_history_cannot_be_deleted_but_empty_records_can()
    {
        var (student, teacher, _) = await ScheduledStudentAsync();

        (await Api.DeleteAsync($"/api/students/{student}", null, Admin)).Expect(409);
        (await Api.DeleteAsync($"/api/teachers/{teacher}", null, Admin)).Expect(409);
        (await Api.GetAsync($"/api/students/{student}", Admin)).Expect(200);
        (await Api.GetAsync($"/api/teachers/{teacher}", Admin)).Expect(200);

        (await Api.DeleteAsync($"/api/students/{await Data.StudentAsync()}", null, Admin)).Expect(204);
        (await Api.DeleteAsync($"/api/teachers/{await Data.TeacherAsync()}", null, Admin)).Expect(204);
    }

    [Fact]
    public async Task A_person_with_an_account_cannot_be_deleted()
    {
        var student = await Data.StudentAsync();
        var teacher = await Data.TeacherAsync();
        await Data.StudentUserAsync(student);
        await Data.TeacherUserAsync(teacher);

        (await Api.DeleteAsync($"/api/students/{student}", null, Admin)).Expect(409);
        (await Api.DeleteAsync($"/api/teachers/{teacher}", null, Admin)).Expect(409);
    }
}

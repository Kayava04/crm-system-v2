namespace Crm.IntegrationTests.Tests;

public class CalendarTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static string From => DateTime.UtcNow.ToString("yyyy-MM-dd");
    private static string To => DateTime.UtcNow.AddDays(120).ToString("yyyy-MM-dd");

    private Task<ApiResponse> CalendarAsync(string token) => Api.GetAsync($"/api/calendar/my?from={From}&to={To}", token);

    [Fact]
    public async Task A_student_sees_own_lessons_with_the_teacher_and_course_but_not_other_students()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync(format: "Offline");
        var studentId = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(studentId, course);
        var otherEnrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), course);
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        (await Data.GenerateAsync(enrollmentId: otherEnrollment, teacherId: await Data.TeacherAsync(), slots: TestData.MonWed)).Expect(201);
        var student = await Data.StudentUserAsync(studentId);

        var calendar = (await CalendarAsync(student.Token)).Expect(200);

        Assert.Equal("Europe/Kyiv", calendar["timeZone"].GetValue<string>());
        var items = calendar["items"].AsArray();
        Assert.Equal(6, items.Count);
        Assert.All(items, i =>
        {
            Assert.Contains("Teacher", i!["teacherName"]!.GetValue<string>());
            Assert.StartsWith("Course", i["courseName"]!.GetValue<string>());
            Assert.False(i["isOnline"]!.GetValue<bool>());
            Assert.False(i["isGroup"]!.GetValue<bool>());
            Assert.Empty(i["students"]!.AsArray());
        });
        Assert.Equal(items.Select(i => i!["startsAt"]!.GetValue<DateTime>()).Order(), items.Select(i => i!["startsAt"]!.GetValue<DateTime>()));
    }

    [Fact]
    public async Task A_teacher_sees_the_students_of_individual_and_group_lessons()
    {
        var teacherId = await Data.TeacherAsync();
        var individualCourse = await Data.CourseAsync();
        var groupCourse = await Data.CourseAsync("Group", lessons: 4);
        var alone = await Data.EnrollmentAsync(await Data.StudentAsync(), individualCourse);
        var m1 = await Data.EnrollmentAsync(await Data.StudentAsync(), groupCourse);
        var m2 = await Data.EnrollmentAsync(await Data.StudentAsync(), groupCourse);
        var group = await Data.GroupAsync(groupCourse, teacherId, m1, m2);
        (await Data.GenerateAsync(enrollmentId: alone, teacherId: teacherId)).Expect(201);
        (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);
        var teacher = await Data.TeacherUserAsync(teacherId);

        var items = (await CalendarAsync(teacher.Token)).Expect(200)["items"].AsArray();

        var individual = items.Where(i => !i!["isGroup"]!.GetValue<bool>()).ToList();
        var grouped = items.Where(i => i!["isGroup"]!.GetValue<bool>()).ToList();
        Assert.Equal(6, individual.Count);
        Assert.Equal(4, grouped.Count);
        Assert.All(individual, i => Assert.Single(i!["students"]!.AsArray()));
        Assert.All(grouped, i =>
        {
            Assert.Equal(2, i!["students"]!.AsArray().Count);
            Assert.StartsWith("Group", i["groupName"]!.GetValue<string>());
        });
        Assert.All(items, i => Assert.Null(i!["teacherName"]));
    }

    [Fact]
    public async Task A_student_in_a_group_sees_the_group_lessons()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync("Group", lessons: 4);
        var studentId = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(studentId, course);
        var group = await Data.GroupAsync(course, teacher, enrollment, await Data.EnrollmentAsync(await Data.StudentAsync(), course));
        (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);
        var student = await Data.StudentUserAsync(studentId);

        var items = (await CalendarAsync(student.Token)).Expect(200)["items"].AsArray();

        Assert.Equal(4, items.Count);
        Assert.All(items, i => Assert.True(i!["isGroup"]!.GetValue<bool>()));
        Assert.All(items, i => Assert.Empty(i!["students"]!.AsArray()));   // classmates stay private
    }

    [Fact]
    public async Task The_range_limits_and_default_are_enforced()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        (await Api.GetAsync("/api/calendar/my", student.Token)).Expect(200);                                   // default: the next 30 days
        (await Api.GetAsync("/api/calendar/my?from=2030-01-01&to=2031-06-01", student.Token)).Expect(400);      // longer than 366 days
        (await Api.GetAsync("/api/calendar/my?from=2030-02-01&to=2030-01-01", student.Token)).Expect(400);      // to before from
    }

    [Fact]
    public async Task Only_the_dates_inside_the_range_are_returned()
    {
        var teacher = await Data.TeacherAsync();
        var studentId = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(studentId, await Data.CourseAsync());
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        var student = await Data.StudentUserAsync(studentId);
        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        var second = lessons[1]!["scheduledDate"]!.GetValue<DateTime>();

        var oneDay = (await Api.GetAsync($"/api/calendar/my?from={second:yyyy-MM-dd}&to={second:yyyy-MM-dd}", student.Token)).Expect(200);

        Assert.Single(oneDay["items"].AsArray());
    }

    [Fact]
    public async Task The_calendar_is_only_for_teachers_and_students_with_a_linked_profile()
    {
        (await CalendarAsync(Admin)).Expect(403);
        (await Api.GetAsync("/api/calendar/my")).Expect(401);

        var unlinked = await Data.UserAsync("Student");   // registered without a profile
        (await CalendarAsync(unlinked.Token)).Expect(404);
    }

    [Fact]
    public async Task My_students_lists_students_from_individual_lessons_and_groups()
    {
        var teacherId = await Data.TeacherAsync();
        var lone = await Data.StudentAsync();
        var inGroup = await Data.StudentAsync();
        var groupCourse = await Data.CourseAsync("Group", lessons: 4);
        var group = await Data.GroupAsync(groupCourse, teacherId, await Data.EnrollmentAsync(inGroup, groupCourse));
        (await Data.GenerateAsync(enrollmentId: await Data.EnrollmentAsync(lone, await Data.CourseAsync()), teacherId: teacherId)).Expect(201);
        (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);
        var teacher = await Data.TeacherUserAsync(teacherId);

        var students = (await Api.GetAsync("/api/teachers/me/students", teacher.Token)).Expect(200).Json!.AsArray();

        Assert.Equal(new[] { lone, inGroup }.Order(), students.Select(s => s!["id"]!.GetValue<Guid>()).Order());
        Assert.All(students, s => Assert.False(string.IsNullOrEmpty(s!["email"]!.GetValue<string>())));
    }

    [Fact]
    public async Task My_students_is_only_for_teachers()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        (await Api.GetAsync("/api/teachers/me/students", student.Token)).Expect(403);
        (await Api.GetAsync("/api/teachers/me/students", Admin)).Expect(403);
        (await Api.GetAsync("/api/teachers/me/students")).Expect(401);
    }
}

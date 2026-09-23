namespace Crm.IntegrationTests.Tests;

public class GroupTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task A_group_can_only_be_created_for_a_group_course_with_an_available_teacher()
    {
        var teacher = await Data.TeacherAsync();
        var individual = await Data.CourseAsync("Individual");
        var group = await Data.CourseAsync("Group");

        (await Api.PostAsync("/api/study-groups", new { courseId = individual, teacherId = teacher, name = "G" }, Admin)).Expect(409);
        (await Api.PostAsync("/api/study-groups", new { courseId = Guid.NewGuid(), teacherId = teacher, name = "G" }, Admin)).Expect(404);
        (await Api.PostAsync("/api/study-groups", new { courseId = group, teacherId = Guid.NewGuid(), name = "G" }, Admin)).Expect(404);
        (await Api.PostAsync("/api/study-groups", new { courseId = group, teacherId = teacher, name = "" }, Admin)).Expect(400);
        (await Api.PostAsync("/api/study-groups", new { courseId = group, teacherId = teacher, name = "G" }, Admin)).Expect(201);
    }

    [Fact]
    public async Task Members_must_be_active_enrollments_of_the_same_course_and_join_only_one_group()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync("Group");
        var group = await Data.GroupAsync(course, teacher);
        var other = await Data.GroupAsync(course, teacher);

        var member = await Data.EnrollmentAsync(await Data.StudentAsync(), course);
        var draft = await Data.EnrollmentAsync(await Data.StudentAsync(), course, activate: false);
        var foreign = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync("Group"));

        (await Api.PostAsync($"/api/study-groups/{group}/members", new { enrollmentId = member }, Admin)).Expect(204);
        (await Api.PostAsync($"/api/study-groups/{group}/members", new { enrollmentId = member }, Admin)).Expect(409);   // already there
        (await Api.PostAsync($"/api/study-groups/{other}/members", new { enrollmentId = member }, Admin)).Expect(409);   // already in a group of this course
        (await Api.PostAsync($"/api/study-groups/{group}/members", new { enrollmentId = draft }, Admin)).Expect(409);     // not active
        (await Api.PostAsync($"/api/study-groups/{group}/members", new { enrollmentId = foreign }, Admin)).Expect(409);   // other course
        (await Api.PostAsync($"/api/study-groups/{group}/members", new { enrollmentId = Guid.NewGuid() }, Admin)).Expect(404);
        (await Api.PostAsync($"/api/study-groups/{Guid.NewGuid()}/members", new { enrollmentId = member }, Admin)).Expect(404);
    }

    [Fact]
    public async Task Members_can_be_removed_and_added_again_and_the_detail_shows_who_they_are()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync("Group");
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), course);
        var group = await Data.GroupAsync(course, teacher, enrollment);

        var detail = (await Api.GetAsync($"/api/study-groups/{group}", Admin)).Expect(200);
        var member = detail["members"].AsArray().Single()!;
        Assert.StartsWith("Test", member["studentName"]!.GetValue<string>());
        Assert.True(member["isActive"]!.GetValue<bool>());

        (await Api.DeleteAsync($"/api/study-groups/{group}/members/{enrollment}", null, Admin)).Expect(204);
        (await Api.DeleteAsync($"/api/study-groups/{group}/members/{enrollment}", null, Admin)).Expect(404);
        (await Api.PostAsync($"/api/study-groups/{group}/members", new { enrollmentId = enrollment }, Admin)).Expect(204);

        var list = (await Api.GetAsync($"/api/study-groups?courseId={course}", Admin)).Expect(200);
        Assert.Equal(1, list.Items.Single()!["membersCount"]!.GetValue<int>());
        (await Api.GetAsync($"/api/study-groups/{Guid.NewGuid()}", Admin)).Expect(404);
    }

    [Fact]
    public async Task Group_lessons_are_one_row_per_lesson_not_one_per_student()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync("Group", lessons: 4);
        var members = new[] { await Data.EnrollmentAsync(await Data.StudentAsync(), course), await Data.EnrollmentAsync(await Data.StudentAsync(), course), await Data.EnrollmentAsync(await Data.StudentAsync(), course) };
        var group = await Data.GroupAsync(course, teacher, members);

        var generated = (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);   // teacher defaults to the group's

        Assert.Equal(4, generated["createdCount"].GetValue<int>());
        var lessons = await Data.LessonsAsync(groupId: group);
        Assert.Equal(4, lessons.Count);
        Assert.All(lessons, l => Assert.Equal(teacher, l!["teacherId"]!.GetValue<Guid>()));
    }

    [Fact]
    public async Task A_group_course_enrollment_cannot_get_individual_lessons()
    {
        var teacher = await Data.TeacherAsync();
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync("Group"));

        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(409);
        (await Api.PostAsync("/api/schedules", new { enrollmentId = enrollment, groupId = (Guid?)null, teacherId = teacher, scheduledDate = DateTime.UtcNow.AddDays(5), durationMinutes = 60, notes = (string?)null }, Admin)).Expect(409);
    }

    [Fact]
    public async Task A_group_lesson_is_paid_to_the_teacher_once_however_many_students_attend()
    {
        var teacher = await Data.TeacherAsync(baseSalary: 1000, lessonsRate: 100);
        var course = await Data.CourseAsync("Group", lessons: 4);
        var group = await Data.GroupAsync(course, teacher,
            await Data.EnrollmentAsync(await Data.StudentAsync(), course), await Data.EnrollmentAsync(await Data.StudentAsync(), course));
        (await Data.GenerateAsync(groupId: group, slots: TestData.Friday)).Expect(201);
        var lesson = (await Data.LessonsAsync(groupId: group))[0]!;
        (await Api.PutAsync($"/api/schedules/{lesson["id"]}/complete", null, Admin)).Expect(204);
        var month = lesson["scheduledDate"]!.GetValue<DateTime>().ToString("yyyy-MM");

        var payroll = (await Api.PostAsync("/api/billing/payrolls", new { teacherId = teacher, period = month }, Admin)).Expect(201);

        Assert.Equal(1, payroll["completedLessonsCount"].GetValue<int>());
        Assert.Equal(1100m, payroll["totalAmount"].GetValue<decimal>());
    }

    [Fact]
    public async Task Updating_a_group_can_change_its_name_and_teacher()
    {
        var course = await Data.CourseAsync("Group");
        var group = await Data.GroupAsync(course, await Data.TeacherAsync());
        var newTeacher = await Data.TeacherAsync();

        (await Api.PutAsync($"/api/study-groups/{group}", new { name = "Renamed", teacherId = newTeacher }, Admin)).Expect(204);
        (await Api.PutAsync($"/api/study-groups/{group}", new { name = "Renamed", teacherId = Guid.NewGuid() }, Admin)).Expect(404);
        (await Api.PutAsync($"/api/study-groups/{group}", new { name = "", teacherId = newTeacher }, Admin)).Expect(400);

        var detail = (await Api.GetAsync($"/api/study-groups/{group}", Admin)).Expect(200);
        Assert.Equal("Renamed", detail["name"].GetValue<string>());
        Assert.Equal(newTeacher, detail["teacherId"].GetValue<Guid>());
    }
}

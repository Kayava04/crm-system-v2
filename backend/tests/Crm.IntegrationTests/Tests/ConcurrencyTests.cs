namespace Crm.IntegrationTests.Tests;

// Many requests at the same moment: the rules must hold and nothing may answer with a server error
public class ConcurrencyTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Simultaneous_enrollments_on_the_same_day_all_succeed_with_unique_numbers()
    {
        var course = await Data.CourseAsync();
        var students = new List<Guid>();
        for (var i = 0; i < 12; i++)
            students.Add(await Data.StudentAsync());

        var responses = await Task.WhenAll(students.Select(s => Api.PostAsync("/api/enrollments", new
        {
            studentId = s, courseId = course, startDate = "2036-06-15",
            discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null
        }, Admin)));

        Assert.All(responses, r => Assert.True(r.Code == 201, $"{r.Code}: {r.Raw}"));
        var numbers = responses.Select(r => r["enrollmentNumber"].GetValue<string>()).ToList();
        Assert.Equal(numbers.Count, numbers.Distinct().Count());
    }

    [Fact]
    public async Task The_same_student_enrolled_twice_at_once_gets_one_enrollment()
    {
        var student = await Data.StudentAsync();
        var course = await Data.CourseAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Api.PostAsync("/api/enrollments", new
        {
            studentId = student, courseId = course, startDate = Today,
            discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null
        }, Admin)));

        Assert.DoesNotContain(responses, r => r.Code >= 500);
        Assert.Equal(1, responses.Count(r => r.Code == 201));
    }

    [Fact]
    public async Task Two_teachers_lessons_requested_at_once_for_the_same_time_are_never_double_booked()
    {
        var teacher = await Data.TeacherAsync();
        var when = DateTime.UtcNow.AddDays(20).Date.AddHours(10);
        var enrollments = new List<Guid>();
        var course = await Data.CourseAsync();
        for (var i = 0; i < 6; i++)
            enrollments.Add(await Data.EnrollmentAsync(await Data.StudentAsync(), course));

        var responses = await Task.WhenAll(enrollments.Select(e => Api.PostAsync("/api/schedules", new
        {
            enrollmentId = e, groupId = (Guid?)null, teacherId = teacher, scheduledDate = when, durationMinutes = 60, notes = (string?)null
        }, Admin)));

        Assert.DoesNotContain(responses, r => r.Code >= 500);
        Assert.Equal(1, responses.Count(r => r.Code == 201));   // the others get 409, the teacher has one lesson at that time
        Assert.Equal(1, (await Data.LessonsAsync(teacherId: teacher)).Count);
    }

    [Fact]
    public async Task Schedules_generated_at_the_same_moment_for_one_teacher_never_overlap()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync();
        var enrollments = new List<Guid>();
        for (var i = 0; i < 5; i++)
            enrollments.Add(await Data.EnrollmentAsync(await Data.StudentAsync(), course));

        var responses = await Task.WhenAll(enrollments.Select(e => Data.GenerateAsync(enrollmentId: e, teacherId: teacher)));

        Assert.DoesNotContain(responses, r => r.Code >= 500);
        Assert.Equal(1, responses.Count(r => r.Code == 201));   // one wins, the rest are told the teacher is busy

        var lessons = (await Data.LessonsAsync(teacherId: teacher))
            .Select(l => l!["scheduledDate"]!.GetValue<DateTime>()).ToList();
        Assert.Equal(lessons.Count, lessons.Distinct().Count());
    }

    [Fact]
    public async Task Lessons_moved_into_the_same_free_slot_at_once_do_not_collide()
    {
        var teacher = await Data.TeacherAsync();
        var course = await Data.CourseAsync();
        var lessonIds = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), course);
            lessonIds.Add((await Api.PostAsync("/api/schedules", new
            {
                enrollmentId = enrollment, groupId = (Guid?)null, teacherId = teacher,
                scheduledDate = DateTime.UtcNow.AddDays(30 + i).Date.AddHours(9), durationMinutes = 60, notes = (string?)null
            }, Admin)).Expect(201).Id);
        }

        var target = DateTime.UtcNow.AddDays(60).Date.AddHours(15);
        var responses = await Task.WhenAll(lessonIds.Select(id =>
            Api.PutAsync($"/api/schedules/{id}/reschedule", new { scheduledDate = target }, Admin)));

        Assert.DoesNotContain(responses, r => r.Code >= 500);
        Assert.Equal(1, responses.Count(r => r.Code == 204));
        Assert.Equal(1, (await Data.LessonsAsync(teacherId: teacher)).Count(l => l!["scheduledDate"]!.GetValue<DateTime>() == target));
    }

    [Fact]
    public async Task Forty_enrollments_at_once_get_forty_different_numbers()
    {
        var course = await Data.CourseAsync();
        var students = new List<Guid>();
        for (var i = 0; i < 40; i++)
            students.Add(await Data.StudentAsync());

        var responses = await Task.WhenAll(students.Select(s => Api.PostAsync("/api/enrollments", new
        {
            studentId = s, courseId = course, startDate = "2037-01-20",
            discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null
        }, Admin)));

        Assert.All(responses, r => Assert.True(r.Code == 201, $"{r.Code}: {r.Raw}"));
        Assert.Equal(40, responses.Select(r => r["enrollmentNumber"].GetValue<string>()).Distinct().Count());
    }

    [Fact]
    public async Task Simultaneous_status_changes_never_leave_half_done_work()
    {
        var student = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync());

        var responses = await Task.WhenAll(
            Api.PutAsync($"/api/students/{student}/status", new { status = "Suspended" }, Admin),
            Api.PutAsync($"/api/students/{student}/status", new { status = "Withdrawn" }, Admin),
            Api.PutAsync($"/api/enrollments/{enrollment}/terminate", null, Admin),
            Api.PutAsync($"/api/enrollments/{enrollment}/suspend", null, Admin));

        Assert.DoesNotContain(responses, r => r.Code >= 500);

        // whatever order won, the data is consistent: an inactive student has no active enrollment
        var studentStatus = (await Api.GetAsync($"/api/students/{student}", Admin))["status"].GetValue<string>();
        var enrollmentStatus = (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin))["status"].GetValue<string>();
        Assert.NotEqual("Active", studentStatus);
        Assert.NotEqual("Active", enrollmentStatus);
    }

    [Fact]
    public async Task Simultaneous_registrations_of_the_same_email_create_one_account()
    {
        var email = TestData.Email();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            Api.PostAsync("/api/auth/register", new { email, role = "Student" }, Admin)));

        Assert.DoesNotContain(responses, r => r.Code >= 500);
        Assert.Equal(1, responses.Count(r => r.Code == 201));
        Assert.Equal("1", await Sql($"select count(*) from identity.users where \"Email\" = '{email}'"));
    }

    [Fact]
    public async Task Simultaneous_creation_of_the_same_student_or_course_stays_unique()
    {
        var email = TestData.Email("student");
        var courseName = "Concurrent course " + TestData.Unique();

        var students = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Api.PostAsync("/api/students", Data.StudentBody(email: email), Admin)));
        var courses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Api.PostAsync("/api/courses", new
        {
            name = courseName, language = "English", level = "A1", format = "Online", lessonType = "Individual",
            durationMonths = 3, lessonsCount = 10, lessonsPerWeek = 2, price = 100, description = (string?)null
        }, Admin)));

        Assert.DoesNotContain(students, r => r.Code >= 500);
        Assert.DoesNotContain(courses, r => r.Code >= 500);
        Assert.Equal(1, students.Count(r => r.Code == 201));
        Assert.Equal(1, courses.Count(r => r.Code == 201));
    }

    [Fact]
    public async Task Many_reads_and_writes_at_once_do_not_break_the_shared_connection_handling()
    {
        var teacher = await Data.TeacherAsync();
        var tasks = new List<Task<ApiResponse>>();

        for (var i = 0; i < 40; i++)
        {
            tasks.Add(Api.GetAsync("/api/students?pageSize=5", Admin));
            tasks.Add(Api.GetAsync($"/api/teachers/{teacher}", Admin));
            tasks.Add(Api.GetAsync("/api/reports/students/summary", Admin));
            tasks.Add(Api.PostAsync("/api/students", Data.StudentBody(), Admin));
        }

        var responses = await Task.WhenAll(tasks);

        Assert.DoesNotContain(responses, r => r.Code >= 500);
    }
}

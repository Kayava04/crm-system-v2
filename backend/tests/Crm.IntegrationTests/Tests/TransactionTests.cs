using Identity.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scheduling.Contracts;

namespace Crm.IntegrationTests.Tests;

// A change that touches several modules is saved completely or not at all. A module is made to fail in the
// middle of the operation and everything that was already saved by the other modules must be rolled back.
public class TransactionTests(CrmApiFactory factory) : ApiTest(factory)
{
    private sealed class FailingScheduleLifecycle : IScheduleLifecycle
    {
        private static Task<T> Fail<T>() => throw new InvalidOperationException("Simulated failure in the Scheduling module.");

        public Task<int> CancelForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default) => Fail<int>();
        public Task<EnrollmentLessonsRestoreResult> RestoreForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default) => Fail<EnrollmentLessonsRestoreResult>();
        public Task<int> CancelForTeacherAsync(Guid teacherId, CancellationToken ct = default) => Fail<int>();
        public Task<TeacherLessonsRestoreResult> RestoreForTeacherAsync(Guid teacherId, CancellationToken ct = default) => Fail<TeacherLessonsRestoreResult>();
    }

    private sealed class FailingStudentLinker : IProfileLinker
    {
        public string ProfileType => "Student";

        public Task<LinkedProfile?> FindByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<LinkedProfile?>(null);

        public Task LinkAsync(Guid profileId, Guid userId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated failure in the Students module.");
    }

    private Api ClientWith(Action<IServiceCollection> configure)
    {
        var host = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(configure));

        return new Api(host.CreateClient());
    }

    private Api FailingScheduling() => ClientWith(services =>
    {
        services.RemoveAll<IScheduleLifecycle>();
        services.AddScoped<IScheduleLifecycle, FailingScheduleLifecycle>();
    });

    private async Task<string?> EnrollmentStatus(Guid id) =>
        await Sql($"select \"Status\" from enrollments.enrollments where \"Id\" = '{id}'");

    [Fact]
    public async Task A_student_status_change_is_rolled_back_when_the_calendar_update_fails()
    {
        var student = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync());
        var failing = FailingScheduling();

        var response = await failing.PutAsync($"/api/students/{student}/status", new { status = "Suspended" }, Admin);

        Assert.Equal(500, response.Code);
        Assert.Equal(500, response["status"].GetValue<int>());   // a structured ProblemDetails, not a stack trace
        // Students saved the new status and Enrollments suspended the enrollment before Scheduling failed: both are undone
        Assert.Equal("Active", (await Api.GetAsync($"/api/students/{student}", Admin))["status"].GetValue<string>());
        Assert.Equal("Active", await EnrollmentStatus(enrollment));
        Assert.Equal("False", await Sql($"select \"AutoSuspended\" from enrollments.enrollments where \"Id\" = '{enrollment}'"));
    }

    [Fact]
    public async Task Withdrawing_a_student_keeps_the_account_active_when_the_calendar_update_fails()
    {
        var student = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(student);
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync());

        Assert.Equal(500, (await FailingScheduling().PutAsync($"/api/students/{student}/status", new { status = "Withdrawn" }, Admin)).Code);

        // three modules were involved (Students, Identity, Enrollments): none of them kept its part
        Assert.Equal("Active", (await Api.GetAsync($"/api/students/{student}", Admin))["status"].GetValue<string>());
        Assert.Equal("Active", await EnrollmentStatus(enrollment));
        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
    }

    [Fact]
    public async Task The_same_change_succeeds_completely_when_nothing_fails()
    {
        var student = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(student);
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync());

        (await Api.PutAsync($"/api/students/{student}/status", new { status = "Withdrawn" }, Admin)).Expect(200);

        Assert.Equal("Withdrawn", (await Api.GetAsync($"/api/students/{student}", Admin))["status"].GetValue<string>());
        Assert.Equal("Suspended", await EnrollmentStatus(enrollment));
        Assert.Equal(403, (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Code);
    }

    [Fact]
    public async Task A_teacher_status_change_is_rolled_back_when_the_calendar_update_fails()
    {
        var teacher = await Data.TeacherAsync();
        var user = await Data.TeacherUserAsync(teacher);

        Assert.Equal(500, (await FailingScheduling().PutAsync($"/api/teachers/{teacher}/status", new { status = "Dismissed" }, Admin)).Code);

        Assert.Equal("Probation", (await Api.GetAsync($"/api/teachers/{teacher}", Admin))["status"].GetValue<string>());
        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
    }

    [Theory]
    [InlineData("suspend")]
    [InlineData("complete")]
    [InlineData("terminate")]
    public async Task An_enrollment_status_change_is_rolled_back_when_the_calendar_update_fails(string action)
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());

        Assert.Equal(500, (await FailingScheduling().PutAsync($"/api/enrollments/{enrollment}/{action}", null, Admin)).Code);

        Assert.Equal("Active", await EnrollmentStatus(enrollment));
    }

    [Fact]
    public async Task Activating_an_enrollment_is_rolled_back_when_restoring_the_lessons_fails()
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync(), activate: false);

        Assert.Equal(500, (await FailingScheduling().PutAsync($"/api/enrollments/{enrollment}/activate", null, Admin)).Code);

        Assert.Equal("Draft", await EnrollmentStatus(enrollment));
    }

    [Fact]
    public async Task A_failed_registration_leaves_no_account_behind()
    {
        var studentId = await Data.StudentAsync();
        var email = TestData.Email();
        var failing = ClientWith(services =>
        {
            services.RemoveAll<IProfileLinker>();
            services.AddScoped<IProfileLinker, FailingStudentLinker>();
        });

        Assert.Equal(500, (await failing.PostAsync("/api/auth/register", new { email, role = "Student", profileType = "Student", profileId = studentId }, Admin)).Code);

        Assert.Equal("0", await Sql($"select count(*) from identity.users where \"Email\" = '{email}'"));
        Assert.Equal("0", await Sql($"select count(*) from identity.user_roles ur join identity.users u on u.\"Id\" = ur.\"UserId\" where u.\"Email\" = '{email}'"));

        // the profile is still free and the same email can be registered normally
        (await Api.PostAsync("/api/auth/register", new { email, role = "Student", profileType = "Student", profileId = studentId }, Admin)).Expect(201);
    }

    [Fact]
    public async Task Work_after_a_finished_transaction_in_the_same_request_still_saves()
    {
        // Register commits its transaction and then writes the notification through another module afterwards
        var user = await Data.UserAsync("Student");

        Assert.Equal(1, (await Api.GetAsync("/api/notifications/unread-count", user.Token))["count"].GetValue<int>());
    }
}

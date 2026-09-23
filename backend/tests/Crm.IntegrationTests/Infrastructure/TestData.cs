namespace Crm.IntegrationTests.Infrastructure;

public sealed record TestUser(Guid UserId, string Email, string Password, string Token);

// Builds the entities a test needs through the public API, exactly as a client would
public sealed class TestData(Api api, string adminToken)
{
    public static readonly object[] TueThu =
    [
        new { dayOfWeek = "Tuesday", startTime = "18:00" },
        new { dayOfWeek = "Thursday", startTime = "18:00" }
    ];

    public static readonly object[] MonWed =
    [
        new { dayOfWeek = "Monday", startTime = "10:00" },
        new { dayOfWeek = "Wednesday", startTime = "10:00" }
    ];

    public static readonly object[] Friday = [new { dayOfWeek = "Friday", startTime = "12:00" }];

    public Api Api { get; } = api;
    public string Admin { get; } = adminToken;

    public static string Unique() => Guid.NewGuid().ToString("N")[..10];

    public static string Email(string prefix = "user") => $"{prefix}-{Unique()}@example.test";

    // ---------------------------------------------------------------- people, courses
    public object TeacherBody(string? email = null, string? lastName = null, decimal baseSalary = 1000, decimal lessonsRate = 100) => new
    {
        firstName = "Teacher",
        lastName = lastName ?? "Test" + Unique(),
        middleName = (string?)null,
        dateOfBirth = "1985-05-05",
        phoneNumber = "+380501112233",
        email = email ?? Email("teacher"),
        city = "Kyiv",
        country = "Ukraine",
        baseSalary,
        lessonsRate,
        comment = (string?)null
    };

    public object StudentBody(string? email = null, string? lastName = null, bool isChild = false) => new
    {
        firstName = "Student",
        lastName = lastName ?? "Test" + Unique(),
        middleName = (string?)null,
        dateOfBirth = "2000-01-01",
        phoneNumber = "+380501112244",
        email = email ?? Email("student"),
        city = "Kyiv",
        country = "Ukraine",
        isChild,
        comment = (string?)null,
        learningGoal = "Work",
        format = "Online",
        lessonType = "Individual",
        intensity = 2,
        currentLevel = "A2",
        hadPreviousCourses = false,
        languages = new[] { "English" }
    };

    public async Task<Guid> TeacherAsync(decimal baseSalary = 1000, decimal lessonsRate = 100) =>
        (await Api.PostAsync("/api/teachers", TeacherBody(baseSalary: baseSalary, lessonsRate: lessonsRate), Admin)).Expect(201).Id;

    public async Task<Guid> StudentAsync() =>
        (await Api.PostAsync("/api/students", StudentBody(), Admin)).Expect(201).Id;

    public async Task<Guid> CourseAsync(string lessonType = "Individual", int lessons = 6, decimal price = 3000, string format = "Online") =>
        (await Api.PostAsync("/api/courses", new
        {
            name = "Course " + Unique(),
            language = "English",
            level = "B1",
            format,
            lessonType,
            durationMonths = 3,
            lessonsCount = lessons,
            lessonsPerWeek = 2,
            price,
            description = (string?)null
        }, Admin)).Expect(201).Id;

    public async Task<Guid> EnrollmentAsync(Guid studentId, Guid courseId, bool activate = true, decimal? discount = null, string? preferred = null)
    {
        var id = (await Api.PostAsync("/api/enrollments", new
        {
            studentId,
            courseId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            discountedPrice = discount,
            comment = (string?)null,
            preferredSchedule = preferred
        }, Admin)).Expect(201).Id;

        if (activate)
            (await Api.PutAsync($"/api/enrollments/{id}/activate", null, Admin)).Expect(200);

        return id;
    }

    // ---------------------------------------------------------------- groups and lessons
    public async Task<Guid> GroupAsync(Guid courseId, Guid teacherId, params Guid[] enrollmentIds)
    {
        var id = (await Api.PostAsync("/api/study-groups", new { courseId, teacherId, name = "Group " + Unique() }, Admin)).Expect(201).Id;

        foreach (var enrollmentId in enrollmentIds)
            (await Api.PostAsync($"/api/study-groups/{id}/members", new { enrollmentId }, Admin)).Expect(204);

        return id;
    }

    public Task<ApiResponse> GenerateAsync(
        Guid? enrollmentId = null, Guid? groupId = null, Guid? teacherId = null,
        object[]? slots = null, int durationMinutes = 60, string? startDate = null, int? lessonsCount = null) =>
        Api.PostAsync("/api/schedules/generate", new
        {
            enrollmentId,
            groupId,
            teacherId,
            slots = slots ?? TueThu,
            durationMinutes,
            startDate,
            lessonsCount
        }, Admin);

    public async Task<JsonArray> LessonsAsync(Guid? enrollmentId = null, Guid? groupId = null, Guid? teacherId = null)
    {
        var query = new List<string> { "pageSize=100" };
        if (enrollmentId is not null) query.Add($"enrollmentId={enrollmentId}");
        if (groupId is not null) query.Add($"groupId={groupId}");
        if (teacherId is not null) query.Add($"teacherId={teacherId}");

        return (await Api.GetAsync("/api/schedules?" + string.Join('&', query), Admin)).Expect(200).Items;
    }

    public static int Count(JsonArray lessons, string status) =>
        lessons.Count(l => l!["status"]!.GetValue<string>() == status);

    // ---------------------------------------------------------------- accounts
    public async Task<TestUser> UserAsync(string role, string? profileType = null, Guid? profileId = null, List<Guid>? permissionIds = null)
    {
        var email = Email(role.ToLowerInvariant());
        var registered = (await Api.PostAsync("/api/auth/register", new
        {
            email,
            role,
            permissionIds,
            profileType,
            profileId
        }, Admin)).Expect(201);

        var password = registered["temporaryPassword"].GetValue<string>();

        return new TestUser(registered["userId"].GetValue<Guid>(), email, password, await LoginAsync(email, password));
    }

    public async Task<string> LoginAsync(string email, string password) =>
        (await Api.PostAsync("/api/auth/login", new { email, password })).Expect(200)["accessToken"].GetValue<string>();

    public Task<TestUser> TeacherUserAsync(Guid teacherId) => UserAsync("Teacher", "Teacher", teacherId);

    public Task<TestUser> StudentUserAsync(Guid studentId) => UserAsync("Student", "Student", studentId);

    public async Task<List<Guid>> PermissionIdsAsync(params string[] names)
    {
        var all = (await Api.GetAsync("/api/auth/permissions", Admin)).Expect(200).Json!.AsArray();

        return all.Where(p => names.Contains(p!["name"]!.GetValue<string>()))
            .Select(p => p!["id"]!.GetValue<Guid>())
            .ToList();
    }
}

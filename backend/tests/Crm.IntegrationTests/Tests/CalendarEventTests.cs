namespace Crm.IntegrationTests.Tests;

// Arbitrary calendar entries that are not lessons: a personal reminder only its owner sees,
// or (for a CanManageSchedule holder) a school-wide notice everyone sees.
public class CalendarEventTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static readonly DateTime Start = DateTime.UtcNow.AddDays(5).Date.AddHours(9);
    private static readonly DateTime End = Start.AddHours(1);

    private static object Personal(string title = "Dentist", DateTime? start = null, DateTime? end = null) => new
    {
        visibility = "Personal", title, description = (string?)null, startsAt = start ?? Start, endsAt = end ?? End, isAllDay = false
    };

    private static object Everyone(string title = "School closed") => new
    {
        visibility = "Everyone", title, description = "No lessons that day", startsAt = Start, endsAt = End, isAllDay = true
    };

    private Task<ApiResponse> CreateAsync(object body, string? token = null) => Api.PostAsync("/api/calendar/events", body, token);

    private Task<ApiResponse> ListAsync(string? token = null) =>
        Api.GetAsync($"/api/calendar/events?from={Start.AddDays(-1):yyyy-MM-dd}&to={Start.AddDays(1):yyyy-MM-dd}", token);

    private async Task<string> ManagerTokenAsync() =>
        (await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanManageSchedule"))).Token;

    // ------------------------------------------------------------------ anyone can keep a personal reminder
    [Fact]
    public async Task Any_authenticated_user_can_create_and_read_back_their_own_personal_event()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        var created = (await CreateAsync(Personal(), student.Token)).Expect(201);

        Assert.Equal("Personal", created["visibility"].GetValue<string>());
        Assert.Equal("Dentist", created["title"].GetValue<string>());
        Assert.True(created["isMine"].GetValue<bool>());
        var id = created.Id;

        var fetched = (await Api.GetAsync($"/api/calendar/events/{id}", student.Token)).Expect(200);
        Assert.Equal("Dentist", fetched["title"].GetValue<string>());
    }

    [Fact]
    public async Task A_teacher_and_the_super_admin_can_also_keep_personal_events()
    {
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        (await CreateAsync(Personal("Prepare materials"), teacher.Token)).Expect(201);
        (await CreateAsync(Personal("Reminder"), Admin)).Expect(201);
    }

    // ------------------------------------------------------------------ only a schedule manager posts to everyone
    [Fact]
    public async Task Creating_an_event_for_everyone_needs_CanManageSchedule()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var plainAdmin = await Data.UserAsync("Admin");
        var manager = await ManagerTokenAsync();

        (await CreateAsync(Everyone(), student.Token)).Expect(403);
        (await CreateAsync(Everyone(), plainAdmin.Token)).Expect(403);

        var created = (await CreateAsync(Everyone(), manager)).Expect(201);
        Assert.Equal("Everyone", created["visibility"].GetValue<string>());
    }

    // ------------------------------------------------------------------ visibility
    [Fact]
    public async Task The_list_shows_my_own_events_and_everyones_but_not_someone_elses_personal_ones()
    {
        var mine = await Data.UserAsync("Admin");
        var other = await Data.UserAsync("Admin");
        var manager = await ManagerTokenAsync();

        var myEvent = (await CreateAsync(Personal("Mine"), mine.Token)).Expect(201).Id;
        var otherEvent = (await CreateAsync(Personal("Theirs"), other.Token)).Expect(201).Id;
        var everyoneEvent = (await CreateAsync(Everyone(), manager)).Expect(201).Id;

        var list = (await ListAsync(mine.Token)).Expect(200).Json!.AsArray();
        var ids = list.Select(i => i!["id"]!.GetValue<Guid>()).ToList();

        Assert.Contains(myEvent, ids);
        Assert.Contains(everyoneEvent, ids);
        Assert.DoesNotContain(otherEvent, ids);

        var mineRow = list.Single(i => i!["id"]!.GetValue<Guid>() == myEvent)!;
        Assert.True(mineRow["isMine"]!.GetValue<bool>());
        var everyoneRow = list.Single(i => i!["id"]!.GetValue<Guid>() == everyoneEvent)!;
        Assert.False(everyoneRow["isMine"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Someone_elses_personal_event_does_not_exist_as_far_as_you_are_concerned()
    {
        var owner = await Data.UserAsync("Admin");
        var other = await Data.UserAsync("Admin");
        var manager = await ManagerTokenAsync();   // even a schedule manager cannot see someone's personal reminder
        var id = (await CreateAsync(Personal(), owner.Token)).Expect(201).Id;

        (await Api.GetAsync($"/api/calendar/events/{id}", other.Token)).Expect(404);
        (await Api.GetAsync($"/api/calendar/events/{id}", manager)).Expect(404);
        (await Api.GetAsync($"/api/calendar/events/{Guid.NewGuid()}", owner.Token)).Expect(404);
    }

    [Fact]
    public async Task Events_outside_the_requested_range_are_left_out()
    {
        var user = await Data.UserAsync("Admin");
        await CreateAsync(Personal("In range"), user.Token);
        await CreateAsync(Personal("Far away", start: Start.AddDays(60), end: Start.AddDays(60).AddHours(1)), user.Token);

        var list = (await ListAsync(user.Token)).Expect(200).Json!.AsArray();

        Assert.Single(list, i => i!["title"]!.GetValue<string>() == "In range");
    }

    // ------------------------------------------------------------------ updating and deleting
    [Fact]
    public async Task The_owner_updates_their_own_personal_event_and_anyone_else_gets_404()
    {
        var owner = await Data.UserAsync("Admin");
        var other = await Data.UserAsync("Admin");
        var id = (await CreateAsync(Personal(), owner.Token)).Expect(201).Id;
        var update = Personal("Updated title", start: Start.AddHours(2), end: Start.AddHours(3));

        (await Api.PutAsync($"/api/calendar/events/{id}", update, other.Token)).Expect(404);

        var updated = (await Api.PutAsync($"/api/calendar/events/{id}", update, owner.Token)).Expect(200);
        Assert.Equal("Updated title", updated["title"].GetValue<string>());
    }

    [Fact]
    public async Task Turning_a_personal_event_into_one_for_everyone_still_needs_the_permission()
    {
        var owner = await Data.UserAsync("Admin");
        var id = (await CreateAsync(Personal(), owner.Token)).Expect(201).Id;

        (await Api.PutAsync($"/api/calendar/events/{id}", Everyone(), owner.Token)).Expect(403);
    }

    [Fact]
    public async Task An_event_for_everyone_is_managed_by_any_schedule_manager_not_only_its_creator()
    {
        var creator = await ManagerTokenAsync();
        var anotherManager = await ManagerTokenAsync();
        var plainAdmin = await Data.UserAsync("Admin");
        var id = (await CreateAsync(Everyone(), creator)).Expect(201).Id;
        var update = Everyone("Updated notice");

        (await Api.PutAsync($"/api/calendar/events/{id}", update, plainAdmin.Token)).Expect(403);
        (await Api.PutAsync($"/api/calendar/events/{id}", update, anotherManager)).Expect(200);
    }

    [Fact]
    public async Task Deleting_follows_the_same_rule_and_the_event_is_gone_afterward()
    {
        var owner = await Data.UserAsync("Admin");
        var other = await Data.UserAsync("Admin");
        var id = (await CreateAsync(Personal(), owner.Token)).Expect(201).Id;

        (await Api.DeleteAsync($"/api/calendar/events/{id}", null, other.Token)).Expect(404);
        (await Api.DeleteAsync($"/api/calendar/events/{id}", null, owner.Token)).Expect(204);
        (await Api.GetAsync($"/api/calendar/events/{id}", owner.Token)).Expect(404);
        (await Api.DeleteAsync($"/api/calendar/events/{id}", null, owner.Token)).Expect(404);
    }

    // ------------------------------------------------------------------ validation and authentication
    [Fact]
    public async Task An_event_cannot_end_before_it_starts_and_needs_a_title()
    {
        var user = await Data.UserAsync("Admin");

        (await CreateAsync(Personal(start: End, end: Start), user.Token)).Expect(400);
        (await CreateAsync(Personal(title: ""), user.Token)).Expect(400);
        (await CreateAsync(Personal(title: new string('a', 201)), user.Token)).Expect(400);
    }

    [Fact]
    public async Task Every_endpoint_needs_a_token()
    {
        (await CreateAsync(Personal())).Expect(401);
        (await ListAsync()).Expect(401);
        (await Api.GetAsync($"/api/calendar/events/{Guid.NewGuid()}")).Expect(401);
        (await Api.PutAsync($"/api/calendar/events/{Guid.NewGuid()}", Personal())).Expect(401);
        (await Api.DeleteAsync($"/api/calendar/events/{Guid.NewGuid()}", null)).Expect(401);
    }
}

namespace Crm.IntegrationTests.Infrastructure;

// Base class of every API test: one shared application and database, a client, an admin token and a data builder
[Collection("Api")]
public abstract class ApiTest(CrmApiFactory factory) : IAsyncLifetime
{
    protected CrmApiFactory Factory { get; } = factory;

    protected Api Api { get; private set; } = null!;
    protected TestData Data { get; private set; } = null!;
    protected string Admin { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Api = new Api(Factory.CreateClient());

        var login = (await Api.PostAsync("/api/auth/login", new { email = CrmApiFactory.AdminEmail, password = CrmApiFactory.AdminPassword })).Expect(200);
        Admin = login["accessToken"].GetValue<string>();
        Data = new TestData(Api, Admin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected Task<string?> Sql(string sql) => Factory.Database.ScalarAsync(sql);

    protected static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");
}

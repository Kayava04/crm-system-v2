using Host.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddLogging();

builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

app.Configure();

app.Run();

// Lets the integration tests start the whole application
public partial class Program;

using Host.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddLogging();

builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

app.Configure();

app.Run();

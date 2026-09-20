using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Application.Features.CancelSchedule;
using Scheduling.Application.Features.CompleteSchedule;
using Scheduling.Application.Features.CreateSchedule;
using Scheduling.Application.Features.GetScheduleById;
using Scheduling.Application.Features.GetSchedules;
using Scheduling.Application.Features.RescheduleSchedule;
using Scheduling.Application.Features.UpdateSchedule;
using Scheduling.Contracts;

namespace Scheduling.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        services.AddScoped<IScheduleLookup, ScheduleLookupService>();

        return services;
    }

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedules")
                       .WithTags("Schedules");

        GetAllEndpoint.Map(group);
        GetByIdEndpoint.Map(group);
        CreateEndpoint.Map(group);
        UpdateEndpoint.Map(group);

        CompleteScheduleEndpoint.Map(group);
        CancelScheduleEndpoint.Map(group);
        RescheduleScheduleEndpoint.Map(group);

        return app;
    }
}

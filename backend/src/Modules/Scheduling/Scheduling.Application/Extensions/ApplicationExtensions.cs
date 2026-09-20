using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Application.Features.AddGroupMember;
using Scheduling.Application.Features.CancelFutureSchedule;
using Scheduling.Application.Features.CancelSchedule;
using Scheduling.Application.Features.CompleteSchedule;
using Scheduling.Application.Features.CreateGroup;
using Scheduling.Application.Features.CreateSchedule;
using Scheduling.Application.Features.GenerateSchedule;
using Scheduling.Application.Features.GetGroupById;
using Scheduling.Application.Features.GetGroups;
using Scheduling.Application.Features.GetMyCalendar;
using Scheduling.Application.Features.GetScheduleById;
using Scheduling.Application.Features.GetSchedules;
using Scheduling.Application.Features.RemoveGroupMember;
using Scheduling.Application.Features.RescheduleSchedule;
using Scheduling.Application.Features.UpdateGroup;
using Scheduling.Application.Features.UpdateSchedule;
using Scheduling.Application.Services;
using Scheduling.Contracts;

namespace Scheduling.Application.Extensions;

public static class ApplicationExtensions
{
    private const string DefaultTimeZone = "Europe/Kyiv";

    public static IServiceCollection AddSchedulingApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        services.AddScoped<IScheduleLookup, ScheduleLookupService>();
        services.AddScoped<LessonTargetResolver>();

        // Lessons are agreed in one school time zone (Scheduling:TimeZone) and stored in UTC
        services.AddSingleton<ISchoolClock>(sp =>
            new SchoolClock(sp.GetRequiredService<IConfiguration>()["Scheduling:TimeZone"] ?? DefaultTimeZone));

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

        GenerateScheduleEndpoint.Map(group);
        CancelFutureScheduleEndpoint.Map(group);

        CompleteScheduleEndpoint.Map(group);
        CancelScheduleEndpoint.Map(group);
        RescheduleScheduleEndpoint.Map(group);

        var groups = app.MapGroup("/api/study-groups")
                        .WithTags("Study groups");

        GetGroupsEndpoint.Map(groups);
        GetGroupByIdEndpoint.Map(groups);
        CreateGroupEndpoint.Map(groups);
        UpdateGroupEndpoint.Map(groups);
        AddGroupMemberEndpoint.Map(groups);
        RemoveGroupMemberEndpoint.Map(groups);

        var calendar = app.MapGroup("/api/calendar")
                          .WithTags("Calendar");

        GetMyCalendarEndpoint.Map(calendar);

        return app;
    }
}

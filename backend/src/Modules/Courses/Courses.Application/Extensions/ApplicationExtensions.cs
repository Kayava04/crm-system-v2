using Courses.Application.Features.ActivateCourse;
using Courses.Application.Features.ArchiveCourse;
using Courses.Application.Features.CreateCourse;
using Courses.Application.Features.DeleteCourse;
using Courses.Application.Features.GetCourseById;
using Courses.Application.Features.GetCourses;
using Courses.Application.Features.UpdateCourse;
using Courses.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Courses.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddCoursesApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        services.AddScoped<ICourseLookup, CourseLookupService>();

        return services;
    }

    public static IEndpointRouteBuilder MapCoursesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses")
                       .WithTags("Courses");

        GetAllEndpoint.Map(group);
        GetByIdEndpoint.Map(group);
        CreateEndpoint.Map(group);
        UpdateEndpoint.Map(group);
        DeleteEndpoint.Map(group);

        ArchiveCourseEndpoint.Map(group);
        ActivateCourseEndpoint.Map(group);

        return app;
    }
}

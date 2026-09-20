using FluentValidation;
using Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Students.Application.Features.AddParentInfo;
using Students.Application.Features.BulkCreateStudents;
using Students.Application.Features.BulkDeleteStudents;
using Students.Application.Features.ChangeStudentStatus;
using Students.Application.Features.CreateStudent;
using Students.Application.Features.DeleteParentInfo;
using Students.Application.Features.DeleteStudent;
using Students.Application.Features.ExportStudents;
using Students.Application.Features.GetMyStudentProfile;
using Students.Application.Features.GetStudentById;
using Students.Application.Features.GetStudents;
using Students.Application.Features.ImportStudents;
using Students.Application.Features.StudentImportTemplate;
using Students.Application.Features.UpdateComment;
using Students.Application.Features.UpdateParentInfo;
using Students.Application.Features.UpdateStudent;
using Students.Application.Features.UpdateStudentPreferences;
using Students.Application.Services;
using Students.Contracts;

namespace Students.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddStudentsApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        // Link user account to student
        services.AddScoped<IProfileLinker, StudentAccountLinker>();

        services.AddScoped<IStudentVerifier, StudentVerifierService>();

        services.AddScoped<IStudentLookup, StudentLookupService>();
        services.AddScoped<StudentBulkCreator>();

        services.AddScoped<IStudentStatistics, StudentStatisticsService>();

        return services;
    }

    public static IEndpointRouteBuilder MapStudentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
                       .WithTags("Students");

        GetAllEndpoint.Map(group);
        GetMyStudentProfileEndpoint.Map(group);
        GetByIdEndpoint.Map(group);
        CreateEndpoint.Map(group);
        UpdateEndpoint.Map(group);
        DeleteEndpoint.Map(group);

        BulkCreateStudentsEndpoint.Map(group);

        ExportStudentsEndpoint.Map(group);
        StudentImportTemplateEndpoint.Map(group);
        ImportStudentsEndpoint.Map(group);
        BulkDeleteStudentsEndpoint.Map(group);

        UpdatePreferencesEndpoint.Map(group);

        AddParentInfoEndpoint.Map(group);
        UpdateParentInfoEndpoint.Map(group);
        DeleteParentInfoEndpoint.Map(group);

        ChangeStudentStatusEndpoint.Map(group);

        UpdateCommentEndpoint.Map(group);

        return app;
    }
}

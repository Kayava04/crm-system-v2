using FluentValidation;
using Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Teachers.Application.Features.AddSalaryRate;
using Teachers.Application.Features.BulkCreateTeachers;
using Teachers.Application.Features.BulkDeleteTeachers;
using Teachers.Application.Features.ChangeTeacherStatus;
using Teachers.Application.Features.CreateTeacher;
using Teachers.Application.Features.DeleteTeacher;
using Teachers.Application.Features.ExportTeachers;
using Teachers.Application.Features.GetTeacherById;
using Teachers.Application.Features.GetTeacherPhoto;
using Teachers.Application.Features.GetMyStudents;
using Teachers.Application.Features.GetMyTeacherProfile;
using Teachers.Application.Features.GetTeachers;
using Teachers.Application.Features.ImportTeachers;
using Teachers.Application.Features.TeacherImportTemplate;
using Teachers.Application.Features.UpdateComment;
using Teachers.Application.Features.UpdateTeacher;
using Teachers.Application.Services;
using Teachers.Contracts;

namespace Teachers.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddTeachersApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        // Link user account to teacher
        services.AddScoped<IProfileLinker, TeacherAccountLinker>();

        services.AddScoped<ITeacherVerifier, TeacherVerifierService>();

        services.AddScoped<ITeacherLookup, TeacherLookupService>();
        services.AddScoped<TeacherBulkCreator>();

        services.AddScoped<ITeacherStatistics, TeacherStatisticsService>();

        return services;
    }

    public static IEndpointRouteBuilder MapTeachersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teachers")
                       .WithTags("Teachers");

        GetAllEndpoint.Map(group);
        GetMyStudentsEndpoint.Map(group);
        GetMyTeacherProfileEndpoint.Map(group);
        GetTeacherPhotoEndpoint.Map(group);
        GetByIdEndpoint.Map(group);
        CreateEndpoint.Map(group);
        UpdateEndpoint.Map(group);
        DeleteEndpoint.Map(group);

        BulkCreateTeachersEndpoint.Map(group);

        ExportTeachersEndpoint.Map(group);
        TeacherImportTemplateEndpoint.Map(group);
        ImportTeachersEndpoint.Map(group);
        BulkDeleteTeachersEndpoint.Map(group);

        ChangeTeacherStatusEndpoint.Map(group);

        AddSalaryRateEndpoint.Map(group);

        UpdateCommentEndpoint.Map(group);

        return app;
    }
}

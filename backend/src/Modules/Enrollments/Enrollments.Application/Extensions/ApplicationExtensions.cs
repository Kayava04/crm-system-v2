using Enrollments.Application.Features.ActivateEnrollment;
using Enrollments.Application.Features.ApplyDiscount;
using Enrollments.Application.Features.CompleteEnrollment;
using Enrollments.Application.Features.CreateEnrollment;
using Enrollments.Application.Features.GetEnrollmentById;
using Enrollments.Application.Features.GetEnrollments;
using Enrollments.Application.Features.RemoveDiscount;
using Enrollments.Application.Features.SuspendEnrollment;
using Enrollments.Application.Features.TerminateEnrollment;
using Enrollments.Application.Features.UpdateEnrollmentComment;
using Enrollments.Application.Features.UpdateEnrollmentPrice;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Enrollments.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddEnrollmentsApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapEnrollmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enrollments")
                       .WithTags("Enrollments");

        GetAllEndpoint.Map(group);
        GetByIdEndpoint.Map(group);
        CreateEndpoint.Map(group);

        ActivateEnrollmentEndpoint.Map(group);
        SuspendEnrollmentEndpoint.Map(group);
        CompleteEnrollmentEndpoint.Map(group);
        TerminateEnrollmentEndpoint.Map(group);

        ApplyDiscountEndpoint.Map(group);
        RemoveDiscountEndpoint.Map(group);

        UpdateEnrollmentPriceEndpoint.Map(group);

        UpdateEnrollmentCommentEndpoint.Map(group);

        return app;
    }
}

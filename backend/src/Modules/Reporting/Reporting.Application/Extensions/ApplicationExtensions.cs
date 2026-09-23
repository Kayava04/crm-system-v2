using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Reporting.Application.Features.BillingSummary;
using Reporting.Application.Features.EnrollmentsSummary;
using Reporting.Application.Features.StudentsSummary;
using Reporting.Application.Features.TeachersSummary;

namespace Reporting.Application.Extensions;

public static class ApplicationExtensions
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
                       .WithTags("Reporting");

        StudentsSummaryEndpoint.Map(group);
        TeachersSummaryEndpoint.Map(group);
        EnrollmentsSummaryEndpoint.Map(group);
        BillingSummaryEndpoint.Map(group);

        return app;
    }
}

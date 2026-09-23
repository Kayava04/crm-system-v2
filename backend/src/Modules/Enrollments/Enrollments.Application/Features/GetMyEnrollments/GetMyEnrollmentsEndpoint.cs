using System.Security.Claims;
using Courses.Contracts;
using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Students.Contracts;

namespace Enrollments.Application.Features.GetMyEnrollments;

public sealed record MyEnrollmentResponse(
    Guid Id,
    string EnrollmentNumber,
    Guid CourseId,
    string CourseName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal EffectivePrice,
    EnrollmentStatus Status
);

public static class GetMyEnrollmentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/my", Handle)
             .RequireAuthorization(policy => policy.RequireRole(nameof(SystemRole.Student)))
             .WithName("GetMyEnrollments")
             .WithSummary("The enrollments (courses) of the current student, newest first")
             .Produces<IReadOnlyList<MyEnrollmentResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        IEnrollmentRepository repository,
        IStudentLookup studentLookup,
        ICourseLookup courseLookup,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var student = await studentLookup.GetByUserIdAsync(userId, ct);
        if (student is null)
            return Results.Problem(detail: "Student profile is not linked to this account.", statusCode: StatusCodes.Status404NotFound);

        var enrollments = await repository.GetByStudentAsync(student.Id, ct);
        var courses = (await courseLookup.GetByIdsAsync(enrollments.Select(e => e.CourseId).Distinct().ToList(), ct))
            .ToDictionary(c => c.Id);

        var response = enrollments
            .OrderByDescending(e => e.StartDate)
            .Select(e => new MyEnrollmentResponse(
                e.Id,
                e.EnrollmentNumber,
                e.CourseId,
                courses.GetValueOrDefault(e.CourseId)?.Name ?? "Unknown course",
                e.StartDate,
                e.EndDate,
                e.EffectivePrice,
                e.Status))
            .ToList();

        return Results.Ok(response);
    }
}

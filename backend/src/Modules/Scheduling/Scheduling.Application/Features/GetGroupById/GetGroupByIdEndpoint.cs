using Enrollments.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Students.Contracts;

namespace Scheduling.Application.Features.GetGroupById;

public sealed record GroupMemberResponse(
    Guid EnrollmentId,
    Guid StudentId,
    string StudentName,
    DateTime JoinedAt
);

public sealed record GroupDetailResponse(
    Guid Id,
    Guid CourseId,
    Guid TeacherId,
    string Name,
    IReadOnlyList<GroupMemberResponse> Members,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class GetGroupByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewSchedule))
             .WithName("GetStudyGroupById")
             .WithSummary("Get study group with its members")
             .Produces<GroupDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudyGroupRepository repository,
        IEnrollmentLookup enrollmentLookup,
        IStudentLookup studentLookup,
        CancellationToken ct
    )
    {
        var studyGroup = await repository.GetByIdAsync(id, ct);
        if (studyGroup is null)
            return Results.Problem(
                detail: $"Study group with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var enrollments = await enrollmentLookup.GetByIdsAsync(
            studyGroup.Members.Select(m => m.EnrollmentId).ToList(), ct);
        var students = await studentLookup.GetByIdsAsync(
            enrollments.Select(e => e.StudentId).Distinct().ToList(), ct);

        var members = studyGroup.Members
            .Select(m =>
            {
                var enrollment = enrollments.FirstOrDefault(e => e.Id == m.EnrollmentId);
                var student = enrollment is null ? null : students.FirstOrDefault(s => s.Id == enrollment.StudentId);

                return new GroupMemberResponse(
                    m.EnrollmentId,
                    enrollment?.StudentId ?? Guid.Empty,
                    student?.FullName ?? "Unknown student",
                    m.JoinedAt);
            })
            .OrderBy(m => m.StudentName)
            .ToList();

        var response = new GroupDetailResponse(
            studyGroup.Id,
            studyGroup.CourseId,
            studyGroup.TeacherId,
            studyGroup.Name,
            members,
            studyGroup.CreatedAt,
            studyGroup.UpdatedAt
        );

        return Results.Ok(response);
    }
}

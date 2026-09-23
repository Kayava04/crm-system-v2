using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Features.RemoveGroupMember;

public sealed record RemoveGroupMemberRequest;

public static class RemoveGroupMemberEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}/members/{enrollmentId:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("RemoveStudyGroupMember")
             .WithSummary("Remove an enrollment from a study group")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        Guid enrollmentId,
        IStudyGroupRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<RemoveGroupMemberRequest> logger,
        CancellationToken ct
    )
    {
        var studyGroup = await repository.GetByIdAsync(id, ct);
        if (studyGroup is null)
            return Results.Problem(
                detail: $"Study group with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var member = studyGroup.RemoveMember(enrollmentId);
        if (member is null)
            return Results.Problem(
                detail: $"Enrollment '{enrollmentId}' is not a member of this group.",
                statusCode: StatusCodes.Status404NotFound
            );

        await repository.RemoveMemberAsync(member, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment {EnrollmentId} removed from group {GroupId}", enrollmentId, id);

        return Results.NoContent();
    }
}

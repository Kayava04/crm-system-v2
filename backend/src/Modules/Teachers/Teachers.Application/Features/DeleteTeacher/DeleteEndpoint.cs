using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Teachers.Application.Abstractions;

namespace Teachers.Application.Features.DeleteTeacher;

public sealed record DeleteRequest;

public static class DeleteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", Handle)
             .WithName("DeleteTeacher")
             .RequireAuthorization(nameof(SystemPermission.CanDeleteTeachers))
             .WithSummary("Delete teacher")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        IScheduleLookup scheduleLookup,
        ILogger<DeleteRequest> logger,
        CancellationToken ct
    )
    {
        var teacher = await repository.GetByIdAsync(id, ct);
        if (teacher is null)
            return Results.Problem(
                detail: $"Teacher with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        // A teacher with history is never erased: set the status to Resigned or Dismissed instead, it can be undone
        var hasHistory = teacher.UserId is not null
            || await scheduleLookup.HasTeacherHistoryAsync(id, ct);

        if (hasHistory)
        {
            logger.LogWarning("Delete refused for teacher {TeacherId}: has an account, lessons or groups", id);

            return Results.Problem(
                detail: "The teacher has an account, lessons or groups and cannot be deleted. Change the status to Resigned or Dismissed instead.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        await repository.DeleteAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher deleted: {TeacherId}", id);

        return Results.NoContent();
    }
}

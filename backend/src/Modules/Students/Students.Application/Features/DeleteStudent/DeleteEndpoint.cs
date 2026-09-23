using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Enrollments.Contracts;
using Microsoft.Extensions.Logging;
using Students.Application.Abstractions;

namespace Students.Application.Features.DeleteStudent;

public sealed record DeleteRequest;

public static class DeleteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", Handle)
             .WithName("DeleteStudent")
             .RequireAuthorization(nameof(SystemPermission.CanDeleteStudents))
             .WithSummary("Delete student")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        IEnrollmentLookup enrollmentLookup,
        ILogger<DeleteRequest> logger,
        CancellationToken ct
    )
    {
        var student = await repository.GetByIdAsync(id, ct);
        if (student is null)
            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        // A student with history is never erased: set the status to Withdrawn instead, it can be undone
        var hasHistory = student.UserId is not null
            || (await enrollmentLookup.GetIdsByStudentAsync(id, ct)).Count > 0;

        if (hasHistory)
        {
            logger.LogWarning("Delete refused for student {StudentId}: has an account or enrollments", id);

            return Results.Problem(
                detail: "The student has an account or enrollments and cannot be deleted. Change the status to Withdrawn instead.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        await repository.DeleteAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Student deleted: {StudentId}", id);

        return Results.NoContent();
    }
}

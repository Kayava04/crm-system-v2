using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Students.Application.Abstractions;

namespace Students.Application.Features.DeleteParentInfo;

public sealed record DeleteParentInfoRequest(Guid Id);

public static class DeleteParentInfoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}/parent-info", Handle)
             .WithName("DeleteParentInfo")
             .RequireAuthorization(nameof(SystemPermission.CanManageStudents))
             .WithSummary("Delete parent info for student")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        ILogger<DeleteParentInfoRequest> logger,
        CancellationToken ct
    )
    {
        var student = await repository.GetByIdAsync(id, ct);
        if (student is null)
            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (student.ParentInfo is null)
        {
            logger.LogWarning("Parent info not found for student {StudentId}", id);

            return Results.Problem(
                detail: "Parent info not found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        student.RemoveParentInfo();

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Parent info deleted for student {StudentId}", id);

        return Results.NoContent();
    }
}

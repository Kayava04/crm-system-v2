using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
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

        await repository.DeleteAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Student deleted: {StudentId}", id);

        return Results.NoContent();
    }
}

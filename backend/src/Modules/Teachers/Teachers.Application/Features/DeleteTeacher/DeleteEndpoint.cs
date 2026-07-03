using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
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

        await repository.DeleteAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher deleted: {TeacherId}", id);

        return Results.NoContent();
    }
}

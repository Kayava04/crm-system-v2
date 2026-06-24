using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Abstractions;
using Students.Application.Abstractions;

namespace Students.Application.Features.DeleteParentInfo;

public static class DeleteParentInfoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}/parent-info", Handle)
             .WithName("DeleteParentInfo")
             .WithSummary("Delete parent info for student")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentRepository repository,
        IUnitOfWork unitOfWork,
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
            return Results.Problem(
                detail: "Parent info not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        student.RemoveParentInfo();

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}

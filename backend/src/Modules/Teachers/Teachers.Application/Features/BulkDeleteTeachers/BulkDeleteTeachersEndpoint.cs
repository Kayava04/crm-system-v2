using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Shared.Kernel.Common;
using Teachers.Application.Abstractions;

namespace Teachers.Application.Features.BulkDeleteTeachers;

public sealed record BulkDeleteTeachersRequest(List<Guid> Ids);

public sealed class BulkDeleteTeachersValidator : AbstractValidator<BulkDeleteTeachersRequest>
{
    public const int MaxItems = 500;

    public BulkDeleteTeachersValidator()
    {
        RuleFor(x => x.Ids)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one id is required.")
            .Must(ids => ids.Count <= MaxItems).WithMessage($"At most {MaxItems} ids per request.");
    }
}

public static class BulkDeleteTeachersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/bulk", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanDeleteTeachers))
             .WithName("BulkDeleteTeachers")
             .WithSummary("Delete many teachers; teachers with an account, lessons or groups are refused (deactivate them instead)")
             .Produces<BulkOperationResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        [FromBody] BulkDeleteTeachersRequest request,
        IValidator<BulkDeleteTeachersRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        IScheduleLookup scheduleLookup,
        ILogger<BulkDeleteTeachersRequest> logger,
        CancellationToken ct
    )
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var teachers = (await repository.GetByIdsForUpdateAsync(request.Ids.Distinct().ToList(), ct))
            .ToDictionary(t => t.Id);

        var results = new List<BulkItemResult>();
        var deleted = 0;

        for (var i = 0; i < request.Ids.Count; i++)
        {
            var id = request.Ids[i];

            if (request.Ids.IndexOf(id) != i)
            {
                results.Add(new BulkItemResult(i, false, id, null, ["Id is repeated in this request."]));
                continue;
            }

            if (!teachers.TryGetValue(id, out var teacher))
            {
                results.Add(new BulkItemResult(i, false, id, null, [$"Teacher with id '{id}' not found."]));
                continue;
            }

            // Same rule as the single delete: a teacher with history is deactivated, never erased
            if (teacher.UserId is not null || await scheduleLookup.HasTeacherHistoryAsync(id, ct))
            {
                results.Add(new BulkItemResult(i, false, id, teacher.Email,
                    ["The teacher has an account, lessons or groups and cannot be deleted. Change the status to Resigned or Dismissed instead."]));
                continue;
            }

            await repository.DeleteAsync(teacher, ct);
            deleted++;
            results.Add(new BulkItemResult(i, true, id, teacher.Email, []));
        }

        if (deleted > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Bulk teacher deletion: {Deleted} of {Total} deleted", deleted, request.Ids.Count);

        return Results.Ok(BulkOperationResponse.From(results));
    }
}

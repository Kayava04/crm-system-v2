using Enrollments.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Common;
using Students.Application.Abstractions;

namespace Students.Application.Features.BulkDeleteStudents;

public sealed record BulkDeleteStudentsRequest(List<Guid> Ids);

public sealed class BulkDeleteStudentsValidator : AbstractValidator<BulkDeleteStudentsRequest>
{
    public const int MaxItems = 500;

    public BulkDeleteStudentsValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("At least one id is required.")
            .Must(ids => ids.Count <= MaxItems).WithMessage($"At most {MaxItems} ids per request.");
    }
}

public static class BulkDeleteStudentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/bulk", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanDeleteStudents))
             .WithName("BulkDeleteStudents")
             .WithSummary("Delete many students; students with an account or enrollments are refused (deactivate them instead)")
             .Produces<BulkOperationResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        [FromBody] BulkDeleteStudentsRequest request,
        IValidator<BulkDeleteStudentsRequest> validator,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        IEnrollmentLookup enrollmentLookup,
        ILogger<BulkDeleteStudentsRequest> logger,
        CancellationToken ct
    )
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var ids = request.Ids.Distinct().ToList();
        var students = (await repository.GetByIdsForUpdateAsync(ids, ct)).ToDictionary(s => s.Id);
        var withEnrollments = await enrollmentLookup.GetStudentIdsWithEnrollmentsAsync(ids, ct);

        var results = new List<BulkItemResult>();
        var deleted = 0;

        for (var i = 0; i < request.Ids.Count; i++)
        {
            var id = request.Ids[i];

            if (i > 0 && request.Ids.IndexOf(id) != i)
            {
                results.Add(new BulkItemResult(i, false, id, null, ["Id is repeated in this request."]));
                continue;
            }

            if (!students.TryGetValue(id, out var student))
            {
                results.Add(new BulkItemResult(i, false, id, null, [$"Student with id '{id}' not found."]));
                continue;
            }

            // Same rule as the single delete: a student with history is deactivated (Withdrawn), never erased
            if (student.UserId is not null || withEnrollments.Contains(id))
            {
                results.Add(new BulkItemResult(i, false, id, student.Email,
                    ["The student has an account or enrollments and cannot be deleted. Change the status to Withdrawn instead."]));
                continue;
            }

            await repository.DeleteAsync(student, ct);
            deleted++;
            results.Add(new BulkItemResult(i, true, id, student.Email, []));
        }

        if (deleted > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Bulk student deletion: {Deleted} of {Total} deleted", deleted, request.Ids.Count);

        return Results.Ok(BulkOperationResponse.From(results));
    }
}

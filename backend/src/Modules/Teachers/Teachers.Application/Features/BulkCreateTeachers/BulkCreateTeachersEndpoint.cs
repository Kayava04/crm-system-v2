using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Common;
using Teachers.Application.Abstractions;
using Teachers.Application.Features.CreateTeacher;
using Teachers.Application.Services;
using Teachers.Domain.Entities;

namespace Teachers.Application.Features.BulkCreateTeachers;

// AllOrNothing = true: if any teacher is invalid nothing is saved. Otherwise the valid ones are saved and the rest reported.
public sealed record BulkCreateTeachersRequest(
    List<CreateTeacherRequest> Teachers,
    bool AllOrNothing = false
);

public sealed class BulkCreateTeachersValidator : AbstractValidator<BulkCreateTeachersRequest>
{
    public const int MaxItems = 500;

    public BulkCreateTeachersValidator()
    {
        RuleFor(x => x.Teachers)
            .NotEmpty().WithMessage("At least one teacher is required.")
            .Must(t => t.Count <= MaxItems).WithMessage($"At most {MaxItems} teachers per request.");
    }
}

public static class BulkCreateTeachersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/bulk", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateTeachers))
             .WithName("BulkCreateTeachers")
             .WithSummary("Create many teachers at once; every item gets its own result")
             .Produces<BulkOperationResponse>(StatusCodes.Status200OK)
             .Produces<BulkOperationResponse>(StatusCodes.Status400BadRequest)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        BulkCreateTeachersRequest request,
        IValidator<BulkCreateTeachersRequest> requestValidator,
        IValidator<CreateTeacherRequest> itemValidator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        ILogger<BulkCreateTeachersRequest> logger,
        CancellationToken ct
    )
    {
        var requestValidation = await requestValidator.ValidateAsync(request, ct);
        if (!requestValidation.IsValid)
            return Results.ValidationProblem(requestValidation.ToDictionary());

        var items = request.Teachers;
        var existingEmails = await repository.GetExistingEmailsAsync(
            items.Select(t => t?.Email ?? string.Empty).Where(e => e.Length > 0).ToList(), ct);

        var errors = new List<string>[items.Count];
        var seenEmails = new HashSet<string>();

        for (var i = 0; i < items.Count; i++)
        {
            errors[i] = [];

            if (items[i] is null)
            {
                errors[i].Add("Teacher data is missing.");
                continue;
            }

            var validation = await itemValidator.ValidateAsync(items[i], ct);
            errors[i].AddRange(validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

            var email = items[i].Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
                continue;

            if (existingEmails.Contains(email))
                errors[i].Add($"Email: Teacher with email '{items[i].Email}' already exists.");
            else if (!seenEmails.Add(email))
                errors[i].Add($"Email: Email '{items[i].Email}' is repeated in this request.");
        }

        var anyInvalid = errors.Any(e => e.Count > 0);

        if (anyInvalid && request.AllOrNothing)
        {
            logger.LogWarning("Bulk teacher creation refused: {Invalid} of {Total} teachers are invalid",
                errors.Count(e => e.Count > 0), items.Count);

            // Valid items were not saved either; say so instead of leaving them without an explanation
            const string notSaved = "Not saved: the request is all-or-nothing and other items are invalid.";

            return Results.Json(
                BulkOperationResponse.From(items
                    .Select((t, i) => Result(i, t, null, errors[i].Count > 0 ? errors[i] : [notSaved]))
                    .ToList()),
                statusCode: StatusCodes.Status400BadRequest);
        }

        var created = new Dictionary<int, Teacher>();

        for (var i = 0; i < items.Count; i++)
        {
            if (errors[i].Count > 0)
                continue;

            var teacher = TeacherFactory.Build(items[i]);
            created[i] = teacher;
            await repository.AddAsync(teacher, ct);
        }

        if (created.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Bulk teacher creation: {Created} of {Total} created", created.Count, items.Count);

        var results = items
            .Select((t, i) => Result(i, t, created.GetValueOrDefault(i)?.Id, errors[i]))
            .ToList();

        return Results.Ok(BulkOperationResponse.From(results));
    }

    private static BulkItemResult Result(int index, CreateTeacherRequest? item, Guid? id, List<string> errors) =>
        new(index, errors.Count == 0 && id is not null, id, item?.Email, errors);
}

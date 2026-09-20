using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Common;
using Students.Application.Abstractions;
using Students.Application.Features.CreateStudent;
using Students.Application.Services;
using Students.Domain.Entities;

namespace Students.Application.Features.BulkCreateStudents;

// AllOrNothing = true: if any student is invalid nothing is saved. Otherwise the valid ones are saved and the rest reported.
public sealed record BulkCreateStudentsRequest(
    List<CreateRequest> Students,
    bool AllOrNothing = false
);

public sealed class BulkCreateStudentsValidator : AbstractValidator<BulkCreateStudentsRequest>
{
    public const int MaxItems = 500;

    public BulkCreateStudentsValidator()
    {
        RuleFor(x => x.Students)
            .NotEmpty().WithMessage("At least one student is required.")
            .Must(s => s.Count <= MaxItems).WithMessage($"At most {MaxItems} students per request.");
    }
}

public static class BulkCreateStudentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/bulk", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateStudents))
             .WithName("BulkCreateStudents")
             .WithSummary("Create many students at once; every item gets its own result")
             .Produces<BulkOperationResponse>(StatusCodes.Status200OK)
             .Produces<BulkOperationResponse>(StatusCodes.Status400BadRequest)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        BulkCreateStudentsRequest request,
        IValidator<BulkCreateStudentsRequest> requestValidator,
        IValidator<CreateRequest> itemValidator,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        ILogger<BulkCreateStudentsRequest> logger,
        CancellationToken ct
    )
    {
        var requestValidation = await requestValidator.ValidateAsync(request, ct);
        if (!requestValidation.IsValid)
            return Results.ValidationProblem(requestValidation.ToDictionary());

        var items = request.Students;
        var existingEmails = await repository.GetExistingEmailsAsync(
            items.Select(s => s.Email ?? string.Empty).Where(e => e.Length > 0).ToList(), ct);

        var errors = new List<string>[items.Count];
        var seenEmails = new HashSet<string>();

        for (var i = 0; i < items.Count; i++)
        {
            errors[i] = [];

            if (items[i] is null)
            {
                errors[i].Add("Student data is missing.");
                continue;
            }

            var validation = await itemValidator.ValidateAsync(items[i], ct);
            errors[i].AddRange(validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

            var email = items[i].Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
                continue;

            if (existingEmails.Contains(email))
                errors[i].Add($"Email: Student with email '{items[i].Email}' already exists.");
            else if (!seenEmails.Add(email))
                errors[i].Add($"Email: Email '{items[i].Email}' is repeated in this request.");
        }

        var anyInvalid = errors.Any(e => e.Count > 0);

        if (anyInvalid && request.AllOrNothing)
        {
            logger.LogWarning("Bulk student creation refused: {Invalid} of {Total} students are invalid",
                errors.Count(e => e.Count > 0), items.Count);

            // Valid items were not saved either; say so instead of leaving them without an explanation
            const string notSaved = "Not saved: the request is all-or-nothing and other items are invalid.";

            return Results.Json(
                BulkOperationResponse.From(items
                    .Select((s, i) => Result(i, s, null, errors[i].Count > 0 ? errors[i] : [notSaved]))
                    .ToList()),
                statusCode: StatusCodes.Status400BadRequest);
        }

        var created = new Dictionary<int, Student>();

        for (var i = 0; i < items.Count; i++)
        {
            if (errors[i].Count > 0)
                continue;

            var student = StudentFactory.Build(items[i]);
            created[i] = student;
            await repository.AddAsync(student, ct);
        }

        if (created.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Bulk student creation: {Created} of {Total} created", created.Count, items.Count);

        var results = items
            .Select((s, i) => Result(i, s, created.GetValueOrDefault(i)?.Id, errors[i]))
            .ToList();

        return Results.Ok(BulkOperationResponse.From(results));
    }

    private static BulkItemResult Result(int index, CreateRequest? item, Guid? id, List<string> errors) =>
        new(index, errors.Count == 0 && id is not null, id, item?.Email, errors);
}

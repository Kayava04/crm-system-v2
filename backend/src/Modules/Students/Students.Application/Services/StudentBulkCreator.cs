using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Common;
using Students.Application.Abstractions;
using Students.Domain.Entities;

namespace Students.Application.Services;

// Refused = the request was all-or-nothing and something was invalid, so nothing was saved
internal sealed record BulkCreateOutcome(IReadOnlyList<BulkItemResult> Results, bool Refused);

// The one place that validates and saves many students, used by the bulk endpoint and by file import
internal sealed class StudentBulkCreator(
    IValidator<Features.CreateStudent.CreateRequest> validator,
    IStudentRepository repository,
    IStudentUnitOfWork unitOfWork,
    ILogger<StudentBulkCreator> logger)
{
    private const string NotSaved = "Not saved: the request is all-or-nothing and other items are invalid.";

    public async Task<BulkCreateOutcome> CreateAsync(
        IReadOnlyList<BulkCreateItem> items,
        bool allOrNothing,
        bool dryRun,
        CancellationToken ct)
    {
        var existingEmails = await repository.GetExistingEmailsAsync(
            items.Select(i => i.Request?.Email ?? string.Empty).Where(e => e.Length > 0).ToList(), ct);

        var errors = new List<string>[items.Count];
        var seenEmails = new HashSet<string>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            errors[i] = [.. item.Errors ?? []];

            if (item.Request is null)
            {
                if (errors[i].Count == 0)
                    errors[i].Add("Student data is missing.");

                continue;
            }

            var handled = item.HandledKeys ?? new HashSet<string>();
            var validation = await validator.ValidateAsync(item.Request, ct);

            errors[i].AddRange(validation.Errors
                .Where(e => !handled.Contains(KeyOf(e.PropertyName)))
                .Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));

            var email = item.Request.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email) || handled.Contains("email"))
                continue;

            if (existingEmails.Contains(email))
                errors[i].Add($"Email: Student with email '{item.Request.Email}' already exists.");
            else if (!seenEmails.Add(email))
                errors[i].Add($"Email: Email '{item.Request.Email}' is repeated in this request.");
        }

        if (allOrNothing && errors.Any(e => e.Count > 0))
        {
            logger.LogWarning("Bulk student creation refused: {Invalid} of {Total} students are invalid",
                errors.Count(e => e.Count > 0), items.Count);

            return new BulkCreateOutcome(
                items.Select((item, i) => Result(i, item, null, errors[i].Count > 0 ? errors[i] : [NotSaved], dryRun)).ToList(),
                Refused: true);
        }

        var created = new Dictionary<int, Student>();

        for (var i = 0; i < items.Count; i++)
        {
            if (errors[i].Count > 0)
                continue;

            var student = StudentFactory.Build(items[i].Request!);
            created[i] = student;

            if (!dryRun)
                await repository.AddAsync(student, ct);
        }

        if (!dryRun && created.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Bulk student creation{DryRun}: {Created} of {Total} {Verb}",
            dryRun ? " (dry run)" : string.Empty, created.Count, items.Count, dryRun ? "are valid" : "created");

        return new BulkCreateOutcome(
            items.Select((item, i) => Result(i, item, dryRun ? null : created.GetValueOrDefault(i)?.Id, errors[i], dryRun)).ToList(),
            Refused: false);
    }

    private static BulkItemResult Result(int index, BulkCreateItem item, Guid? id, IReadOnlyList<string> errors, bool dryRun) =>
        new(index, errors.Count == 0 && (id is not null || dryRun), id, item.Request?.Email, errors);

    // "Languages[0]" and "Languages" are the field "languages"
    internal static string KeyOf(string propertyName)
    {
        var bracket = propertyName.IndexOf('[');
        var name = bracket >= 0 ? propertyName[..bracket] : propertyName;

        return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}

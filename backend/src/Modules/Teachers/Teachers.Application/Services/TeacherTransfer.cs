using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Shared.Files;
using Teachers.Application.Features.CreateTeacher;
using Teachers.Domain.Entities;
using Teachers.Domain.Enums;

namespace Teachers.Application.Services;

// What an exported teacher looks like in a JSON file: the fields of a new teacher plus id, status and dates.
// The same file can be imported again (the extra fields are ignored).
internal sealed record TeacherExportItem(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    string City,
    string Country,
    decimal? BaseSalary,
    decimal? LessonsRate,
    string? Comment,
    TeacherStatus Status,
    bool HasAccount,
    DateTime CreatedAt);

internal static class TeacherTransfer
{
    public const int MaxImportRows = 2000;
    public const int MaxExportRows = 10_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ------------------------------------------------------------------ export (the salary shown is the one in force now)
    public static IReadOnlyDictionary<string, object?> ToRow(Teacher t) => new Dictionary<string, object?>
    {
        ["firstName"] = t.FirstName,
        ["lastName"] = t.LastName,
        ["middleName"] = t.MiddleName,
        ["dateOfBirth"] = t.DateOfBirth,
        ["phoneNumber"] = t.PhoneNumber,
        ["email"] = t.Email,
        ["city"] = t.City,
        ["country"] = t.Country,
        ["baseSalary"] = t.CurrentSalaryRate?.BaseSalary,
        ["lessonsRate"] = t.CurrentSalaryRate?.LessonsRate,
        ["comment"] = t.Comment,
        ["status"] = t.Status.ToString(),
        ["hasAccount"] = t.UserId is not null,
        ["createdAt"] = t.CreatedAt,
    };

    public static TeacherExportItem ToExportItem(Teacher t) => new(
        t.Id, t.FirstName, t.LastName, t.MiddleName, t.DateOfBirth, t.PhoneNumber, t.Email, t.City, t.Country,
        t.CurrentSalaryRate?.BaseSalary, t.CurrentSalaryRate?.LessonsRate, t.Comment, t.Status, t.UserId is not null, t.CreatedAt);

    public static byte[] ToJsonBytes<T>(IEnumerable<T> items) => JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions);

    public static byte[] SampleJson() => JsonSerializer.SerializeToUtf8Bytes(new[]
    {
        new
        {
            firstName = "Olena", lastName = "Kovalenko", middleName = "Petrivna", dateOfBirth = "1988-07-14",
            phoneNumber = "+380671234567", email = "olena.kovalenko@example.com", city = "Kyiv", country = "Ukraine",
            baseSalary = 20000, lessonsRate = 350, comment = "Speaks German too"
        }
    }, JsonOptions);

    // ------------------------------------------------------------------ import from Excel
    public static BulkCreateItem FromRow(SheetRow row, FileLanguage language)
    {
        var errors = row.Errors.ToList();
        var handled = new HashSet<string>(row.InvalidKeys);

        foreach (var column in TeacherTable.Schema.Columns.Where(c => c.Required && c.ForImport))
        {
            if (row.InvalidKeys.Contains(column.Key))
                continue;

            var value = row.Values.GetValueOrDefault(column.Key);

            if (value is null or "")
            {
                errors.Add($"{column.Header(language)}: {(language == FileLanguage.Uk ? "Обов'язкове поле." : "This field is required.")}");
                handled.Add(column.Key);
            }
        }

        string Text(string key) => row.Values.GetValueOrDefault(key) as string ?? string.Empty;
        string? Optional(string key) => Text(key) is { Length: > 0 } text ? text : null;
        decimal Number(string key) => row.Values.GetValueOrDefault(key) is decimal number ? number : 0;

        var request = new CreateTeacherRequest(
            Text("firstName"),
            Text("lastName"),
            Optional("middleName"),
            row.Values.GetValueOrDefault("dateOfBirth") is DateOnly born ? born : default,
            Text("phoneNumber"),
            Text("email"),
            Text("city"),
            Text("country"),
            Number("baseSalary"),
            Number("lessonsRate"),
            Optional("comment"));

        return new BulkCreateItem(request, errors, handled);
    }

    // ------------------------------------------------------------------ import from JSON
    public static (IReadOnlyList<BulkCreateItem> Items, IReadOnlyList<int> Rows) ParseJson(Stream stream)
    {
        JsonNode? root;

        try
        {
            root = JsonNode.Parse(stream, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (JsonException ex)
        {
            throw new ImportFileException($"The file is not valid JSON: {ex.Message}");
        }

        var array = root as JsonArray ?? (root as JsonObject)?["teachers"] as JsonArray
            ?? throw new ImportFileException("The JSON must be an array of teachers, or an object with a \"teachers\" array.");

        if (array.Count == 0)
            throw new ImportFileException("The file contains no teachers.");

        if (array.Count > MaxImportRows)
            throw new ImportFileException($"The file has more than {MaxImportRows} teachers. Split it into smaller files.");

        var requiredKeys = TeacherTable.Schema.Columns.Where(c => c.Required && c.ForImport).Select(c => c.Key).ToList();
        var items = new List<BulkCreateItem>();

        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is not JsonObject obj)
            {
                items.Add(new BulkCreateItem(null, ["Each item must be an object with the teacher's fields."]));
                continue;
            }

            var errors = new List<string>();
            var handled = new HashSet<string>();

            foreach (var key in requiredKeys)
            {
                var present = obj.Any(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase) && p.Value is not null);
                if (present)
                    continue;

                errors.Add($"{key}: This field is required.");
                handled.Add(key);
            }

            try
            {
                items.Add(new BulkCreateItem(obj.Deserialize<CreateTeacherRequest>(JsonOptions), errors, handled));
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid data: {ex.Message}");
                items.Add(new BulkCreateItem(null, errors));
            }
        }

        return (items, Enumerable.Range(1, items.Count).ToList());
    }
}

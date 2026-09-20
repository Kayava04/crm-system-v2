using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Education.Contracts.Enums;
using Shared.Files;
using Students.Application.Features.CreateStudent;
using Students.Domain.Entities;
using Students.Domain.Enums;

namespace Students.Application.Services;

// What an exported student looks like in a JSON file: the fields of a new student plus id, status and dates.
// The same file can be imported again (the extra fields are ignored).
internal sealed record StudentExportItem(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    string City,
    string Country,
    bool IsChild,
    string? Comment,
    LearningGoal? LearningGoal,
    Format? Format,
    LessonType? LessonType,
    int? Intensity,
    Level? CurrentLevel,
    bool? HadPreviousCourses,
    IReadOnlyList<Language> Languages,
    StudentStatus Status,
    bool HasAccount,
    DateTime CreatedAt);

internal static class StudentTransfer
{
    public const int MaxImportRows = 2000;
    public const int MaxExportRows = 10_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ------------------------------------------------------------------ export
    public static IReadOnlyDictionary<string, object?> ToRow(Student s) => new Dictionary<string, object?>
    {
        ["firstName"] = s.FirstName,
        ["lastName"] = s.LastName,
        ["middleName"] = s.MiddleName,
        ["dateOfBirth"] = s.DateOfBirth,
        ["phoneNumber"] = s.PhoneNumber,
        ["email"] = s.Email,
        ["city"] = s.City,
        ["country"] = s.Country,
        ["isChild"] = s.IsChild,
        ["learningGoal"] = s.Preferences?.LearningGoal.ToString(),
        ["format"] = s.Preferences?.Format.ToString(),
        ["lessonType"] = s.Preferences?.LessonType.ToString(),
        ["intensity"] = s.Preferences?.Intensity,
        ["currentLevel"] = s.Preferences?.CurrentLevel.ToString(),
        ["hadPreviousCourses"] = s.Preferences?.HadPreviousCourses,
        ["languages"] = s.Languages.Select(l => l.Language.ToString()).ToList(),
        ["comment"] = s.Comment,
        ["status"] = s.Status.ToString(),
        ["hasAccount"] = s.UserId is not null,
        ["createdAt"] = s.CreatedAt,
    };

    public static StudentExportItem ToExportItem(Student s) => new(
        s.Id, s.FirstName, s.LastName, s.MiddleName, s.DateOfBirth, s.PhoneNumber, s.Email, s.City, s.Country, s.IsChild, s.Comment,
        s.Preferences?.LearningGoal, s.Preferences?.Format, s.Preferences?.LessonType, s.Preferences?.Intensity,
        s.Preferences?.CurrentLevel, s.Preferences?.HadPreviousCourses,
        s.Languages.Select(l => l.Language).ToList(), s.Status, s.UserId is not null, s.CreatedAt);

    public static byte[] ToJsonBytes<T>(IEnumerable<T> items) => JsonSerializer.SerializeToUtf8Bytes(items, JsonOptions);

    public static byte[] SampleJson() => JsonSerializer.SerializeToUtf8Bytes(new[]
    {
        new
        {
            firstName = "Ivan", lastName = "Petrenko", middleName = "Andriyovych", dateOfBirth = "2001-03-25",
            phoneNumber = "+380501112233", email = "ivan.petrenko@example.com", city = "Kyiv", country = "Ukraine",
            isChild = false, comment = "Prefers evening lessons",
            learningGoal = "Work", format = "Online", lessonType = "Individual", intensity = 3, currentLevel = "B1",
            hadPreviousCourses = false, languages = new[] { "English", "German" }
        }
    }, JsonOptions);

    // ------------------------------------------------------------------ import from Excel
    public static BulkCreateItem FromRow(SheetRow row, FileLanguage language)
    {
        var errors = row.Errors.ToList();
        var handled = new HashSet<string>(row.InvalidKeys);

        foreach (var column in StudentTable.Schema.Columns.Where(c => c.Required && c.ForImport))
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
        bool Flag(string key) => row.Values.GetValueOrDefault(key) is true;
        T Choice<T>(string key) where T : struct, Enum =>
            row.Values.GetValueOrDefault(key) is string name && Enum.TryParse<T>(name, out var parsed) ? parsed : default;

        var languages = (row.Values.GetValueOrDefault("languages") as IEnumerable<string> ?? [])
            .Select(Enum.Parse<Language>)
            .ToList();

        var request = new CreateRequest(
            Text("firstName"),
            Text("lastName"),
            Optional("middleName"),
            row.Values.GetValueOrDefault("dateOfBirth") is DateOnly born ? born : default,
            Text("phoneNumber"),
            Text("email"),
            Text("city"),
            Text("country"),
            Flag("isChild"),
            Optional("comment"),
            Choice<LearningGoal>("learningGoal"),
            Choice<Format>("format"),
            Choice<LessonType>("lessonType"),
            row.Values.GetValueOrDefault("intensity") is int intensity ? intensity : 0,
            Choice<Level>("currentLevel"),
            Flag("hadPreviousCourses"),
            languages);

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

        var array = root as JsonArray ?? (root as JsonObject)?["students"] as JsonArray
            ?? throw new ImportFileException("The JSON must be an array of students, or an object with a \"students\" array.");

        if (array.Count == 0)
            throw new ImportFileException("The file contains no students.");

        if (array.Count > MaxImportRows)
            throw new ImportFileException($"The file has more than {MaxImportRows} students. Split it into smaller files.");

        var requiredKeys = StudentTable.Schema.Columns.Where(c => c.Required && c.ForImport).Select(c => c.Key).ToList();
        var items = new List<BulkCreateItem>();

        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is not JsonObject obj)
            {
                items.Add(new BulkCreateItem(null, ["Each item must be an object with the student's fields."]));
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
                items.Add(new BulkCreateItem(obj.Deserialize<CreateRequest>(JsonOptions), errors, handled));
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

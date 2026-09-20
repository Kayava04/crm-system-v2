using Education.Contracts.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Files;
using Students.Application.Abstractions;
using Students.Application.Services;

namespace Students.Application.Features.ExportStudents;

public static class ExportStudentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/export", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewStudents))
             .WithName("ExportStudents")
             .WithSummary("Download students as an Excel (.xlsx) or JSON file; accepts the same filters as the list")
             .Produces(StatusCodes.Status200OK, contentType: FileFormats.XlsxContentType)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        IStudentRepository repository,
        CancellationToken ct,
        string? fileFormat = "xlsx",
        string? lang = "en",
        string? search = null,
        string? city = null,
        bool? isChild = null,
        Language? language = null,
        Level? currentLevel = null,
        Format? format = null
    )
    {
        var problems = new Dictionary<string, string[]>();

        if (!FileFormats.TryParseFormat(fileFormat, out var type))
            problems["fileFormat"] = ["Use xlsx or json."];

        if (!FileFormats.TryParseLanguage(lang, out var fileLanguage))
            problems["lang"] = ["Use en or uk."];

        if (problems.Count > 0)
            return Results.ValidationProblem(problems);

        var students = await repository.GetForExportAsync(
            search, city, isChild, language, currentLevel, format, StudentTransfer.MaxExportRows + 1, ct);

        if (students.Count > StudentTransfer.MaxExportRows)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["filters"] = [$"More than {StudentTransfer.MaxExportRows} students match. Narrow the filters and export again."]
            });

        var content = type == FileFormat.Xlsx
            ? SpreadsheetWriter.Write(StudentTable.Schema, fileLanguage, students.Select(StudentTransfer.ToRow), SheetMode.Export)
            : StudentTransfer.ToJsonBytes(students.Select(StudentTransfer.ToExportItem));

        return Results.File(
            content,
            FileFormats.ContentType(type),
            $"students-{DateTime.UtcNow:yyyy-MM-dd}{FileFormats.Extension(type)}");
    }
}

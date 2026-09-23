using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Files;
using Teachers.Application.Abstractions;
using Teachers.Application.Services;
using Teachers.Domain.Enums;

namespace Teachers.Application.Features.ExportTeachers;

public static class ExportTeachersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/export", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewTeachers))
             .WithName("ExportTeachers")
             .WithSummary("Download teachers as an Excel (.xlsx) or JSON file; accepts the same filters as the list")
             .Produces(StatusCodes.Status200OK, contentType: FileFormats.XlsxContentType)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        ITeacherRepository repository,
        CancellationToken ct,
        string? fileFormat = "xlsx",
        string? lang = "en",
        string? search = null,
        string? city = null,
        TeacherStatus? status = null
    )
    {
        var problems = new Dictionary<string, string[]>();

        if (!FileFormats.TryParseFormat(fileFormat, out var type))
            problems["fileFormat"] = ["Use xlsx or json."];

        if (!FileFormats.TryParseLanguage(lang, out var fileLanguage))
            problems["lang"] = ["Use en or uk."];

        if (problems.Count > 0)
            return Results.ValidationProblem(problems);

        var teachers = await repository.GetForExportAsync(
            search, city, status, TeacherTransfer.MaxExportRows + 1, ct);

        if (teachers.Count > TeacherTransfer.MaxExportRows)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["filters"] = [$"More than {TeacherTransfer.MaxExportRows} teachers match. Narrow the filters and export again."]
            });

        var content = type == FileFormat.Xlsx
            ? SpreadsheetWriter.Write(TeacherTable.Schema, fileLanguage, teachers.Select(TeacherTransfer.ToRow), SheetMode.Export)
            : TeacherTransfer.ToJsonBytes(teachers.Select(TeacherTransfer.ToExportItem));

        return Results.File(
            content,
            FileFormats.ContentType(type),
            $"teachers-{DateTime.UtcNow:yyyy-MM-dd}{FileFormats.Extension(type)}");
    }
}

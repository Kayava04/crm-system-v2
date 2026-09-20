using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Files;
using Teachers.Application.Services;

namespace Teachers.Application.Features.TeacherImportTemplate;

public static class TeacherImportTemplateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/import-template", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateTeachers))
             .WithName("TeacherImportTemplate")
             .WithSummary("Download an empty Excel template (with a help sheet) or a JSON sample for importing teachers")
             .Produces(StatusCodes.Status200OK, contentType: FileFormats.XlsxContentType)
             .ProducesValidationProblem();
    }

    private static IResult Handle(string? fileFormat = "xlsx", string? lang = "en")
    {
        var problems = new Dictionary<string, string[]>();

        if (!FileFormats.TryParseFormat(fileFormat, out var type))
            problems["fileFormat"] = ["Use xlsx or json."];

        if (!FileFormats.TryParseLanguage(lang, out var fileLanguage))
            problems["lang"] = ["Use en or uk."];

        if (problems.Count > 0)
            return Results.ValidationProblem(problems);

        var content = type == FileFormat.Xlsx
            ? SpreadsheetWriter.Write(TeacherTable.Schema, fileLanguage, [], SheetMode.Template)
            : TeacherTransfer.SampleJson();

        return Results.File(content, FileFormats.ContentType(type), $"teachers-import-template{FileFormats.Extension(type)}");
    }
}

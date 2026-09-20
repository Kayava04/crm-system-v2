using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Files;
using Shared.Kernel.Common;
using Students.Application.Services;

namespace Students.Application.Features.ImportStudents;

public sealed record ImportStudentsRequest;

public static class ImportStudentsEndpoint
{
    private const long MaxFileBytes = 5 * 1024 * 1024;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/import", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateStudents))
             .DisableAntiforgery()
             .WithName("ImportStudents")
             .WithSummary("Import students from an Excel (.xlsx) or JSON file (multipart form field 'file'). Use dryRun=true to check the file without saving.")
             .Accepts<IFormFile>("multipart/form-data")
             .Produces<ImportResponse>(StatusCodes.Status200OK)
             .Produces<ImportResponse>(StatusCodes.Status400BadRequest)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        IFormFile? file,
        StudentBulkCreator creator,
        ILogger<ImportStudentsRequest> logger,
        CancellationToken ct,
        bool allOrNothing = false,
        bool dryRun = false
    )
    {
        if (file is null || file.Length == 0)
            return Problem("No file was uploaded. Send it in the 'file' field of a multipart form.");

        if (file.Length > MaxFileBytes)
            return Problem($"The file is larger than {MaxFileBytes / 1024 / 1024} MB.");

        if (!FileFormats.TryDetect(file.FileName, out var type))
            return Problem("Unsupported file type. Upload an Excel (.xlsx) or a JSON (.json) file.");

        IReadOnlyList<BulkCreateItem> items;
        IReadOnlyList<int> rows;
        IReadOnlyList<string> ignoredColumns = [];
        var language = FileLanguage.En;

        try
        {
            using var content = new MemoryStream();
            await file.CopyToAsync(content, ct);
            content.Position = 0;

            if (type == FileFormat.Xlsx)
            {
                var data = SpreadsheetReader.Read(content, StudentTable.Schema, StudentTransfer.MaxImportRows);
                language = data.Language;

                if (data.MissingRequiredColumns.Count > 0)
                    throw new ImportFileException(language == FileLanguage.Uk
                        ? $"У файлі немає обов'язкових колонок: {string.Join(", ", data.MissingRequiredColumns)}. Завантажте шаблон і використайте його заголовки."
                        : $"The file has no required columns: {string.Join(", ", data.MissingRequiredColumns)}. Download the template and use its headers.");

                if (data.Rows.Count == 0)
                    throw new ImportFileException(language == FileLanguage.Uk ? "У файлі немає студентів." : "The file contains no students.");

                ignoredColumns = data.IgnoredColumns;
                items = data.Rows.Select(r => StudentTransfer.FromRow(r, language)).ToList();
                rows = data.Rows.Select(r => r.RowNumber).ToList();
            }
            else
            {
                (items, rows) = StudentTransfer.ParseJson(content);
            }
        }
        catch (ImportFileException ex)
        {
            logger.LogWarning("Student import refused: {Reason}", ex.Message);

            return Problem(ex.Message);
        }

        var outcome = await creator.CreateAsync(items, allOrNothing, dryRun, ct);

        var results = outcome.Results
            .Select((r, i) => new ImportRowResult(
                rows[i],
                r.Success,
                r.Id,
                r.Key,
                type == FileFormat.Xlsx ? ImportErrors.UseColumnNames(r.Errors, StudentTable.Schema, language) : r.Errors))
            .ToList();

        var response = new ImportResponse(
            type.ToString().ToLowerInvariant(),
            language.ToString().ToLowerInvariant(),
            dryRun,
            allOrNothing,
            results.Count,
            results.Count(r => r.Success),
            results.Count(r => !r.Success),
            ignoredColumns,
            results);

        logger.LogInformation("Student import ({Type}{DryRun}): {Ok} of {Total} rows ok",
            type, dryRun ? ", dry run" : string.Empty, response.Succeeded, response.Total);

        return outcome.Refused
            ? Results.Json(response, statusCode: StatusCodes.Status400BadRequest)
            : Results.Ok(response);
    }

    private static IResult Problem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [message] });
}

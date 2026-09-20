namespace Shared.Files;

public enum FileFormat
{
    Xlsx,
    Json
}

public static class FileFormats
{
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string JsonContentType = "application/json";

    public static string ContentType(FileFormat format) => format == FileFormat.Xlsx ? XlsxContentType : JsonContentType;

    public static string Extension(FileFormat format) => format == FileFormat.Xlsx ? ".xlsx" : ".json";

    public static bool TryParseFormat(string? value, out FileFormat format)
    {
        switch (value?.Trim().TrimStart('.').ToLowerInvariant())
        {
            case "xlsx" or "excel":
                format = FileFormat.Xlsx;
                return true;
            case "json":
                format = FileFormat.Json;
                return true;
            default:
                format = default;
                return false;
        }
    }

    // By the extension of the uploaded file name
    public static bool TryDetect(string? fileName, out FileFormat format) =>
        TryParseFormat(Path.GetExtension(fileName), out format);

    public static bool TryParseLanguage(string? value, out FileLanguage language)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null or "" or "en":
                language = FileLanguage.En;
                return true;
            case "uk" or "ua":
                language = FileLanguage.Uk;
                return true;
            default:
                language = default;
                return false;
        }
    }
}

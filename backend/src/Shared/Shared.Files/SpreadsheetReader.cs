using System.Globalization;
using ClosedXML.Excel;

namespace Shared.Files;

// A row of the sheet with its values already converted to the column types; Errors explain what could not be converted
public sealed record SheetRow(
    int RowNumber,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<string> Errors,
    IReadOnlySet<string> InvalidKeys);

public sealed record SheetData(
    FileLanguage Language,
    IReadOnlyList<SheetRow> Rows,
    IReadOnlyList<string> IgnoredColumns,
    IReadOnlyList<string> MissingRequiredColumns);

// The file as a whole cannot be used (not an Excel file, empty, too many rows...); the message is meant for the person
public sealed class SpreadsheetFormatException(string message) : ImportFileException(message);

public static class SpreadsheetReader
{
    private static readonly string[] DateFormats =
        ["yyyy-MM-dd", "yyyy-M-d", "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy/MM/dd", "yyyy.MM.dd"];

    public static SheetData Read(Stream stream, TableSchema schema, int maxRows)
    {
        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            throw new SpreadsheetFormatException("The file is not a valid Excel workbook (.xlsx). / Файл не є коректною книгою Excel (.xlsx).");
        }

        using (workbook)
        {
            var helpNames = new[] { schema.HelpSheetName(FileLanguage.En), schema.HelpSheetName(FileLanguage.Uk) };
            var sheet = workbook.Worksheets.FirstOrDefault(w => !helpNames.Contains(w.Name, StringComparer.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault()
                ?? throw new SpreadsheetFormatException("The workbook has no sheets. / У книзі немає аркушів.");

            var used = sheet.RangeUsed();
            if (used is null)
                throw new SpreadsheetFormatException("The sheet is empty. / Аркуш порожній.");

            var headerRow = used.FirstRow().RowNumber();
            var lastColumn = used.LastColumn().ColumnNumber();
            var lastRow = used.LastRow().RowNumber();

            // ---- which sheet column is which field
            var map = new Dictionary<int, ColumnDef>();
            var ignored = new List<string>();
            int english = 0, ukrainian = 0;

            for (var c = 1; c <= lastColumn; c++)
            {
                var text = sheet.Cell(headerRow, c).GetString().Trim();
                if (text.Length == 0)
                    continue;

                var normalized = Localization.NormalizeHeader(text);
                var column = schema.Columns.FirstOrDefault(d =>
                    normalized == Localization.NormalizeHeader(d.HeaderEn)
                    || normalized == Localization.NormalizeHeader(d.HeaderUk)
                    || normalized == Localization.NormalizeHeader(d.Key));

                if (column is null || map.ContainsValue(column))
                {
                    ignored.Add(text);
                    continue;
                }

                map[c] = column;

                if (normalized == Localization.NormalizeHeader(column.HeaderUk) && normalized != Localization.NormalizeHeader(column.HeaderEn))
                    ukrainian++;
                else if (normalized == Localization.NormalizeHeader(column.HeaderEn))
                    english++;
            }

            var language = ukrainian > english ? FileLanguage.Uk : FileLanguage.En;

            var missing = schema.Columns
                .Where(d => d.Required && d.ForImport && !map.ContainsValue(d))
                .Select(d => d.Header(language))
                .ToList();

            if (missing.Count > 0)
                return new SheetData(language, [], ignored, missing);

            // ---- the rows
            var rows = new List<SheetRow>();

            for (var r = headerRow + 1; r <= lastRow; r++)
            {
                if (map.Keys.All(c => IsBlank(sheet.Cell(r, c))))
                    continue;

                if (rows.Count >= maxRows)
                    throw new SpreadsheetFormatException(
                        $"The file has more than {maxRows} rows. Split it into smaller files. / У файлі понад {maxRows} рядків. Розбийте його на менші файли.");

                var values = new Dictionary<string, object?>();
                var errors = new List<string>();
                var invalid = new HashSet<string>();

                foreach (var (columnNumber, column) in map)
                {
                    var (value, error) = Convert(sheet.Cell(r, columnNumber), column, language);
                    values[column.Key] = value;

                    if (error is null)
                        continue;

                    errors.Add($"{column.Header(language)}: {error}");
                    invalid.Add(column.Key);
                }

                rows.Add(new SheetRow(r, values, errors, invalid));
            }

            return new SheetData(language, rows, ignored, []);
        }
    }

    private static bool IsBlank(IXLCell cell) => cell.IsEmpty() || string.IsNullOrWhiteSpace(cell.GetString());

    private static (object? Value, string? Error) Convert(IXLCell cell, ColumnDef column, FileLanguage language)
    {
        if (IsBlank(cell))
            return (null, null);

        string T(string en, string uk) => Localization.T(language, en, uk);
        var text = ReadText(cell);

        switch (column.Type)
        {
            case ColumnType.Text:
                return (text, null);

            case ColumnType.Date:
            case ColumnType.DateTime:
                if (cell.DataType == XLDataType.DateTime)
                    return column.Type == ColumnType.Date ? (DateOnly.FromDateTime(cell.GetDateTime()), null) : (cell.GetDateTime(), null);

                if (cell.DataType == XLDataType.Number && cell.GetDouble() > 1 && cell.GetDouble() < 3_000_000)
                {
                    var fromSerial = DateTime.FromOADate(cell.GetDouble());
                    return column.Type == ColumnType.Date ? (DateOnly.FromDateTime(fromSerial), null) : (fromSerial, null);
                }

                if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return column.Type == ColumnType.Date ? (DateOnly.FromDateTime(parsed), null) : (parsed, null);

                return (null, T($"'{text}' is not a valid date. Use year-month-day, for example 2001-03-25.",
                                $"«{text}» не є коректною датою. Використайте формат рік-місяць-день, наприклад 2001-03-25."));

            case ColumnType.Integer:
                if (cell.DataType == XLDataType.Number && Math.Abs(cell.GetDouble() % 1) < 1e-9)
                    return ((int)cell.GetDouble(), null);

                return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole)
                    ? (whole, null)
                    : (null, T($"'{text}' is not a whole number.", $"«{text}» не є цілим числом."));

            case ColumnType.Decimal:
                if (cell.DataType == XLDataType.Number)
                    return ((decimal)cell.GetDouble(), null);

                // "1500,50" is how many people write a decimal
                var normalized = text.Replace(" ", string.Empty).Replace(',', '.');

                return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                    ? (number, null)
                    : (null, T($"'{text}' is not a number.", $"«{text}» не є числом."));

            case ColumnType.Boolean:
                if (cell.DataType == XLDataType.Boolean)
                    return (cell.GetBoolean(), null);

                return Localization.ParseBoolean(text) is { } flag
                    ? (flag, null)
                    : (null, T($"'{text}' is not clear. Use Yes or No.", $"«{text}» незрозуміло. Використайте «Так» або «Ні»."));

            case ColumnType.Choice:
                return MatchChoice(column, text, language);

            case ColumnType.MultiChoice:
                var parts = text.Split([',', ';', '/', '|', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var values = new List<string>();

                foreach (var part in parts)
                {
                    var (value, error) = MatchChoice(column, part, language);
                    if (error is not null)
                        return (null, error);

                    if (!values.Contains((string)value!))
                        values.Add((string)value!);
                }

                return (values, null);
        }

        return (text, null);
    }

    private static (object? Value, string? Error) MatchChoice(ColumnDef column, string text, FileLanguage language)
    {
        var wanted = Localization.NormalizeHeader(text);

        var choice = column.Choices?.FirstOrDefault(c =>
            wanted == Localization.NormalizeHeader(c.Value)
            || wanted == Localization.NormalizeHeader(c.En)
            || wanted == Localization.NormalizeHeader(c.Uk));

        if (choice is not null)
            return (choice.Value, null);

        var allowed = string.Join(", ", column.Choices?.Select(c => c.Display(language)) ?? []);

        return (null, Localization.T(language,
            $"'{text}' is not allowed. Allowed values: {allowed}.",
            $"«{text}» не підходить. Допустимі значення: {allowed}."));
    }

    // Numbers typed into a text column (phone numbers!) must not turn into 3.8E+11
    private static string ReadText(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number)
            return ((decimal)cell.GetDouble()).ToString("0.############################", CultureInfo.InvariantCulture);

        return cell.GetString().Trim();
    }
}

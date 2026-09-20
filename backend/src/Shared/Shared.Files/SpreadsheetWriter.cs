using ClosedXML.Excel;

namespace Shared.Files;

public enum SheetMode
{
    // Every column, the data as it is in the system
    Export,

    // Only the columns an import reads, one example row, drop-down lists and a help sheet
    Template
}

public static class SpreadsheetWriter
{
    private const string RequiredFill = "#FCE4D6";
    private const string OptionalFill = "#DDEBF7";

    public static byte[] Write(
        TableSchema schema,
        FileLanguage language,
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        SheetMode mode)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(schema.SheetName(language));

        var columns = schema.Columns.Where(c => mode == SheetMode.Export || c.ForImport).ToList();

        for (var i = 0; i < columns.Count; i++)
        {
            var header = sheet.Cell(1, i + 1);
            header.Value = columns[i].Header(language);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml(columns[i].Required && columns[i].ForImport ? RequiredFill : OptionalFill);
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Alignment.WrapText = true;
        }

        var widths = columns.Select(c => c.Header(language).Length).ToArray();
        var rowNumber = 1;

        foreach (var row in rows)
        {
            rowNumber++;

            for (var i = 0; i < columns.Count; i++)
            {
                row.TryGetValue(columns[i].Key, out var value);
                widths[i] = Math.Max(widths[i], WriteCell(sheet.Cell(rowNumber, i + 1), columns[i], value, language));
            }
        }

        // The data sheet stays empty on purpose: an example row left by mistake would be imported as a real person.
        // Examples live on the help sheet.
        if (mode == SheetMode.Template)
        {
            for (var i = 0; i < columns.Count; i++)
                AddDropDown(sheet, columns[i], i + 1, language);
        }

        for (var i = 0; i < columns.Count; i++)
            sheet.Column(i + 1).Width = Math.Clamp(widths[i] * 1.15 + 2, 10, 50);

        sheet.SheetView.FreezeRows(1);

        if (mode == SheetMode.Export && rowNumber > 1)
            sheet.Range(1, 1, rowNumber, columns.Count).SetAutoFilter();

        if (mode == SheetMode.Template)
            AddHelpSheet(workbook, schema, columns, language);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    // Returns the length of the text written, used to size the column
    private static int WriteCell(IXLCell cell, ColumnDef column, object? value, FileLanguage language)
    {
        if (value is null)
            return 0;

        switch (column.Type)
        {
            case ColumnType.Date:
                var date = value switch
                {
                    DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                    DateTime dt => dt.Date,
                    _ => (DateTime?)null
                };
                if (date is null) break;
                cell.Value = date.Value;
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                return 10;

            case ColumnType.DateTime:
                if (value is not DateTime moment) break;
                cell.Value = DateTime.SpecifyKind(moment, DateTimeKind.Unspecified);
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                return 16;

            case ColumnType.Integer:
                cell.Value = Convert.ToInt64(value);
                return cell.GetString().Length;

            case ColumnType.Decimal:
                cell.Value = Convert.ToDecimal(value);
                cell.Style.NumberFormat.Format = "0.00";
                return cell.GetString().Length;

            case ColumnType.Boolean:
                var yes = value is bool b && b;
                cell.Value = yes ? Localization.Yes(language) : Localization.No(language);
                return cell.GetString().Length;

            case ColumnType.Choice:
                var choice = column.Choices?.FirstOrDefault(c => c.Value == value.ToString());
                cell.Value = choice?.Display(language) ?? value.ToString();
                return cell.GetString().Length;

            case ColumnType.MultiChoice:
                var display = (value as IEnumerable<string> ?? [value.ToString()!])
                    .Select(v => column.Choices?.FirstOrDefault(c => c.Value == v)?.Display(language) ?? v);
                cell.Value = string.Join(", ", display);
                return cell.GetString().Length;
        }

        cell.Value = value.ToString();

        return cell.GetString().Length;
    }

    private static void AddDropDown(IXLWorksheet sheet, ColumnDef column, int columnNumber, FileLanguage language)
    {
        IEnumerable<string>? options = column.Type switch
        {
            ColumnType.Choice => column.Choices?.Select(c => c.Display(language)),
            ColumnType.Boolean => [Localization.Yes(language), Localization.No(language)],
            _ => null
        };

        if (options is null)
            return;

        var list = string.Join(",", options);

        // Excel refuses a drop-down list longer than 255 characters
        if (list.Length > 255)
            return;

        var validation = sheet.Range(2, columnNumber, 1000, columnNumber).CreateDataValidation();
        validation.List($"\"{list}\"");
        validation.IgnoreBlanks = true;
        validation.ShowErrorMessage = false;   // typing another spelling is fine: the import understands more than the list shows
    }

    private static void AddHelpSheet(XLWorkbook workbook, TableSchema schema, List<ColumnDef> columns, FileLanguage language)
    {
        var help = workbook.Worksheets.Add(schema.HelpSheetName(language));
        string T(string en, string uk) => Localization.T(language, en, uk);

        help.Cell(1, 1).Value = T("How to fill in the table", "Як заповнити таблицю");
        help.Cell(1, 1).Style.Font.Bold = true;
        help.Cell(1, 1).Style.Font.FontSize = 14;
        help.Cell(2, 1).Value = T(
            "Fill in one row per person on the first sheet, starting from row 2 (examples are in the last column of this sheet). Orange headers are required, blue ones are optional.",
            "Заповніть на першому аркуші один рядок на одну особу, починаючи з 2-го рядка (приклади наведено в останній колонці цього аркуша). Помаранчеві заголовки обов'язкові, сині необов'язкові.");
        help.Cell(3, 1).Value = T(
            "Dates: year-month-day (2001-03-25) or day.month.year (25.03.2001). Several values in one cell are separated by commas.",
            "Дати: рік-місяць-день (2001-03-25) або день.місяць.рік (25.03.2001). Кілька значень в одній клітинці розділяйте комами.");

        var headers = new[] { T("Column", "Колонка"), T("Required", "Обов'язкова"), T("What to enter", "Що вводити"), T("Allowed values", "Допустимі значення"), T("Example", "Приклад") };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = help.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(OptionalFill);
        }

        for (var r = 0; r < columns.Count; r++)
        {
            var column = columns[r];
            var allowed = column.Type switch
            {
                ColumnType.Choice or ColumnType.MultiChoice => string.Join(", ", column.Choices?.Select(c => c.Display(language)) ?? []),
                ColumnType.Boolean => $"{Localization.Yes(language)} / {Localization.No(language)}",
                _ => string.Empty
            };

            help.Cell(6 + r, 1).Value = column.Header(language);
            help.Cell(6 + r, 2).Value = column.Required ? T("Yes", "Так") : T("No", "Ні");
            help.Cell(6 + r, 3).Value = column.Note(language);
            help.Cell(6 + r, 4).Value = allowed;
            help.Cell(6 + r, 5).Value = column.Example(language);
        }

        help.Column(1).Width = 26;
        help.Column(2).Width = 12;
        help.Column(3).Width = 60;
        help.Column(4).Width = 50;
        help.Column(5).Width = 24;
        help.Range(6, 1, 5 + columns.Count, 5).Style.Alignment.WrapText = true;
        help.Range(6, 1, 5 + columns.Count, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    }
}

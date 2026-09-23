using ClosedXML.Excel;

namespace Crm.IntegrationTests.Infrastructure;

// Builds an Excel file the way a person would fill one in
public static class Xlsx
{
    public static byte[] Build(string[] headers, params object?[][] rows) => Build("Data", headers, rows);

    public static byte[] Build(string sheetName, string[] headers, params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var c = 0; c < headers.Length; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                if (rows[r][c] is { } value)
                    sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(value);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    public static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}

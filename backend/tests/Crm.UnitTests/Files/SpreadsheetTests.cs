using ClosedXML.Excel;
using Shared.Files;

namespace Crm.UnitTests.Files;

public class SpreadsheetTests
{
    private static readonly Choice[] Colors =
    [
        new("Red", "Red", "Червоний"),
        new("DarkBlue", "Dark blue", "Темно-синій"),
    ];

    private static readonly TableSchema Schema = new("People", "Люди",
    [
        new ColumnDef("name", "Full name", "Ім'я та прізвище", ColumnType.Text, Required: true, ExampleEn: "Ann", ExampleUk: "Анна", NoteEn: "Any name", NoteUk: "Будь-яке ім'я"),
        new ColumnDef("born", "Date of birth", "Дата народження", ColumnType.Date, Required: true),
        new ColumnDef("age", "Age (years)", "Вік (років)", ColumnType.Integer),
        new ColumnDef("salary", "Salary", "Зарплата", ColumnType.Decimal),
        new ColumnDef("member", "Member", "Учасник", ColumnType.Boolean),
        new ColumnDef("color", "Favourite colour", "Улюблений колір", ColumnType.Choice, Choices: Colors),
        new ColumnDef("colors", "Colours", "Кольори", ColumnType.MultiChoice, Choices: Colors),
        new ColumnDef("phone", "Phone", "Телефон", ColumnType.Text),
        new ColumnDef("status", "Status", "Статус", ColumnType.Text, ForImport: false),
    ]);

    private static MemoryStream Workbook(Action<IXLWorksheet> fill)
    {
        using var workbook = new XLWorkbook();
        fill(workbook.Worksheets.Add("Data"));
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream;
    }

    private static void Header(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
    }

    // ------------------------------------------------------------------ writing
    [Fact]
    public void Exported_headers_are_the_readable_names_in_the_chosen_language()
    {
        var english = Read(SpreadsheetWriter.Write(Schema, FileLanguage.En, [], SheetMode.Export));
        var ukrainian = Read(SpreadsheetWriter.Write(Schema, FileLanguage.Uk, [], SheetMode.Export));

        Assert.Equal("People", english.Worksheet(1).Name);
        Assert.Equal(["Full name", "Date of birth", "Age (years)", "Salary", "Member", "Favourite colour", "Colours", "Phone", "Status"],
            english.Worksheet(1).Row(1).CellsUsed().Select(c => c.GetString()));
        Assert.Equal("Люди", ukrainian.Worksheet(1).Name);
        Assert.Equal("Ім'я та прізвище", ukrainian.Worksheet(1).Cell(1, 1).GetString());
    }

    [Fact]
    public void Values_are_written_as_real_dates_numbers_and_readable_words()
    {
        var bytes = SpreadsheetWriter.Write(Schema, FileLanguage.En,
        [
            new Dictionary<string, object?>
            {
                ["name"] = "Ann", ["born"] = new DateOnly(2001, 3, 25), ["age"] = 24, ["salary"] = 1500.5m, ["member"] = true,
                ["color"] = "DarkBlue", ["colors"] = new[] { "Red", "DarkBlue" }, ["phone"] = "+380501112233", ["status"] = "Active"
            }
        ], SheetMode.Export);

        var sheet = Read(bytes).Worksheet(1);

        Assert.Equal(XLDataType.DateTime, sheet.Cell(2, 2).DataType);   // a date Excel understands, not text
        Assert.Equal(new DateTime(2001, 3, 25), sheet.Cell(2, 2).GetDateTime());
        Assert.Equal(XLDataType.Number, sheet.Cell(2, 3).DataType);
        Assert.Equal(1500.5, sheet.Cell(2, 4).GetDouble());
        Assert.Equal("Yes", sheet.Cell(2, 5).GetString());
        Assert.Equal("Dark blue", sheet.Cell(2, 6).GetString());
        Assert.Equal("Red, Dark blue", sheet.Cell(2, 7).GetString());
        Assert.Equal("+380501112233", sheet.Cell(2, 8).GetString());
    }

    [Fact]
    public void Ukrainian_files_use_ukrainian_words_for_values()
    {
        var bytes = SpreadsheetWriter.Write(Schema, FileLanguage.Uk,
        [
            new Dictionary<string, object?> { ["name"] = "Анна", ["member"] = false, ["color"] = "Red", ["colors"] = new[] { "DarkBlue" } }
        ], SheetMode.Export);

        var sheet = Read(bytes).Worksheet(1);

        Assert.Equal("Ні", sheet.Cell(2, 5).GetString());
        Assert.Equal("Червоний", sheet.Cell(2, 6).GetString());
        Assert.Equal("Темно-синій", sheet.Cell(2, 7).GetString());
    }

    [Fact]
    public void An_empty_export_still_has_the_header_row()
    {
        var sheet = Read(SpreadsheetWriter.Write(Schema, FileLanguage.En, [], SheetMode.Export)).Worksheet(1);

        Assert.Equal("Full name", sheet.Cell(1, 1).GetString());
        Assert.True(sheet.Cell(2, 1).IsEmpty());
    }

    [Fact]
    public void The_template_has_import_columns_only_an_example_drop_downs_and_a_help_sheet()
    {
        var workbook = Read(SpreadsheetWriter.Write(Schema, FileLanguage.En, [], SheetMode.Template));
        var sheet = workbook.Worksheet(1);

        Assert.DoesNotContain("Status", sheet.Row(1).CellsUsed().Select(c => c.GetString()));   // export-only columns are not in the template
        Assert.True(sheet.Cell(2, 1).IsEmpty());                                                 // no example row that could be imported by mistake
        Assert.Contains(sheet.DataValidations, v => v.AllowedValues == XLAllowedValues.List);    // drop-down lists

        var help = workbook.Worksheet("Help");
        Assert.Contains(help.Column(1).CellsUsed(), c => c.GetString() == "Full name");
        Assert.Contains(help.Column(4).CellsUsed(), c => c.GetString() == "Red, Dark blue");     // allowed values are listed
        Assert.Contains(help.Column(5).CellsUsed(), c => c.GetString() == "Ann");                // examples are on the help sheet
    }

    // ------------------------------------------------------------------ reading
    [Fact]
    public void A_written_export_can_be_read_back_unchanged()
    {
        var bytes = SpreadsheetWriter.Write(Schema, FileLanguage.En,
        [
            new Dictionary<string, object?>
            {
                ["name"] = "Ann", ["born"] = new DateOnly(2001, 3, 25), ["age"] = 24, ["salary"] = 1500.5m, ["member"] = true,
                ["color"] = "DarkBlue", ["colors"] = new[] { "Red", "DarkBlue" }, ["phone"] = "+380501112233"
            }
        ], SheetMode.Export);

        var data = SpreadsheetReader.Read(new MemoryStream(bytes), Schema, 100);

        var row = Assert.Single(data.Rows);
        Assert.Empty(row.Errors);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("Ann", row.Values["name"]);
        Assert.Equal(new DateOnly(2001, 3, 25), row.Values["born"]);
        Assert.Equal(24, row.Values["age"]);
        Assert.Equal(1500.5m, row.Values["salary"]);
        Assert.Equal(true, row.Values["member"]);
        Assert.Equal("DarkBlue", row.Values["color"]);
        Assert.Equal(new[] { "Red", "DarkBlue" }, (IEnumerable<string>)row.Values["colors"]!);
        Assert.Equal("+380501112233", row.Values["phone"]);
    }

    [Theory]
    [InlineData("Full name")]
    [InlineData("full name")]
    [InlineData("FULL NAME")]
    [InlineData("  Full   name ")]
    [InlineData("full_name")]
    [InlineData("Ім'я та прізвище")]
    [InlineData("Імя та прізвище")]
    [InlineData("name")]
    public void Headers_are_recognised_whatever_the_spelling_language_or_key(string header)
    {
        using var stream = Workbook(s => { Header(s, header, "Date of birth"); s.Cell(2, 1).Value = "Ann"; s.Cell(2, 2).Value = "2001-03-25"; });

        var data = SpreadsheetReader.Read(stream, Schema, 100);

        Assert.Empty(data.MissingRequiredColumns);
        Assert.Equal("Ann", data.Rows.Single().Values["name"]);
    }

    [Fact]
    public void Text_in_brackets_of_a_header_is_ignored()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Age"); s.Cell(2, 1).Value = "Ann"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = 30; });

        Assert.Equal(30, SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["age"]);
    }

    [Fact]
    public void The_language_of_the_file_is_detected_from_its_headers()
    {
        using var ukrainian = Workbook(s => Header(s, "Ім'я та прізвище", "Дата народження"));
        using var english = Workbook(s => Header(s, "Full name", "Date of birth"));

        Assert.Equal(FileLanguage.Uk, SpreadsheetReader.Read(ukrainian, Schema, 100).Language);
        Assert.Equal(FileLanguage.En, SpreadsheetReader.Read(english, Schema, 100).Language);
    }

    [Fact]
    public void Missing_required_columns_are_named_and_unknown_and_export_only_columns_are_ignored()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Nickname", "Status"); s.Cell(2, 1).Value = "Ann"; });

        var data = SpreadsheetReader.Read(stream, Schema, 100);

        Assert.Equal(["Date of birth"], data.MissingRequiredColumns);
        Assert.Equal(["Nickname"], data.IgnoredColumns);   // "Status" is a known export column and is not reported
        Assert.Empty(data.Rows);
    }

    [Theory]
    [InlineData("2001-03-25")]
    [InlineData("25.03.2001")]
    [InlineData("25/03/2001")]
    [InlineData("2001/03/25")]
    public void Dates_typed_as_text_are_understood_in_the_usual_formats(string text)
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth"); s.Cell(2, 1).Value = "Ann"; s.Cell(2, 2).Value = text; });

        Assert.Equal(new DateOnly(2001, 3, 25), SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["born"]);
    }

    [Fact]
    public void A_bad_date_is_reported_with_the_column_and_an_example()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth"); s.Cell(2, 1).Value = "Ann"; s.Cell(2, 2).Value = "31.02.2001"; });

        var error = Assert.Single(SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Errors);

        Assert.StartsWith("Date of birth:", error);
        Assert.Contains("31.02.2001", error);
        Assert.Contains("2001-03-25", error);
    }

    [Fact]
    public void Errors_are_written_in_the_language_of_the_file()
    {
        using var stream = Workbook(s => { Header(s, "Ім'я та прізвище", "Дата народження"); s.Cell(2, 1).Value = "Анна"; s.Cell(2, 2).Value = "вчора"; });

        var error = Assert.Single(SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Errors);

        Assert.StartsWith("Дата народження:", error);
        Assert.Contains("не є коректною датою", error);
    }

    [Theory]
    [InlineData("Red", "Red")]
    [InlineData("red", "Red")]
    [InlineData("Dark blue", "DarkBlue")]
    [InlineData("darkblue", "DarkBlue")]
    [InlineData("Темно-синій", "DarkBlue")]
    [InlineData("червоний", "Red")]
    public void A_choice_is_recognised_by_value_english_or_ukrainian_name(string typed, string stored)
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Favourite colour"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = typed; });

        Assert.Equal(stored, SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["color"]);
    }

    [Fact]
    public void An_unknown_choice_lists_what_is_allowed()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Favourite colour"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = "Green"; });

        var error = Assert.Single(SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Errors);

        Assert.Contains("'Green' is not allowed", error);
        Assert.Contains("Red, Dark blue", error);
    }

    [Theory]
    [InlineData("Red, Dark blue", new[] { "Red", "DarkBlue" })]
    [InlineData("Red; darkblue", new[] { "Red", "DarkBlue" })]
    [InlineData("Red,Red", new[] { "Red" })]
    [InlineData("Червоний, Темно-синій", new[] { "Red", "DarkBlue" })]
    public void Several_values_in_one_cell_are_split_and_repeats_dropped(string typed, string[] expected)
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Colours"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = typed; });

        Assert.Equal(expected, (IEnumerable<string>)SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["colors"]!);
    }

    [Theory]
    [InlineData("Yes", true)]
    [InlineData("yes", true)]
    [InlineData("Так", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("No", false)]
    [InlineData("Ні", false)]
    [InlineData("0", false)]
    public void Yes_and_no_are_understood_in_both_languages(string typed, bool expected)
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Member"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = typed; });

        Assert.Equal(expected, SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["member"]);
    }

    [Fact]
    public void A_boolean_cell_and_an_unclear_answer()
    {
        using var stream = Workbook(s =>
        {
            Header(s, "Full name", "Date of birth", "Member");
            s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = true;
            s.Cell(3, 1).Value = "B"; s.Cell(3, 2).Value = "2001-03-25"; s.Cell(3, 3).Value = "maybe";
        });

        var rows = SpreadsheetReader.Read(stream, Schema, 100).Rows;

        Assert.Equal(true, rows[0].Values["member"]);
        Assert.Contains("Yes or No", Assert.Single(rows[1].Errors));
    }

    [Theory]
    [InlineData("1500,50", 1500.50)]
    [InlineData("1500.50", 1500.50)]
    [InlineData("1 500", 1500)]
    public void Decimals_accept_a_comma_and_spaces(string typed, double expected)
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Salary"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = typed; });

        Assert.Equal((decimal)expected, SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["salary"]);
    }

    [Fact]
    public void Numbers_that_are_not_numbers_are_reported()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Age", "Salary"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = "12.5"; s.Cell(2, 4).Value = "lots"; });

        var errors = SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Errors;

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.StartsWith("Age") && e.Contains("whole number"));
        Assert.Contains(errors, e => e.StartsWith("Salary") && e.Contains("not a number"));
    }

    [Fact]
    public void A_phone_number_typed_as_a_number_keeps_all_its_digits()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Phone"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; s.Cell(2, 3).Value = 380501112233L; });

        Assert.Equal("380501112233", SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["phone"]);
    }

    [Fact]
    public void Blank_rows_are_skipped_but_row_numbers_stay_those_of_the_sheet()
    {
        using var stream = Workbook(s =>
        {
            Header(s, "Full name", "Date of birth");
            s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25";
            s.Cell(5, 1).Value = "B"; s.Cell(5, 2).Value = "2002-04-26";
        });

        var rows = SpreadsheetReader.Read(stream, Schema, 100).Rows;

        Assert.Equal([2, 5], rows.Select(r => r.RowNumber));
    }

    [Fact]
    public void Empty_optional_cells_are_null()
    {
        using var stream = Workbook(s => { Header(s, "Full name", "Date of birth", "Age"); s.Cell(2, 1).Value = "A"; s.Cell(2, 2).Value = "2001-03-25"; });

        Assert.Null(SpreadsheetReader.Read(stream, Schema, 100).Rows.Single().Values["age"]);
    }

    [Fact]
    public void An_untouched_template_contains_no_rows_and_its_help_sheet_is_never_read_as_data()
    {
        var bytes = SpreadsheetWriter.Write(Schema, FileLanguage.En, [], SheetMode.Template);

        var data = SpreadsheetReader.Read(new MemoryStream(bytes), Schema, 100);

        Assert.Empty(data.Rows);
        Assert.Empty(data.MissingRequiredColumns);
    }

    [Fact]
    public void Files_that_cannot_be_used_are_refused_with_a_readable_message()
    {
        Assert.Contains("not a valid Excel", Assert.Throws<SpreadsheetFormatException>(() =>
            SpreadsheetReader.Read(new MemoryStream("not excel"u8.ToArray()), Schema, 100)).Message);

        using var empty = Workbook(_ => { });
        Assert.Contains("empty", Assert.Throws<SpreadsheetFormatException>(() => SpreadsheetReader.Read(empty, Schema, 100)).Message);

        using var big = Workbook(s => { Header(s, "Full name", "Date of birth"); for (var r = 2; r <= 5; r++) { s.Cell(r, 1).Value = "A"; s.Cell(r, 2).Value = "2001-03-25"; } });
        Assert.Contains("more than 3 rows", Assert.Throws<SpreadsheetFormatException>(() => SpreadsheetReader.Read(big, Schema, 3)).Message);
    }

    private static XLWorkbook Read(byte[] bytes) => new(new MemoryStream(bytes));
}

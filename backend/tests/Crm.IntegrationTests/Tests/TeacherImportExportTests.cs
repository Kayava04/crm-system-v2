using ClosedXML.Excel;

namespace Crm.IntegrationTests.Tests;

public class TeacherImportExportTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static readonly string[] HeadersEn =
        ["First name", "Last name", "Middle name", "Date of birth", "Phone", "Email", "City", "Country", "Base salary", "Rate per lesson", "Comment"];

    private static readonly string[] HeadersUk =
        ["Ім'я", "Прізвище", "По батькові", "Дата народження", "Телефон", "Електронна пошта", "Місто", "Країна", "Базова ставка", "Ставка за заняття", "Коментар"];

    private static object?[] RowEn(string email, string lastName) =>
        ["Olena", lastName, "Petrivna", new DateTime(1988, 7, 14), "+380671234567", email, "Kyiv", "Ukraine", 20000, 350, "speaks German"];

    private static string Tag() => "Zq" + TestData.Unique();

    private Task<ApiResponse> ImportAsync(byte[] file, string name = "teachers.xlsx", string query = "", string? token = null) =>
        Api.UploadAsync("/api/teachers/import" + query, name, file, token ?? Admin);

    private static JsonNode Row(ApiResponse response, int index) => response["rows"].AsArray()[index]!;

    private static string Errors(JsonNode row) => string.Join(" | ", row["errors"]!.AsArray().Select(e => e!.GetValue<string>()));

    private async Task<int> CountAsync(string lastName) =>
        (await Api.GetAsync($"/api/teachers?search={lastName}", Admin))["totalCount"].GetValue<int>();

    // ================================================================== export
    [Fact]
    public async Task Excel_export_has_readable_names_and_the_salary_in_force()
    {
        var tag = Tag();
        var id = (await Api.PostAsync("/api/teachers", Data.TeacherBody(lastName: tag, baseSalary: 1800, lessonsRate: 180), Admin)).Expect(201).Id;
        (await Api.PutAsync($"/api/teachers/{id}/status", new { status = "OnLeave" }, Admin)).Expect(200);
        await Data.TeacherUserAsync(id);

        var file = (await Api.DownloadAsync($"/api/teachers/export?search={tag}", Admin)).Expect(200);

        Assert.Matches(@"^teachers-\d{4}-\d{2}-\d{2}\.xlsx$", file.FileName);
        var sheet = file.Workbook.Worksheet(1);
        Assert.Equal("Teachers", sheet.Name);
        Assert.Equal(HeadersEn.Concat(["Status", "Has an account", "Created"]), sheet.Row(1).CellsUsed().Select(c => c.GetString()));
        Assert.Equal(1800, sheet.Cell(2, 9).GetDouble());
        Assert.Equal(180, sheet.Cell(2, 10).GetDouble());
        Assert.Equal("On leave", sheet.Cell(2, 12).GetString());
        Assert.Equal("Yes", sheet.Cell(2, 13).GetString());
        Assert.Equal(XLDataType.DateTime, sheet.Cell(2, 4).DataType);
    }

    [Fact]
    public async Task Ukrainian_export_and_json_export()
    {
        var tag = Tag();
        var id = (await Api.PostAsync("/api/teachers", Data.TeacherBody(lastName: tag), Admin)).Expect(201).Id;

        var uk = (await Api.DownloadAsync($"/api/teachers/export?search={tag}&lang=uk", Admin)).Expect(200).Workbook.Worksheet(1);
        Assert.Equal("Викладачі", uk.Name);
        Assert.Equal(HeadersUk.Concat(["Статус", "Має акаунт", "Дата створення"]), uk.Row(1).CellsUsed().Select(c => c.GetString()));
        Assert.Equal("Випробувальний термін", uk.Cell(2, 12).GetString());
        Assert.Equal("Ні", uk.Cell(2, 13).GetString());

        var json = (await Api.DownloadAsync($"/api/teachers/export?search={tag}&fileFormat=json", Admin)).Expect(200);
        var teacher = Assert.Single(json.Json.AsArray())!;
        Assert.Equal(id, teacher["id"]!.GetValue<Guid>());
        Assert.Equal(1000m, teacher["baseSalary"]!.GetValue<decimal>());
        Assert.Equal("Probation", teacher["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task Export_can_be_filtered_by_status_and_city_and_needs_permission()
    {
        var tag = Tag();
        var kept = (await Api.PostAsync("/api/teachers", Data.TeacherBody(lastName: tag), Admin)).Expect(201).Id;
        var employed = (await Api.PostAsync("/api/teachers", Data.TeacherBody(lastName: tag), Admin)).Expect(201).Id;
        (await Api.PutAsync($"/api/teachers/{employed}/status", new { status = "Employed" }, Admin)).Expect(200);

        var onlyEmployed = (await Api.DownloadAsync($"/api/teachers/export?search={tag}&status=Employed&fileFormat=json", Admin)).Json.AsArray();
        var nobody = (await Api.DownloadAsync($"/api/teachers/export?search={tag}&city=Nowhere&fileFormat=json", Admin)).Json.AsArray();

        Assert.Equal(employed, Assert.Single(onlyEmployed)!["id"]!.GetValue<Guid>());
        Assert.Empty(nobody);
        Assert.NotEqual(kept, employed);

        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        (await Api.DownloadAsync("/api/teachers/export", teacher.Token)).Expect(403);
        (await Api.DownloadAsync("/api/teachers/export?fileFormat=pdf", Admin)).Expect(400);
    }

    // ================================================================== template
    [Fact]
    public async Task The_template_is_empty_with_headers_and_a_help_sheet_in_both_languages()
    {
        var en = (await Api.DownloadAsync("/api/teachers/import-template", Admin)).Expect(200);
        var uk = (await Api.DownloadAsync("/api/teachers/import-template?lang=uk", Admin)).Expect(200);

        Assert.Equal(HeadersEn, en.Workbook.Worksheet(1).Row(1).CellsUsed().Select(c => c.GetString()));
        Assert.True(en.Workbook.Worksheet(1).Cell(2, 1).IsEmpty());
        Assert.Contains(en.Workbook.Worksheet("Help").CellsUsed(), c => c.GetString().Contains("Paid for each completed lesson"));
        Assert.Equal(HeadersUk, uk.Workbook.Worksheet(1).Row(1).CellsUsed().Select(c => c.GetString()));
        Assert.Contains(uk.Workbook.Worksheet("Довідка").CellsUsed(), c => c.GetString().Contains("Оплата за кожне проведене заняття"));

        (await ImportAsync(en.Content)).Expect(400);   // an untouched template holds no teachers
    }

    // ================================================================== import
    [Fact]
    public async Task An_english_file_is_imported_with_the_first_salary_rate()
    {
        var tag = Tag();
        var email = TestData.Email("import");

        var response = (await ImportAsync(Xlsx.Build(HeadersEn, RowEn(email, tag)))).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
        Assert.Equal(2, Row(response, 0)["row"]!.GetValue<int>());
        var teacher = (await Api.GetAsync($"/api/teachers/{Row(response, 0)["id"]!.GetValue<Guid>()}", Admin)).Expect(200);
        Assert.Equal("Olena", teacher["firstName"].GetValue<string>());
        Assert.Equal("1988-07-14", teacher["dateOfBirth"].GetValue<string>());
        Assert.Equal("Probation", teacher["status"].GetValue<string>());
        Assert.Equal(20000m, teacher["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());
        Assert.Equal(350m, teacher["currentSalaryRate"]!["lessonsRate"]!.GetValue<decimal>());
        Assert.Equal("speaks German", teacher["comment"].GetValue<string>());
    }

    [Fact]
    public async Task A_ukrainian_file_with_decimal_commas_is_imported()
    {
        var tag = Tag();
        object?[] row = ["Андрій", tag, "", "14.07.1988", "+380671234567", TestData.Email("uk"), "Одеса", "Україна", "20 000,50", "350,5", ""];

        var response = (await ImportAsync(Xlsx.Build(HeadersUk, row))).Expect(200);

        Assert.Equal("uk", response["language"].GetValue<string>());
        var teacher = (await Api.GetAsync($"/api/teachers/{Row(response, 0)["id"]!.GetValue<Guid>()}", Admin)).Expect(200);
        Assert.Equal(20000.50m, teacher["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());
        Assert.Equal(350.5m, teacher["currentSalaryRate"]!["lessonsRate"]!.GetValue<decimal>());
        Assert.Equal("Одеса", teacher["city"].GetValue<string>());
    }

    [Fact]
    public async Task Problems_are_reported_per_row_with_column_names()
    {
        var tag = Tag();
        var existing = TestData.Email("existing");
        (await Api.PostAsync("/api/teachers", Data.TeacherBody(email: existing), Admin)).Expect(201);

        object?[] Bad(int column, object? value)
        {
            var row = RowEn(TestData.Email(), tag);
            row[column] = value;
            return row;
        }

        var response = (await ImportAsync(Xlsx.Build(HeadersEn,
            RowEn(TestData.Email(), tag),     // row 2: fine
            Bad(8, "lots"),                   // row 3: salary is not a number
            Bad(8, 0),                        // row 4: salary must be greater than 0
            Bad(9, null),                     // row 5: rate missing
            Bad(3, "2999-01-01"),             // row 6: born in the future
            RowEn(existing, tag)))).Expect(200);   // row 7: email exists

        Assert.Equal(6, response["total"].GetValue<int>());
        Assert.Equal(1, response["succeeded"].GetValue<int>());
        Assert.Contains("Base salary: 'lots' is not a number", Errors(Row(response, 1)));
        Assert.Contains("Base salary: Base salary must be greater than 0", Errors(Row(response, 2)));
        Assert.Equal("Rate per lesson: This field is required.", Errors(Row(response, 3)));
        Assert.StartsWith("Date of birth:", Errors(Row(response, 4)));
        Assert.Contains("Email: Teacher with email", Errors(Row(response, 5)));
        Assert.Equal(1, await CountAsync(tag));
    }

    [Fact]
    public async Task Dry_run_all_or_nothing_missing_columns_and_bad_files()
    {
        var tag = Tag();
        var good = RowEn(TestData.Email(), tag);
        var bad = RowEn("nope", tag);

        var preview = (await ImportAsync(Xlsx.Build(HeadersEn, good, bad), query: "?dryRun=true")).Expect(200);
        Assert.Equal(1, preview["succeeded"].GetValue<int>());
        Assert.Null(Row(preview, 0)["id"]);
        Assert.Equal(0, await CountAsync(tag));

        (await ImportAsync(Xlsx.Build(HeadersEn, good, bad), query: "?allOrNothing=true")).Expect(400);
        Assert.Equal(0, await CountAsync(tag));

        var withoutSalary = HeadersEn.Where(h => !h.Contains("salary", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Contains("Base salary", (await ImportAsync(Xlsx.Build(withoutSalary, good.Where((_, i) => i != 8).ToArray()))).Expect(400).Raw);
        (await ImportAsync(System.Text.Encoding.UTF8.GetBytes("x"), "teachers.csv")).Expect(400);
        (await ImportAsync(System.Text.Encoding.UTF8.GetBytes("x"), "teachers.xlsx")).Expect(400);
    }

    [Fact]
    public async Task An_exported_file_can_be_edited_and_imported_back_in_excel_and_json()
    {
        var tag = Tag();
        for (var i = 0; i < 2; i++)
            (await Api.PostAsync("/api/teachers", Data.TeacherBody(lastName: tag, baseSalary: 2500, lessonsRate: 250), Admin)).Expect(201);

        using var workbook = (await Api.DownloadAsync($"/api/teachers/export?search={tag}", Admin)).Workbook;
        var sheet = workbook.Worksheet(1);
        for (var r = 2; r <= 3; r++)
            sheet.Cell(r, 6).Value = "xlsx-" + sheet.Cell(r, 6).GetString();
        Assert.Equal(2, (await ImportAsync(Xlsx.Save(workbook))).Expect(200)["succeeded"].GetValue<int>());

        var json = (await Api.DownloadAsync($"/api/teachers/export?search={tag}&fileFormat=json", Admin)).Json.AsArray();
        var copies = new JsonArray(json.Select((t, i) =>
        {
            var copy = t!.DeepClone();
            copy["email"] = $"json-{i}-{TestData.Unique()}@example.test";
            return copy;
        }).ToArray());
        var imported = (await ImportAsync(System.Text.Encoding.UTF8.GetBytes(copies.ToJsonString()), "teachers.json")).Expect(200);

        Assert.Equal(4, json.Count);   // 2 originals + 2 imported from Excel
        Assert.Equal(4, imported["succeeded"].GetValue<int>());
        Assert.Equal(8, await CountAsync(tag));
    }

    [Fact]
    public async Task Json_import_reports_problems_per_item_and_needs_permission()
    {
        var tag = Tag();
        var good = new { firstName = "Olena", lastName = tag, dateOfBirth = "1988-07-14", phoneNumber = "+380671234567", email = TestData.Email(), city = "Kyiv", country = "Ukraine", baseSalary = 20000, lessonsRate = 350 };
        var json = System.Text.Json.JsonSerializer.Serialize(new object[] { good, new { firstName = "Only" }, "text" });

        var response = (await ImportAsync(System.Text.Encoding.UTF8.GetBytes(json), "teachers.json")).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
        Assert.Contains("baseSalary: This field is required.", Errors(Row(response, 1)));
        Assert.Contains("must be an object", Errors(Row(response, 2)));

        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        Assert.Equal(403, (await ImportAsync(Xlsx.Build(HeadersEn, RowEn(TestData.Email(), tag)), token: student.Token)).Code);
        Assert.Equal(403, (await ImportAsync(Xlsx.Build(HeadersEn, RowEn(TestData.Email(), tag)), token: teacher.Token)).Code);
        Assert.Equal(401, (await Api.UploadAsync("/api/teachers/import", "teachers.xlsx", Xlsx.Build(HeadersEn))).Code);
    }
}

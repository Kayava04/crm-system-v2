using ClosedXML.Excel;

namespace Crm.IntegrationTests.Tests;

public class StudentImportExportTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static readonly string[] HeadersEn =
    [
        "First name", "Last name", "Middle name", "Date of birth", "Phone", "Email", "City", "Country", "Is a child",
        "Learning goal", "Lesson format", "Lesson type", "Intensity (1-7)", "Current level", "Had previous courses", "Languages", "Comment"
    ];

    private static readonly string[] HeadersUk =
    [
        "Ім'я", "Прізвище", "По батькові", "Дата народження", "Телефон", "Електронна пошта", "Місто", "Країна", "Дитина",
        "Мета навчання", "Формат занять", "Тип занять", "Інтенсивність (1-7)", "Поточний рівень", "Були попередні курси", "Мови, що вивчає", "Коментар"
    ];

    private static object?[] RowEn(string email, string lastName, string city = "Kyiv") =>
        ["Ivan", lastName, "Andriyovych", new DateTime(2001, 3, 25), "+380501112233", email, city, "Ukraine", "No",
         "Work", "Online", "Individual", 3, "B1", "No", "English, German", "prefers evenings"];

    private static string Tag() => "Zq" + TestData.Unique();

    private async Task<ApiResponse> ImportAsync(byte[] file, string name = "students.xlsx", string query = "", string? token = null) =>
        await Api.UploadAsync("/api/students/import" + query, name, file, token ?? Admin);

    private static JsonNode Row(ApiResponse response, int index) => response["rows"].AsArray()[index]!;

    private static string Errors(JsonNode row) => string.Join(" | ", row["errors"]!.AsArray().Select(e => e!.GetValue<string>()));

    private async Task<int> CountAsync(string lastName) =>
        (await Api.GetAsync($"/api/students?search={lastName}", Admin))["totalCount"].GetValue<int>();

    // ================================================================== export
    [Fact]
    public async Task Excel_export_has_readable_english_column_names_and_real_values()
    {
        var tag = Tag();
        var withAccount = (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag, email: TestData.Email("a")), Admin)).Expect(201).Id;
        await Data.StudentUserAsync(withAccount);
        (await Api.PutAsync($"/api/students/{withAccount}/status", new { status = "Suspended" }, Admin)).Expect(200);
        (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag, email: TestData.Email("b")), Admin)).Expect(201);

        var file = (await Api.DownloadAsync($"/api/students/export?search={tag}", Admin)).Expect(200);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Matches(@"^students-\d{4}-\d{2}-\d{2}\.xlsx$", file.FileName);
        var sheet = file.Workbook.Worksheet(1);
        Assert.Equal("Students", sheet.Name);
        Assert.Equal(HeadersEn.Concat(["Status", "Has an account", "Created"]), sheet.Row(1).CellsUsed().Select(c => c.GetString()));

        var rows = Enumerable.Range(2, 2).Select(r => sheet.Row(r)).ToList();
        var suspended = rows.Single(r => r.Cell(18).GetString() == "Suspended");
        Assert.Equal("Yes", suspended.Cell(19).GetString());            // has an account
        Assert.Equal(XLDataType.DateTime, suspended.Cell(4).DataType);   // the date of birth is a real Excel date
        Assert.Equal(new DateTime(2000, 1, 1), suspended.Cell(4).GetDateTime());
        Assert.Equal("Work", suspended.Cell(10).GetString());
        Assert.Equal("Online", suspended.Cell(11).GetString());
        Assert.Equal("A2", suspended.Cell(14).GetString());
        Assert.Equal("English", suspended.Cell(16).GetString());
        Assert.Equal("No", suspended.Cell(9).GetString());
    }

    [Fact]
    public async Task Excel_export_in_ukrainian_uses_ukrainian_names_and_values()
    {
        var tag = Tag();
        (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag), Admin)).Expect(201);

        var file = (await Api.DownloadAsync($"/api/students/export?search={tag}&lang=uk", Admin)).Expect(200);

        var sheet = file.Workbook.Worksheet(1);
        Assert.Equal("Студенти", sheet.Name);
        Assert.Equal(HeadersUk.Concat(["Статус", "Має акаунт", "Дата створення"]), sheet.Row(1).CellsUsed().Select(c => c.GetString()));
        Assert.Equal("Робота", sheet.Cell(2, 10).GetString());
        Assert.Equal("Онлайн", sheet.Cell(2, 11).GetString());
        Assert.Equal("Індивідуальні", sheet.Cell(2, 12).GetString());
        Assert.Equal("Англійська", sheet.Cell(2, 16).GetString());
        Assert.Equal("Активний", sheet.Cell(2, 18).GetString());
        Assert.Equal("Ні", sheet.Cell(2, 19).GetString());
    }

    [Fact]
    public async Task Json_export_has_the_student_fields_status_and_id()
    {
        var tag = Tag();
        var id = (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag), Admin)).Expect(201).Id;

        var file = (await Api.DownloadAsync($"/api/students/export?search={tag}&fileFormat=json", Admin)).Expect(200);

        Assert.Equal("application/json", file.ContentType);
        Assert.EndsWith(".json", file.FileName);
        var student = Assert.Single(file.Json.AsArray())!;
        Assert.Equal(id, student["id"]!.GetValue<Guid>());
        Assert.Equal(tag, student["lastName"]!.GetValue<string>());
        Assert.Equal("Work", student["learningGoal"]!.GetValue<string>());
        Assert.Equal("Online", student["format"]!.GetValue<string>());
        Assert.Equal("English", student["languages"]![0]!.GetValue<string>());
        Assert.Equal("Active", student["status"]!.GetValue<string>());
        Assert.False(student["hasAccount"]!.GetValue<bool>());
        Assert.Equal("2000-01-01", student["dateOfBirth"]!.GetValue<string>());
    }

    [Fact]
    public async Task Export_accepts_the_same_filters_as_the_list()
    {
        var tag = Tag();
        (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag), Admin)).Expect(201);
        var child = (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag, isChild: true), Admin)).Expect(201).Id;

        var all = (await Api.DownloadAsync($"/api/students/export?search={tag}&fileFormat=json", Admin)).Json.AsArray();
        var children = (await Api.DownloadAsync($"/api/students/export?search={tag}&isChild=true&fileFormat=json", Admin)).Json.AsArray();
        var nobody = (await Api.DownloadAsync($"/api/students/export?search={tag}&city=Nowhere&fileFormat=json", Admin)).Json.AsArray();
        var german = (await Api.DownloadAsync($"/api/students/export?search={tag}&language=German&fileFormat=json", Admin)).Json.AsArray();

        Assert.Equal(2, all.Count);
        Assert.Equal(child, Assert.Single(children)!["id"]!.GetValue<Guid>());
        Assert.Empty(nobody);
        Assert.Empty(german);
    }

    [Fact]
    public async Task An_export_with_no_students_is_still_a_valid_file()
    {
        var xlsx = (await Api.DownloadAsync($"/api/students/export?search={Tag()}", Admin)).Expect(200);
        var json = (await Api.DownloadAsync($"/api/students/export?search={Tag()}&fileFormat=json", Admin)).Expect(200);

        Assert.Equal("First name", xlsx.Workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Empty(json.Json.AsArray());
    }

    [Fact]
    public async Task Export_rejects_unknown_options_and_needs_permission()
    {
        (await Api.DownloadAsync("/api/students/export?fileFormat=pdf", Admin)).Expect(400);
        (await Api.DownloadAsync("/api/students/export?lang=fr", Admin)).Expect(400);

        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        (await Api.DownloadAsync("/api/students/export", student.Token)).Expect(403);
        (await Api.DownloadAsync("/api/students/export", teacher.Token)).Expect(403);
        (await Api.DownloadAsync("/api/students/export")).Expect(401);
    }

    // ================================================================== template
    [Theory]
    [InlineData("en", "Students", "First name", "Help")]
    [InlineData("uk", "Студенти", "Ім'я", "Довідка")]
    public async Task The_template_is_an_empty_sheet_with_headers_drop_downs_and_a_help_sheet(string lang, string sheetName, string firstHeader, string helpName)
    {
        var file = (await Api.DownloadAsync($"/api/students/import-template?lang={lang}", Admin)).Expect(200);

        var workbook = file.Workbook;
        var sheet = workbook.Worksheet(1);
        Assert.Equal(sheetName, sheet.Name);
        Assert.Equal(firstHeader, sheet.Cell(1, 1).GetString());
        Assert.Equal((lang == "en" ? HeadersEn : HeadersUk).Length, sheet.Row(1).CellsUsed().Count());
        Assert.True(sheet.Cell(2, 1).IsEmpty());                                     // nothing that could be imported by mistake
        Assert.True(sheet.DataValidations.Any());                                    // drop-down lists for the fixed values
        Assert.Contains(workbook.Worksheet(helpName).Column(1).CellsUsed(), c => c.GetString() == firstHeader);
        Assert.Matches(@"^students-import-template\.xlsx$", file.FileName);
    }

    [Fact]
    public async Task The_template_lists_the_allowed_values_on_the_help_sheet()
    {
        var help = (await Api.DownloadAsync("/api/students/import-template", Admin)).Workbook.Worksheet("Help");

        var text = string.Join("\n", help.CellsUsed().Select(c => c.GetString()));
        Assert.Contains("Work, Relocation, Study, Personal", text);
        Assert.Contains("English, French, German, Polish, Spanish, Italian", text);
        Assert.Contains("A1, A2, B1, B2, C1, C2", text);
    }

    [Fact]
    public async Task An_untouched_template_is_refused_because_it_holds_no_students()
    {
        var template = (await Api.DownloadAsync("/api/students/import-template", Admin)).Content;

        var response = (await ImportAsync(template)).Expect(400);

        Assert.Contains("no students", response.Raw);
    }

    [Fact]
    public async Task The_json_sample_can_be_imported_as_it_is_after_changing_the_email()
    {
        var sample = (await Api.DownloadAsync("/api/students/import-template?fileFormat=json", Admin)).Expect(200);
        var array = sample.Json.AsArray();
        array[0]!["email"] = TestData.Email();
        array[0]!["lastName"] = Tag();

        var response = (await ImportAsync(System.Text.Encoding.UTF8.GetBytes(array.ToJsonString()), "students.json")).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
    }

    [Fact]
    public async Task Template_needs_permission_and_a_known_format()
    {
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        (await Api.DownloadAsync("/api/students/import-template", teacher.Token)).Expect(403);
        (await Api.DownloadAsync("/api/students/import-template?fileFormat=doc", Admin)).Expect(400);
    }

    // ================================================================== import from Excel
    [Fact]
    public async Task An_english_file_is_imported_and_every_field_reaches_the_student()
    {
        var tag = Tag();
        var email = TestData.Email("import");

        var response = (await ImportAsync(Xlsx.Build(HeadersEn, RowEn(email, tag)))).Expect(200);

        Assert.Equal("xlsx", response["fileType"].GetValue<string>());
        Assert.Equal("en", response["language"].GetValue<string>());
        Assert.Equal(1, response["succeeded"].GetValue<int>());
        var row = Row(response, 0);
        Assert.Equal(2, row["row"]!.GetValue<int>());            // the row number in the sheet
        Assert.Equal(email, row["key"]!.GetValue<string>());

        var student = (await Api.GetAsync($"/api/students/{row["id"]!.GetValue<Guid>()}", Admin)).Expect(200);
        Assert.Equal("Ivan", student["firstName"].GetValue<string>());
        Assert.Equal("Andriyovych", student["middleName"].GetValue<string>());
        Assert.Equal("2001-03-25", student["dateOfBirth"].GetValue<string>());
        Assert.Equal("Kyiv", student["city"].GetValue<string>());
        Assert.False(student["isChild"].GetValue<bool>());
        Assert.Equal("prefers evenings", student["comment"].GetValue<string>());
        Assert.Equal("Work", student["preferences"]!["learningGoal"]!.GetValue<string>());
        Assert.Equal("Online", student["preferences"]!["format"]!.GetValue<string>());
        Assert.Equal("Individual", student["preferences"]!["lessonType"]!.GetValue<string>());
        Assert.Equal(3, student["preferences"]!["intensity"]!.GetValue<int>());
        Assert.Equal("B1", student["preferences"]!["currentLevel"]!.GetValue<string>());
        Assert.Equal(["English", "German"], student["languages"].AsArray().Select(l => l!.GetValue<string>()).Order());
        Assert.Equal("Active", student["status"].GetValue<string>());
    }

    [Fact]
    public async Task A_ukrainian_file_with_ukrainian_words_and_dotted_dates_is_imported()
    {
        var tag = Tag();
        object?[] row = ["Олена", tag, "Петрівна", "14.07.2010", "+380671234567", TestData.Email("uk"), "Львів", "Україна", "Так",
                         "Навчання", "Офлайн", "Групові", 5, "a2", "Так", "Французька; німецька", "діти"];

        var response = (await ImportAsync(Xlsx.Build(HeadersUk, row))).Expect(200);

        Assert.Equal("uk", response["language"].GetValue<string>());
        var student = (await Api.GetAsync($"/api/students/{Row(response, 0)["id"]!.GetValue<Guid>()}", Admin)).Expect(200);
        Assert.Equal("2010-07-14", student["dateOfBirth"].GetValue<string>());
        Assert.True(student["isChild"].GetValue<bool>());
        Assert.Equal("Study", student["preferences"]!["learningGoal"]!.GetValue<string>());
        Assert.Equal("Offline", student["preferences"]!["format"]!.GetValue<string>());
        Assert.Equal("Group", student["preferences"]!["lessonType"]!.GetValue<string>());
        Assert.Equal("A2", student["preferences"]!["currentLevel"]!.GetValue<string>());
        Assert.True(student["preferences"]!["hadPreviousCourses"]!.GetValue<bool>());
        Assert.Equal(["French", "German"], student["languages"].AsArray().Select(l => l!.GetValue<string>()).Order());
    }

    [Fact]
    public async Task Headers_may_be_written_in_any_case_and_mixed_languages_and_extra_columns_are_reported()
    {
        var tag = Tag();
        string[] headers = ["first name", "LAST NAME", "Дата народження", "phone", "Email", "city", "country", "learning goal", "lesson format", "lesson type",
                            "intensity", "current level", "languages", "Nickname"];
        object?[] row = ["Ivan", tag, new DateTime(2001, 3, 25), "+380501112233", TestData.Email(), "Kyiv", "Ukraine", "work", "online", "individual", 3, "b1", "english", "Vanya"];

        var response = (await ImportAsync(Xlsx.Build(headers, row))).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
        Assert.Equal(["Nickname"], response["ignoredColumns"].AsArray().Select(c => c!.GetValue<string>()));
    }

    [Fact]
    public async Task Each_problem_is_reported_with_the_row_and_the_column_name_and_good_rows_are_still_saved()
    {
        var tag = Tag();
        var existing = TestData.Email("existing");
        (await Api.PostAsync("/api/students", Data.StudentBody(email: existing), Admin)).Expect(201);
        var repeated = TestData.Email("repeated");

        object?[] Bad(int column, object? value, string email)
        {
            var row = RowEn(email, tag);
            row[column] = value;
            return row;
        }

        var file = Xlsx.Build(HeadersEn,
            RowEn(TestData.Email(), tag),                    // row 2: fine
            Bad(3, "31.02.2001", TestData.Email()),          // row 3: impossible date
            Bad(13, "Z9", TestData.Email()),                 // row 4: unknown level
            Bad(1, null, TestData.Email()),                  // row 5: last name missing
            Bad(4, "abc", TestData.Email()),                 // row 6: not a phone number
            Bad(12, "twelve", TestData.Email()),             // row 7: intensity is not a number
            RowEn(existing, tag),                            // row 8: email already in the system
            RowEn(repeated, tag),                            // row 9: fine
            RowEn(repeated, tag),                            // row 10: same email again
            Bad(12, 9, TestData.Email()),                    // row 11: intensity out of range
            Bad(15, "English, Klingon", TestData.Email()));  // row 12: unknown language

        var response = (await ImportAsync(file)).Expect(200);

        Assert.Equal(11, response["total"].GetValue<int>());
        Assert.Equal(2, response["succeeded"].GetValue<int>());
        Assert.Equal(9, response["failed"].GetValue<int>());
        Assert.Equal(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, response["rows"].AsArray().Select(r => r!["row"]!.GetValue<int>()));

        Assert.Contains("Date of birth: '31.02.2001' is not a valid date", Errors(Row(response, 1)));
        Assert.Contains("Current level: 'Z9' is not allowed. Allowed values: A1, A2, B1, B2, C1, C2", Errors(Row(response, 2)));
        Assert.Equal("Last name: This field is required.", Errors(Row(response, 3)));
        Assert.StartsWith("Phone:", Errors(Row(response, 4)));
        Assert.Contains("Intensity (1-7): 'twelve' is not a whole number", Errors(Row(response, 5)));
        Assert.Contains("Email: Student with email", Errors(Row(response, 6)));
        Assert.True(Row(response, 7)["success"]!.GetValue<bool>());
        Assert.Contains("repeated in this request", Errors(Row(response, 8)));
        Assert.StartsWith("Intensity (1-7):", Errors(Row(response, 9)));
        Assert.Contains("Languages: 'Klingon' is not allowed", Errors(Row(response, 10)));
        Assert.Equal(2, await CountAsync(tag));   // rows 2 and 9 were saved, nothing else
    }

    [Fact]
    public async Task Several_problems_in_one_row_are_all_reported_at_once()
    {
        var tag = Tag();
        var row = RowEn(TestData.Email(), tag);
        row[3] = "yesterday";
        row[4] = "abc";
        row[13] = "Z9";

        var response = (await ImportAsync(Xlsx.Build(HeadersEn, row))).Expect(200);

        var errors = Row(response, 0)["errors"]!.AsArray().Select(e => e!.GetValue<string>()).ToList();
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.StartsWith("Date of birth:"));
        Assert.Contains(errors, e => e.StartsWith("Phone:"));
        Assert.Contains(errors, e => e.StartsWith("Current level:"));
    }

    [Fact]
    public async Task Errors_in_a_ukrainian_file_use_ukrainian_column_names_and_messages()
    {
        var row = RowEn(TestData.Email(), Tag());
        row[3] = "вчора";
        row[13] = "Z9";

        var response = (await ImportAsync(Xlsx.Build(HeadersUk, row))).Expect(200);

        var errors = Errors(Row(response, 0));
        Assert.Contains("Дата народження: «вчора» не є коректною датою", errors);
        Assert.Contains("Поточний рівень: «Z9» не підходить", errors);
    }

    [Fact]
    public async Task Required_columns_that_are_missing_are_named_and_nothing_is_imported()
    {
        var tag = Tag();
        var headers = HeadersEn.Where(h => h != "Email" && h != "City").ToArray();
        var row = RowEn(TestData.Email(), tag).Where((_, i) => HeadersEn[i] != "Email" && HeadersEn[i] != "City").ToArray();

        var response = (await ImportAsync(Xlsx.Build(headers, row))).Expect(400);

        Assert.Contains("Email", response.Raw);
        Assert.Contains("City", response.Raw);
        Assert.Equal(0, await CountAsync(tag));
    }

    [Fact]
    public async Task Files_that_cannot_be_used_are_refused_with_a_clear_message()
    {
        (await ImportAsync(Xlsx.Build(HeadersEn), "students.xlsx")).Expect(400);                                            // header only
        (await ImportAsync(System.Text.Encoding.UTF8.GetBytes("a,b,c"), "students.csv")).Expect(400);                        // unsupported type
        Assert.Contains("not a valid Excel", (await ImportAsync(System.Text.Encoding.UTF8.GetBytes("not excel"), "students.xlsx")).Expect(400).Raw);
        (await ImportAsync([], "students.xlsx")).Expect(400);                                                                // empty upload
        (await Api.PostAsync("/api/students/import", new { }, Admin)).Expect(415);                                            // not a form at all
        (await ImportAsync(new byte[6 * 1024 * 1024], "students.xlsx")).Expect(400);                                          // too large
    }

    [Fact]
    public async Task Too_many_rows_are_refused()
    {
        var rows = Enumerable.Range(0, 2001).Select(_ => RowEn(TestData.Email(), "Zq")).ToArray();

        var response = (await ImportAsync(Xlsx.Build(HeadersEn, rows))).Expect(400);

        Assert.Contains("more than 2000 rows", response.Raw);
    }

    [Fact]
    public async Task A_dry_run_checks_the_file_and_saves_nothing()
    {
        var tag = Tag();
        var file = Xlsx.Build(HeadersEn, RowEn(TestData.Email(), tag), RowEn("not-an-email", tag));

        var preview = (await ImportAsync(file, query: "?dryRun=true")).Expect(200);

        Assert.True(preview["dryRun"].GetValue<bool>());
        Assert.Equal(1, preview["succeeded"].GetValue<int>());
        Assert.True(Row(preview, 0)["success"]!.GetValue<bool>());
        Assert.Null(Row(preview, 0)["id"]);                       // nothing exists yet
        Assert.False(Row(preview, 1)["success"]!.GetValue<bool>());
        Assert.Equal(0, await CountAsync(tag));

        var real = (await ImportAsync(file)).Expect(200);
        Assert.False(real["dryRun"].GetValue<bool>());
        Assert.Equal(1, await CountAsync(tag));
    }

    [Fact]
    public async Task With_all_or_nothing_one_bad_row_stops_the_whole_file()
    {
        var tag = Tag();
        var file = Xlsx.Build(HeadersEn, RowEn(TestData.Email(), tag), RowEn("not-an-email", tag));

        var response = (await ImportAsync(file, query: "?allOrNothing=true")).Expect(400);

        Assert.True(response["allOrNothing"].GetValue<bool>());
        Assert.Equal(0, response["succeeded"].GetValue<int>());
        Assert.Contains("all-or-nothing", Errors(Row(response, 0)));   // the good row is told why it was not saved
        Assert.StartsWith("Email:", Errors(Row(response, 1)));
        Assert.Equal(0, await CountAsync(tag));
    }

    [Fact]
    public async Task Blank_rows_are_skipped_and_row_numbers_still_match_the_sheet()
    {
        var tag = Tag();
        using var workbook = new XLWorkbook(new MemoryStream(Xlsx.Build(HeadersEn, RowEn(TestData.Email(), tag))));
        var sheet = workbook.Worksheet(1);
        for (var c = 0; c < HeadersEn.Length; c++)
            sheet.Cell(5, c + 1).Value = XLCellValue.FromObject(RowEn(TestData.Email(), tag)[c] ?? "");

        var response = (await ImportAsync(Xlsx.Save(workbook))).Expect(200);

        Assert.Equal([2, 5], response["rows"].AsArray().Select(r => r!["row"]!.GetValue<int>()));
    }

    [Fact]
    public async Task A_phone_number_typed_as_a_number_keeps_all_its_digits()
    {
        var tag = Tag();
        var row = RowEn(TestData.Email(), tag);
        row[4] = 380501112233L;

        var response = (await ImportAsync(Xlsx.Build(HeadersEn, row))).Expect(200);

        Assert.Equal("380501112233", (await Api.GetAsync($"/api/students/{Row(response, 0)["id"]!.GetValue<Guid>()}", Admin))["phoneNumber"].GetValue<string>());
    }

    [Fact]
    public async Task An_exported_file_can_be_edited_and_imported_back()
    {
        var tag = Tag();
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
            ids.Add((await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag, email: TestData.Email("orig")), Admin)).Expect(201).Id);

        var exported = (await Api.DownloadAsync($"/api/students/export?search={tag}", Admin)).Expect(200);
        using var workbook = exported.Workbook;
        var sheet = workbook.Worksheet(1);
        for (var r = 2; r <= 4; r++)
            sheet.Cell(r, 6).Value = "copy-" + sheet.Cell(r, 6).GetString();   // emails must stay unique

        var response = (await ImportAsync(Xlsx.Save(workbook))).Expect(200);

        Assert.Equal(3, response["succeeded"].GetValue<int>());
        Assert.Empty(response["ignoredColumns"].AsArray());   // the export-only columns (status, created...) are known and silently ignored
        Assert.Equal(6, await CountAsync(tag));
        var copy = (await Api.GetAsync($"/api/students/{Row(response, 0)["id"]!.GetValue<Guid>()}", Admin)).Expect(200);
        Assert.Equal("Work", copy["preferences"]!["learningGoal"]!.GetValue<string>());
        Assert.Equal("A2", copy["preferences"]!["currentLevel"]!.GetValue<string>());
        Assert.Equal("Active", copy["status"].GetValue<string>());   // status is not copied: a new student starts active
    }

    [Fact]
    public async Task Import_needs_permission()
    {
        var file = Xlsx.Build(HeadersEn, RowEn(TestData.Email(), Tag()));
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        Assert.Equal(403, (await ImportAsync(file, token: teacher.Token)).Code);
        Assert.Equal(403, (await ImportAsync(file, token: student.Token)).Code);
        Assert.Equal(401, (await Api.UploadAsync("/api/students/import", "students.xlsx", file)).Code);
    }

    // ================================================================== import from JSON
    private Task<ApiResponse> ImportJsonAsync(string json, string query = "") =>
        ImportAsync(System.Text.Encoding.UTF8.GetBytes(json), "students.json", query);

    private static object Student(string tag, string? email = null) => new
    {
        firstName = "Ivan", lastName = tag, dateOfBirth = "2001-03-25", phoneNumber = "+380501112233", email = email ?? TestData.Email(),
        city = "Kyiv", country = "Ukraine", learningGoal = "work", format = "ONLINE", lessonType = "Individual",
        intensity = 3, currentLevel = "B1", languages = new[] { "English" }
    };

    [Fact]
    public async Task A_json_array_is_imported_and_enum_names_are_case_insensitive()
    {
        var tag = Tag();

        var response = (await ImportJsonAsync(System.Text.Json.JsonSerializer.Serialize(new[] { Student(tag), Student(tag) }))).Expect(200);

        Assert.Equal("json", response["fileType"].GetValue<string>());
        Assert.Equal(2, response["succeeded"].GetValue<int>());
        Assert.Equal([1, 2], response["rows"].AsArray().Select(r => r!["row"]!.GetValue<int>()));   // 1-based position in the file
        Assert.Equal(2, await CountAsync(tag));
    }

    [Fact]
    public async Task A_json_object_with_a_students_array_is_accepted_too()
    {
        var tag = Tag();

        var response = (await ImportJsonAsync(System.Text.Json.JsonSerializer.Serialize(new { students = new[] { Student(tag) } }))).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
    }

    [Fact]
    public async Task Json_problems_are_reported_per_item()
    {
        var tag = Tag();
        var json = $$"""
        [
          {{System.Text.Json.JsonSerializer.Serialize(Student(tag))}},
          { "firstName": "Only", "lastName": "{{tag}}" },
          {{System.Text.Json.JsonSerializer.Serialize(new { firstName = "Bad", lastName = tag, dateOfBirth = "2001-03-25", phoneNumber = "+380501112233", email = TestData.Email(), city = "Kyiv", country = "Ukraine", learningGoal = "Nonsense", format = "Online", lessonType = "Individual", intensity = 3, currentLevel = "B1", languages = new[] { "English" } })}},
          "not an object"
        ]
        """;

        var response = (await ImportJsonAsync(json)).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
        Assert.Contains("email: This field is required.", Errors(Row(response, 1)));
        Assert.Contains("learningGoal: This field is required.", Errors(Row(response, 1)));
        Assert.Contains("Invalid data", Errors(Row(response, 2)));
        Assert.Contains("must be an object", Errors(Row(response, 3)));
        Assert.Equal(1, await CountAsync(tag));
    }

    [Fact]
    public async Task Json_files_that_are_not_usable_are_refused()
    {
        Assert.Contains("not valid JSON", (await ImportJsonAsync("{ broken")).Expect(400).Raw);
        Assert.Contains("array of students", (await ImportJsonAsync("{\"hello\": 1}")).Expect(400).Raw);
        Assert.Contains("no students", (await ImportJsonAsync("[]")).Expect(400).Raw);
    }

    [Fact]
    public async Task A_json_export_can_be_edited_and_imported_back()
    {
        var tag = Tag();
        (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag, email: TestData.Email("orig")), Admin)).Expect(201);
        var exported = (await Api.DownloadAsync($"/api/students/export?search={tag}&fileFormat=json", Admin)).Json.AsArray();
        exported[0]!["email"] = TestData.Email("copy");

        var response = (await ImportJsonAsync(exported.ToJsonString())).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());   // the extra fields (id, status...) are simply ignored
        Assert.Equal(2, await CountAsync(tag));
    }

    [Fact]
    public async Task A_json_dry_run_and_all_or_nothing_work_like_the_excel_ones()
    {
        var tag = Tag();
        var json = System.Text.Json.JsonSerializer.Serialize(new object[] { Student(tag), Student(tag, "broken") });

        Assert.Equal(1, (await ImportJsonAsync(json, "?dryRun=true")).Expect(200)["succeeded"].GetValue<int>());
        Assert.Equal(0, await CountAsync(tag));

        (await ImportJsonAsync(json, "?allOrNothing=true")).Expect(400);
        Assert.Equal(0, await CountAsync(tag));
    }
}

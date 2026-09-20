using Education.Contracts.Enums;
using Shared.Files;
using Students.Application.Services;
using Students.Domain.Enums;
using Teachers.Application.Services;
using Teachers.Domain.Enums;

namespace Crm.UnitTests.Files;

public class TablesAndTransferTests
{
    // ------------------------------------------------------------------ every value of an enum has a readable name
    private static void AssertCovers<TEnum>(IEnumerable<Choice> choices) where TEnum : struct, Enum
    {
        Assert.Equal(Enum.GetNames<TEnum>().Order(), choices.Select(c => c.Value).Order());
        Assert.All(choices, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.En));
            Assert.False(string.IsNullOrWhiteSpace(c.Uk));
        });
    }

    [Fact]
    public void Every_enum_value_used_in_files_has_an_english_and_a_ukrainian_name()
    {
        AssertCovers<LearningGoal>(StudentTable.LearningGoals);
        AssertCovers<Format>(StudentTable.Formats);
        AssertCovers<LessonType>(StudentTable.LessonTypes);
        AssertCovers<Level>(StudentTable.Levels);
        AssertCovers<Language>(StudentTable.Languages);
        AssertCovers<StudentStatus>(StudentTable.Statuses);
        AssertCovers<TeacherStatus>(TeacherTable.Statuses);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Column_names_are_unique_and_readable_in_both_languages(bool student)
    {
        var schema = student ? StudentTable.Schema : TeacherTable.Schema;

        foreach (var language in new[] { FileLanguage.En, FileLanguage.Uk })
        {
            var headers = schema.Columns.Select(c => c.Header(language)).ToList();

            Assert.Equal(headers.Count, headers.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(headers, h => Assert.DoesNotContain(h, new[] { "", " " }));
            // no technical names like "firstName" or "phoneNumber" in what a person sees
            Assert.All(headers, h => Assert.DoesNotMatch("^[a-z]+[A-Z]", h));
        }

        Assert.All(schema.Columns.Where(c => c.ForImport), c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.ExampleEn), c.Key);
            Assert.False(string.IsNullOrWhiteSpace(c.ExampleUk), c.Key);
        });
    }

    [Fact]
    public void Export_only_columns_are_not_required_and_import_columns_have_keys_that_match_the_json_fields()
    {
        Assert.All(StudentTable.Schema.Columns.Where(c => !c.ForImport), c => Assert.False(c.Required));
        Assert.All(TeacherTable.Schema.Columns.Where(c => !c.ForImport), c => Assert.False(c.Required));

        var studentKeys = typeof(Students.Application.Features.CreateStudent.CreateRequest).GetProperties()
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..]);
        Assert.Equal(studentKeys.Order(), StudentTable.Schema.Columns.Where(c => c.ForImport).Select(c => c.Key).Order());

        var teacherKeys = typeof(Teachers.Application.Features.CreateTeacher.CreateTeacherRequest).GetProperties()
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..]);
        Assert.Equal(teacherKeys.Order(), TeacherTable.Schema.Columns.Where(c => c.ForImport).Select(c => c.Key).Order());
    }

    // ------------------------------------------------------------------ an exported student can be read back
    [Fact]
    public void A_student_written_to_excel_and_read_back_becomes_the_same_create_request()
    {
        var student = Students.Domain.Entities.Student.Create("Ivan", "Petrenko", "Andriyovych", new DateOnly(2001, 3, 25), "+380501112233",
            "ivan@example.test", "Kyiv", "Ukraine", false, "note");
        student.SetPreferences(Students.Domain.Entities.StudentPreferences.Create(student.Id, LearningGoal.Study, Format.Offline, LessonType.Group, 5, Level.C1, true));
        student.AddLanguage(Language.French);
        student.AddLanguage(Language.Polish);

        var bytes = SpreadsheetWriter.Write(StudentTable.Schema, FileLanguage.Uk, [StudentTransfer.ToRow(student)], SheetMode.Export);
        var data = SpreadsheetReader.Read(new MemoryStream(bytes), StudentTable.Schema, 10);
        var item = StudentTransfer.FromRow(data.Rows.Single(), data.Language);

        Assert.Empty(item.Errors!);
        var request = item.Request!;
        Assert.Equal(("Ivan", "Petrenko", "Andriyovych"), (request.FirstName, request.LastName, request.MiddleName));
        Assert.Equal(new DateOnly(2001, 3, 25), request.DateOfBirth);
        Assert.Equal("ivan@example.test", request.Email);
        Assert.Equal((LearningGoal.Study, Format.Offline, LessonType.Group, 5, Level.C1, true),
            (request.LearningGoal, request.Format, request.LessonType, request.Intensity, request.CurrentLevel, request.HadPreviousCourses));
        Assert.Equal([Language.French, Language.Polish], request.Languages);
        Assert.Equal("note", request.Comment);
    }

    [Fact]
    public void A_missing_required_value_is_reported_once_and_not_again_by_validation()
    {
        var bytes = SpreadsheetWriter.Write(StudentTable.Schema, FileLanguage.En,
            [new Dictionary<string, object?> { ["firstName"] = "Ivan" }], SheetMode.Export);
        var data = SpreadsheetReader.Read(new MemoryStream(bytes), StudentTable.Schema, 10);

        var item = StudentTransfer.FromRow(data.Rows.Single(), data.Language);

        Assert.Contains("Last name: This field is required.", item.Errors!);
        Assert.Contains("lastName", item.HandledKeys!);   // validation will skip it
        Assert.DoesNotContain("firstName", item.HandledKeys!);
    }

    [Fact]
    public void Json_items_missing_required_fields_are_flagged_and_the_rest_is_kept()
    {
        var (items, rows) = StudentTransfer.ParseJson(new MemoryStream("""[{"firstName":"Ivan"}, {"firstName":"Anna"}]"""u8.ToArray()));

        Assert.Equal([1, 2], rows);
        Assert.Contains("lastName: This field is required.", items[0].Errors!);
        Assert.Contains("email", items[1].HandledKeys!);
    }

    [Fact]
    public void Json_problems_at_file_level_throw_a_readable_message()
    {
        Assert.Contains("not valid JSON", Assert.Throws<ImportFileException>(() => StudentTransfer.ParseJson(new MemoryStream("{"u8.ToArray()))).Message);
        Assert.Contains("array of teachers", Assert.Throws<ImportFileException>(() => TeacherTransfer.ParseJson(new MemoryStream("{}"u8.ToArray()))).Message);
        Assert.Contains("no students", Assert.Throws<ImportFileException>(() => StudentTransfer.ParseJson(new MemoryStream("[]"u8.ToArray()))).Message);
    }

    [Fact]
    public void Validator_messages_are_shown_with_the_column_name_the_person_knows()
    {
        var errors = new[] { "PhoneNumber: too short", "Languages[1]: Invalid language.", "Email: taken", "Unknown: whatever", "no colon" };

        Assert.Equal(["Phone: too short", "Languages: Invalid language.", "Email: taken", "Unknown: whatever", "no colon"],
            ImportErrors.UseColumnNames(errors, StudentTable.Schema, FileLanguage.En).Select(e => e.Replace("Мови, що вивчає", "Languages")));
        Assert.Equal("Телефон: too short", ImportErrors.UseColumnNames(["PhoneNumber: too short"], StudentTable.Schema, FileLanguage.Uk)[0]);
    }
}

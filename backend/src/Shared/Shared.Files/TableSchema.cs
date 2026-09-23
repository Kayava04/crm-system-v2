namespace Shared.Files;

public enum FileLanguage
{
    En,
    Uk
}

public enum ColumnType
{
    Text,
    Date,
    DateTime,
    Integer,
    Decimal,
    Boolean,
    Choice,
    MultiChoice
}

// One allowed value of a choice column: the value the system stores and how a person reads it in each language
public sealed record Choice(string Value, string En, string Uk)
{
    public string Display(FileLanguage language) => language == FileLanguage.Uk ? Uk : En;
}

// A column of a table a person can open in Excel. Key is the field name (same as in the JSON files).
// ForImport = false marks columns that only appear in exports (status, creation date...); an import ignores them.
public sealed record ColumnDef(
    string Key,
    string HeaderEn,
    string HeaderUk,
    ColumnType Type,
    bool Required = false,
    bool ForImport = true,
    IReadOnlyList<Choice>? Choices = null,
    string ExampleEn = "",
    string ExampleUk = "",
    string NoteEn = "",
    string NoteUk = "")
{
    public string Header(FileLanguage language) => language == FileLanguage.Uk ? HeaderUk : HeaderEn;

    public string Note(FileLanguage language) => language == FileLanguage.Uk ? NoteUk : NoteEn;

    public string Example(FileLanguage language) => language == FileLanguage.Uk ? ExampleUk : ExampleEn;
}

public sealed class TableSchema(
    string sheetNameEn,
    string sheetNameUk,
    IReadOnlyList<ColumnDef> columns)
{
    public IReadOnlyList<ColumnDef> Columns { get; } = columns;

    public string SheetName(FileLanguage language) => language == FileLanguage.Uk ? sheetNameUk : sheetNameEn;

    public string HelpSheetName(FileLanguage language) => language == FileLanguage.Uk ? "Довідка" : "Help";

    public ColumnDef? ByKey(string key) =>
        Columns.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
}

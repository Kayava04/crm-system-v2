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

public sealed record Choice(string Value, string En, string Uk)
{
    public string Display(FileLanguage language) => language == FileLanguage.Uk ? Uk : En;
}

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

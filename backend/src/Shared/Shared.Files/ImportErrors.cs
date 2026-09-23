namespace Shared.Files;

public static class ImportErrors
{
    // Validation reports "PhoneNumber: ..."; a person who filled in Excel knows that column as "Phone" or "Телефон"
    public static IReadOnlyList<string> UseColumnNames(IReadOnlyList<string> errors, TableSchema schema, FileLanguage language) =>
        errors.Select(e => UseColumnName(e, schema, language)).ToList();

    private static string UseColumnName(string error, TableSchema schema, FileLanguage language)
    {
        var colon = error.IndexOf(':');
        if (colon <= 0)
            return error;

        var name = error[..colon];
        var bracket = name.IndexOf('[');
        if (bracket >= 0)
            name = name[..bracket];

        var column = schema.ByKey(name);

        return column is null ? error : column.Header(language) + error[colon..];
    }
}

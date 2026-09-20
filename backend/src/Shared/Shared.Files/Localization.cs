using System.Text;

namespace Shared.Files;

internal static class Localization
{
    public static string T(FileLanguage language, string en, string uk) => language == FileLanguage.Uk ? uk : en;

    public static string Yes(FileLanguage language) => T(language, "Yes", "Так");

    public static string No(FileLanguage language) => T(language, "No", "Ні");

    // "First name", "first_name" and "firstName" are the same header; text in brackets is a hint and is ignored
    public static string NormalizeHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return string.Empty;

        var text = header;
        var open = text.IndexOf('(');
        if (open > 0)
            text = text[..open];

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            // apostrophes are dropped so "Ім'я" and "Імя" match
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    public static bool? ParseBoolean(string text) =>
        NormalizeHeader(text) switch
        {
            "yes" or "y" or "true" or "1" or "так" or "т" => true,
            "no" or "n" or "false" or "0" or "ні" or "н" => false,
            _ => null
        };
}

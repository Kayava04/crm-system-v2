using System.Security.Cryptography;

namespace Identity.Application.Services;

internal static class TemporaryPasswordGenerator
{
    public static string Generate()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";
        const string allChars = upper + lower + digits + special;

        var passwordChars = new List<char>
        {
            Pick(upper),
            Pick(lower),
            Pick(digits),
            Pick(special)
        };

        passwordChars.AddRange(Enumerable.Range(0, 8).Select(_ => Pick(allChars)));

        return new string(passwordChars
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .ToArray());
    }

    private static char Pick(string chars) => chars[RandomNumberGenerator.GetInt32(chars.Length)];
}

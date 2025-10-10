using System.Text.RegularExpressions;

namespace AzuEPI.Text;

public class RegexHelpers
{
    private static readonly Regex AlphanumericRegex = new Regex(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

    public static string TrimInvalidCharacters(string input)
    {
        return AlphanumericRegex.Replace(input, string.Empty);
    }
}
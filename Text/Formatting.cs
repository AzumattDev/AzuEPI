namespace AzuEPI.Text;

public static class Formatting
{
    private static readonly string[] wbSuffixes = { "", "k", "m", "b" };

    internal static string FormatNumberSimpleNoDecimal(float number)
    {
        CalculateShortNumberAndSuffix(number, out double shortNumber, out string suffix);
        return $"{shortNumber:N0}{suffix}";
    }

    private static void CalculateShortNumberAndSuffix(float number, out double shortNumber, out string suffix)
    {
        int mag = (int)(Math.Log10(number) / 3);
        mag = Math.Min(wbSuffixes.Length - 1, mag);
        double divisor = Math.Pow(10, mag * 3);

        shortNumber = number / divisor;
        suffix = wbSuffixes[mag];
    }
}
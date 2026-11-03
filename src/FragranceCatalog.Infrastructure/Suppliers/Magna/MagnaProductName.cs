using System.Globalization;
using System.Text.RegularExpressions;
using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

internal static partial class MagnaProductName
{
    public static Gender FindGender(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Gender.Unspecified;
        }

        if (WomenWord().IsMatch(text))
        {
            return Gender.Women;
        }

        if (MenWord().IsMatch(text))
        {
            return Gender.Men;
        }

        return UnisexWord().IsMatch(text) ? Gender.Unisex : Gender.Unspecified;
    }

    public static int? FindSizeMl(string name)
    {
        var match = Milliliters().Match(name);
        if (!match.Success || !int.TryParse(match.Groups["size"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var sizeMl))
        {
            return null;
        }

        return sizeMl > 0 ? sizeMl : null;
    }

    [GeneratedRegex(@"\b(dama|mujer)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WomenWord();

    [GeneratedRegex(@"\b(caballero|hombre)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MenWord();

    [GeneratedRegex(@"\bunisex\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnisexWord();

    [GeneratedRegex(@"(?<size>\d+)\s?ml(?![a-z])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Milliliters();
}

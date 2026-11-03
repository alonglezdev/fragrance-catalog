using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FragranceCatalog.Domain.Products;

/// <summary>
/// Human-readable identifier used in public URLs: lowercase ASCII letters and digits separated by single hyphens.
/// </summary>
public sealed partial record Slug
{
    private Slug(string value) => Value = value;

    /// <summary>The slug text, already normalized.</summary>
    public string Value { get; }

    /// <summary>
    /// Builds a slug from free text such as a brand or product name, dropping accents and symbols,
    /// so "Señorita Eau de Parfum 100ml" becomes "senorita-eau-de-parfum-100ml". Normalizing an existing slug returns it unchanged.
    /// </summary>
    /// <param name="text">Text with at least one letter or digit.</param>
    public static Slug FromText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var lowercaseAscii = RemoveAccents(text).ToLowerInvariant();
        var value = NonAlphanumericRun().Replace(lowercaseAscii, "-").Trim('-');
        if (value.Length == 0)
        {
            throw new ArgumentException($"'{text}' has no letters or digits to build a slug from.", nameof(text));
        }

        return new Slug(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    private static string RemoveAccents(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var baseCharacters = decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);
        return string.Concat(baseCharacters).Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();
}

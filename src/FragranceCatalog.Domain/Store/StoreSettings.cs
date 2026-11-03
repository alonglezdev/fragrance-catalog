using System.Text.RegularExpressions;

namespace FragranceCatalog.Domain.Store;

/// <summary>
/// Store identity and the WhatsApp contact customers use to buy, edited from the admin panel. There is exactly one row.
/// </summary>
public sealed partial class StoreSettings
{
    /// <summary>Identifier of the single settings row.</summary>
    public const int SingletonId = 1;

    /// <summary>Longest store name allowed.</summary>
    public const int MaxStoreNameLength = 80;

    /// <summary>Fewest digits a WhatsApp number may have, country code included.</summary>
    public const int MinWhatsAppNumberDigits = 10;

    /// <summary>Most digits a WhatsApp number may have, country code included.</summary>
    public const int MaxWhatsAppNumberDigits = 15;

    /// <summary>Longest WhatsApp message template allowed.</summary>
    public const int MaxMessageTemplateLength = 500;

    /// <summary>Store name used until the administrator sets the real one.</summary>
    public const string DefaultStoreName = "Dolce Aroma";

    /// <summary>Message customers send until the administrator changes it.</summary>
    public const string DefaultMessageTemplate = "Hola, me interesa el {product} ({price})";

    /// <summary>Placeholder replaced with the brand and product name.</summary>
    public const string ProductPlaceholder = "{product}";

    /// <summary>Placeholder replaced with the brand name.</summary>
    public const string BrandPlaceholder = "{brand}";

    /// <summary>Placeholder replaced with the bottle size, or nothing when unknown.</summary>
    public const string SizePlaceholder = "{size}";

    /// <summary>Placeholder replaced with the formatted sale price.</summary>
    public const string PricePlaceholder = "{price}";

    private static readonly char[] _phoneFormattingCharacters = [' ', '-', '(', ')', '+'];
    private static readonly HashSet<string> _supportedPlaceholders = [ProductPlaceholder, BrandPlaceholder, SizePlaceholder, PricePlaceholder];

    private StoreSettings(int id, string storeName, string whatsAppNumber, string whatsAppMessageTemplate)
    {
        Id = id;
        StoreName = storeName;
        WhatsAppNumber = whatsAppNumber;
        WhatsAppMessageTemplate = whatsAppMessageTemplate;
    }

    /// <summary>Always <see cref="SingletonId"/>.</summary>
    public int Id { get; private set; }

    /// <summary>Store name shown to customers.</summary>
    public string StoreName { get; private set; }

    /// <summary>Digits only, country code included, or empty when the store has not configured WhatsApp yet.</summary>
    public string WhatsAppNumber { get; private set; }

    /// <summary>Pre-written message with placeholders that opens in WhatsApp when a customer asks for a product.</summary>
    public string WhatsAppMessageTemplate { get; private set; }

    /// <summary>Whether customers can be sent to WhatsApp; without a number the buy button is hidden.</summary>
    public bool HasWhatsAppNumber => WhatsAppNumber.Length > 0;

    /// <summary>Settings for a new store: provisional name, no WhatsApp number and the default message.</summary>
    public static StoreSettings CreateDefault() =>
        new(SingletonId, DefaultStoreName, string.Empty, DefaultMessageTemplate);

    /// <summary>Strips the spaces, hyphens, parentheses and plus sign people type in phone numbers, so "+52 (33) 1234-5678" becomes "523312345678".</summary>
    /// <param name="whatsAppNumber">Number as typed by the administrator.</param>
    public static string NormalizeWhatsAppNumber(string whatsAppNumber)
    {
        ArgumentNullException.ThrowIfNull(whatsAppNumber);

        return string.Concat(whatsAppNumber.Where(character => !_phoneFormattingCharacters.Contains(character)));
    }

    /// <summary>Placeholders in the template that the message builder does not know, such as "{name}".</summary>
    /// <param name="template">Message template to inspect.</param>
    public static IReadOnlyList<string> FindUnknownPlaceholders(string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return Placeholder().Matches(template)
            .Select(match => match.Value)
            .Where(placeholder => !_supportedPlaceholders.Contains(placeholder))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Replaces every store setting at once, normalizing the phone number and rejecting invalid values.</summary>
    /// <param name="storeName">Store name, 1 to <see cref="MaxStoreNameLength"/> characters.</param>
    /// <param name="whatsAppNumber">Number as typed, or empty to disable WhatsApp; once normalized it must have 10 to 15 digits.</param>
    /// <param name="whatsAppMessageTemplate">Message, 1 to <see cref="MaxMessageTemplateLength"/> characters, using only supported placeholders.</param>
    public void Update(string storeName, string whatsAppNumber, string whatsAppMessageTemplate)
    {
        var trimmedStoreName = ValidateStoreName(storeName);
        var normalizedNumber = ValidateWhatsAppNumber(whatsAppNumber);
        ValidateMessageTemplate(whatsAppMessageTemplate);

        StoreName = trimmedStoreName;
        WhatsAppNumber = normalizedNumber;
        WhatsAppMessageTemplate = whatsAppMessageTemplate;
    }

    private static string ValidateStoreName(string storeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);

        var trimmed = storeName.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, MaxStoreNameLength, nameof(storeName));
        return trimmed;
    }

    private static string ValidateWhatsAppNumber(string whatsAppNumber)
    {
        var normalized = NormalizeWhatsAppNumber(whatsAppNumber);
        if (normalized.Length == 0)
        {
            return normalized;
        }

        var hasValidDigits = normalized.All(char.IsAsciiDigit)
            && normalized.Length is >= MinWhatsAppNumberDigits and <= MaxWhatsAppNumberDigits;
        if (!hasValidDigits)
        {
            throw new ArgumentException($"'{whatsAppNumber}' must have {MinWhatsAppNumberDigits} to {MaxWhatsAppNumberDigits} digits including the country code.", nameof(whatsAppNumber));
        }

        return normalized;
    }

    private static void ValidateMessageTemplate(string whatsAppMessageTemplate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(whatsAppMessageTemplate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(whatsAppMessageTemplate.Length, MaxMessageTemplateLength, nameof(whatsAppMessageTemplate));

        var unknownPlaceholders = FindUnknownPlaceholders(whatsAppMessageTemplate);
        if (unknownPlaceholders.Count > 0)
        {
            throw new ArgumentException($"Unknown placeholders: {string.Join(", ", unknownPlaceholders)}.", nameof(whatsAppMessageTemplate));
        }
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex Placeholder();
}

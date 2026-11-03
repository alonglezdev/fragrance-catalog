using System.Globalization;
using System.Text.RegularExpressions;
using FragranceCatalog.Domain.Common;

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
        Guard.ThrowIfInvalid(Validate(storeName, whatsAppNumber, whatsAppMessageTemplate));

        StoreName = storeName.Trim();
        WhatsAppNumber = NormalizeWhatsAppNumber(whatsAppNumber);
        WhatsAppMessageTemplate = whatsAppMessageTemplate;
    }

    /// <summary>Lists every problem of the proposed settings; an empty list means <see cref="Update"/> will accept them.</summary>
    /// <param name="storeName">Store name.</param>
    /// <param name="whatsAppNumber">Number as typed; it is normalized before it is checked, and empty is allowed.</param>
    /// <param name="whatsAppMessageTemplate">Message with placeholders.</param>
    public static IReadOnlyList<StoreSettingsError> Validate(string storeName, string whatsAppNumber, string whatsAppMessageTemplate) =>
    [
        .. ValidateStoreName(storeName),
        .. ValidateWhatsAppNumber(whatsAppNumber),
        .. ValidateMessageTemplate(whatsAppMessageTemplate),
    ];

    private static IEnumerable<StoreSettingsError> ValidateStoreName(string storeName)
    {
        if (string.IsNullOrWhiteSpace(storeName))
        {
            return [new StoreSettingsError(StoreSettingsErrorCode.StoreNameRequired, storeName ?? string.Empty)];
        }

        return storeName.Trim().Length > MaxStoreNameLength
            ? [new StoreSettingsError(StoreSettingsErrorCode.StoreNameTooLong, storeName)]
            : [];
    }

    private static IEnumerable<StoreSettingsError> ValidateWhatsAppNumber(string whatsAppNumber)
    {
        var normalized = NormalizeWhatsAppNumber(whatsAppNumber ?? string.Empty);
        if (normalized.Length == 0)
        {
            return [];
        }

        var hasValidDigits = normalized.All(char.IsAsciiDigit)
            && normalized.Length is >= MinWhatsAppNumberDigits and <= MaxWhatsAppNumberDigits;
        return hasValidDigits ? [] : [new StoreSettingsError(StoreSettingsErrorCode.InvalidWhatsAppNumber, whatsAppNumber!)];
    }

    private static IEnumerable<StoreSettingsError> ValidateMessageTemplate(string whatsAppMessageTemplate)
    {
        if (string.IsNullOrWhiteSpace(whatsAppMessageTemplate))
        {
            return [new StoreSettingsError(StoreSettingsErrorCode.MessageTemplateRequired, whatsAppMessageTemplate ?? string.Empty)];
        }

        var errors = FindUnknownPlaceholders(whatsAppMessageTemplate)
            .Select(placeholder => new StoreSettingsError(StoreSettingsErrorCode.UnknownPlaceholder, placeholder))
            .ToList();
        if (whatsAppMessageTemplate.Length > MaxMessageTemplateLength)
        {
            errors.Add(new StoreSettingsError(StoreSettingsErrorCode.MessageTemplateTooLong, whatsAppMessageTemplate.Length.ToString(CultureInfo.InvariantCulture)));
        }

        return errors;
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex Placeholder();
}

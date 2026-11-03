namespace FragranceCatalog.Application.Settings;

/// <summary>Store settings proposed by the administrator.</summary>
/// <param name="StoreName">Store name.</param>
/// <param name="WhatsAppNumber">Number as typed, or empty to hide the buy button.</param>
/// <param name="WhatsAppMessageTemplate">Message with placeholders.</param>
public sealed record StoreSettingsValues(string StoreName, string WhatsAppNumber, string WhatsAppMessageTemplate);

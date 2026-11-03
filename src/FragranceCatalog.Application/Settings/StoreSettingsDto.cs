using FragranceCatalog.Domain.Store;

namespace FragranceCatalog.Application.Settings;

/// <summary>Store settings as the admin panel shows them, including the template the public endpoint must not expose.</summary>
/// <param name="StoreName">Store name.</param>
/// <param name="WhatsAppNumber">Digits only, or empty when not configured.</param>
/// <param name="WhatsAppMessageTemplate">Message with placeholders.</param>
public sealed record StoreSettingsDto(string StoreName, string WhatsAppNumber, string WhatsAppMessageTemplate)
{
    /// <summary>Copies the settings entity.</summary>
    /// <param name="settings">The settings.</param>
    public static StoreSettingsDto From(StoreSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new StoreSettingsDto(settings.StoreName, settings.WhatsAppNumber, settings.WhatsAppMessageTemplate);
    }
}

namespace FragranceCatalog.Domain.Store;

/// <summary>Reasons the store settings cannot be saved.</summary>
public enum StoreSettingsErrorCode
{
    /// <summary>The store name is blank.</summary>
    StoreNameRequired,

    /// <summary>The store name is longer than allowed.</summary>
    StoreNameTooLong,

    /// <summary>The WhatsApp number is not 10 to 15 digits with country code.</summary>
    InvalidWhatsAppNumber,

    /// <summary>The WhatsApp message is blank.</summary>
    MessageTemplateRequired,

    /// <summary>The WhatsApp message is longer than allowed.</summary>
    MessageTemplateTooLong,

    /// <summary>The WhatsApp message uses a placeholder that does not exist.</summary>
    UnknownPlaceholder,
}

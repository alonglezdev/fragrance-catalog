using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Store;

namespace FragranceCatalog.Application.Settings;

/// <summary>Saves the store name and WhatsApp contact; the number is normalized by the domain.</summary>
/// <param name="store">Settings data.</param>
public sealed class UpdateStoreSettings(ISettingsStore store)
{
    /// <summary>Validates and saves the settings; invalid values change nothing and are all reported together.</summary>
    /// <param name="values">Proposed settings.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<StoreSettingsDto>> ExecuteAsync(StoreSettingsValues values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        var errors = StoreSettings.Validate(values.StoreName, values.WhatsAppNumber, values.WhatsAppMessageTemplate);
        if (errors.Count > 0)
        {
            return Result.Failure<StoreSettingsDto>(errors.Select(error => Error.Validation(error.Code, error.Value)));
        }

        var settings = await store.GetStoreSettingsAsync(cancellationToken);
        settings.Update(values.StoreName, values.WhatsAppNumber, values.WhatsAppMessageTemplate);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(StoreSettingsDto.From(settings));
    }
}

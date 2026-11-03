using FragranceCatalog.Application.Common;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Saves the store-wide pricing settings. Changing strategy keeps the ranges.</summary>
/// <param name="store">Pricing data.</param>
public sealed class UpdatePricingSettings(IPricingStore store)
{
    /// <summary>Validates and saves the settings; invalid values change nothing and are all reported together.</summary>
    /// <param name="values">Proposed settings.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<PricingSettingsDto>> ExecuteAsync(PricingSettingsValues values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);

        var errors = values.Validate();
        if (errors.Count > 0)
        {
            return Result.Failure<PricingSettingsDto>(PricingErrors.From(errors));
        }

        var settings = await store.GetSettingsForUpdateAsync(cancellationToken);
        values.ApplyTo(settings);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(PricingSettingsDto.From(settings));
    }
}

using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Pricing settings proposed by the administrator.</summary>
/// <param name="ActiveBaseStrategy">Strategy used when no rule applies.</param>
/// <param name="BasePercent">Global markup percentage.</param>
/// <param name="RoundingMode">Rounding applied to calculated prices.</param>
/// <param name="MinProfit">Low profit threshold in MXN.</param>
/// <param name="CostChangeAlertPercent">Cost alert threshold.</param>
public sealed record PricingSettingsValues(BaseStrategy ActiveBaseStrategy, decimal BasePercent, RoundingMode RoundingMode, decimal MinProfit, int CostChangeAlertPercent)
{
    /// <summary>Lists every problem of these values.</summary>
    public IReadOnlyList<PricingSettingsError> Validate() =>
        PricingSettings.Validate(ActiveBaseStrategy, BasePercent, RoundingMode, MinProfit, CostChangeAlertPercent);

    /// <summary>Writes these values on the settings entity.</summary>
    /// <param name="settings">Settings to change.</param>
    public void ApplyTo(PricingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Update(ActiveBaseStrategy, BasePercent, RoundingMode, MinProfit, CostChangeAlertPercent);
    }
}

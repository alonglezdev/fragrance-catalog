using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Pricing settings as the admin panel shows them.</summary>
/// <param name="ActiveBaseStrategy">Strategy used when no rule applies.</param>
/// <param name="BasePercent">Global markup percentage.</param>
/// <param name="RoundingMode">Rounding applied to calculated prices.</param>
/// <param name="MinProfit">Low profit threshold in MXN.</param>
/// <param name="CostChangeAlertPercent">Cost alert threshold.</param>
public sealed record PricingSettingsDto(BaseStrategy ActiveBaseStrategy, decimal BasePercent, RoundingMode RoundingMode, decimal MinProfit, int CostChangeAlertPercent)
{
    /// <summary>Copies the settings entity.</summary>
    /// <param name="settings">The settings.</param>
    public static PricingSettingsDto From(PricingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new PricingSettingsDto(settings.ActiveBaseStrategy, settings.BasePercent, settings.RoundingMode, settings.MinProfit, settings.CostChangeAlertPercent);
    }
}

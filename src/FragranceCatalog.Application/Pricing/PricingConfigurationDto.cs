namespace FragranceCatalog.Application.Pricing;

/// <summary>The whole pricing configuration as the admin panel shows it.</summary>
/// <param name="Settings">Store-wide settings.</param>
/// <param name="Ranges">Base price ranges, cheapest first.</param>
/// <param name="Rules">Brand and product exceptions, active or not.</param>
public sealed record PricingConfigurationDto(PricingSettingsDto Settings, IReadOnlyList<PriceRangeDto> Ranges, IReadOnlyList<PricingRuleDto> Rules);

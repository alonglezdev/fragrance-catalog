namespace FragranceCatalog.Domain.Pricing;

/// <summary>Everything the administrator configured to price products, loaded once and reused for every product.</summary>
/// <param name="Settings">Base strategy, rounding and alert thresholds.</param>
/// <param name="Ranges">Base price ranges, used when the base strategy is <see cref="BaseStrategy.Ranges"/>.</param>
/// <param name="Rules">Brand and product exceptions; inactive or out-of-window rules are ignored.</param>
public sealed record PricingConfiguration(
    PricingSettings Settings,
    IReadOnlyList<PriceRange> Ranges,
    IReadOnlyCollection<PricingRule> Rules);

namespace FragranceCatalog.Domain.Pricing;

/// <summary>A problem found in the pricing settings.</summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Value">The value that caused the problem, as text.</param>
public sealed record PricingSettingsError(PricingSettingsErrorCode Code, string Value);

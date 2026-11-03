namespace FragranceCatalog.Domain.Pricing;

/// <summary>A problem found in a pricing rule.</summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Value">The value that caused the problem, as text.</param>
public sealed record PricingRuleError(PricingRuleErrorCode Code, string Value);

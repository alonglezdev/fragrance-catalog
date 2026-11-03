namespace FragranceCatalog.Domain.Pricing;

/// <summary>Reasons a pricing rule cannot be saved.</summary>
public enum PricingRuleErrorCode
{
    /// <summary>The rule targets something other than a brand or a product.</summary>
    UnknownRuleScope,

    /// <summary>The rule does not say which brand or product it targets.</summary>
    RuleTargetRequired,

    /// <summary>The rule uses an action that does not exist.</summary>
    UnknownPriceAction,

    /// <summary>The rule adds a negative amount.</summary>
    NegativeAmount,

    /// <summary>The rule adds a percentage outside the allowed markup bounds.</summary>
    PercentOutOfRange,

    /// <summary>The rule sets a fixed price of zero or less.</summary>
    FixedPriceNotAboveZero,

    /// <summary>The rule ends at or before its own start.</summary>
    EndsNotAfterStart,
}

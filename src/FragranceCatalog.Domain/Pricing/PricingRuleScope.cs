namespace FragranceCatalog.Domain.Pricing;

/// <summary>What a pricing exception targets; product rules win over brand rules.</summary>
public enum PricingRuleScope
{
    /// <summary>Applies to every product of one brand.</summary>
    Brand,

    /// <summary>Applies to a single product.</summary>
    Product,
}

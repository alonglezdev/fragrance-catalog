namespace FragranceCatalog.Domain.Pricing;

/// <summary>Which part of the pricing configuration produced a sale price, so the admin can see why a product costs what it costs.</summary>
public enum AppliedRuleType
{
    /// <summary>A pricing rule targeting the product.</summary>
    ProductRule,

    /// <summary>A pricing rule targeting the product's brand.</summary>
    BrandRule,

    /// <summary>The base price range that contains the cost.</summary>
    BaseRange,

    /// <summary>The global base percentage.</summary>
    BasePercent,
}

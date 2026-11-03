namespace FragranceCatalog.Domain.Pricing;

/// <summary>What a price range or pricing rule does to the supplier cost to obtain the sale price.</summary>
public enum PriceAction
{
    /// <summary>Adds a fixed amount in MXN to the cost.</summary>
    AddAmount,

    /// <summary>Adds a percentage of the cost.</summary>
    AddPercent,

    /// <summary>Ignores the cost and sells at exactly the given amount, without rounding. Only pricing rules may use it.</summary>
    FixedPrice,
}

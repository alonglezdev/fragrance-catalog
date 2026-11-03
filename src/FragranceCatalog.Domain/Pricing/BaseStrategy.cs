namespace FragranceCatalog.Domain.Pricing;

/// <summary>How the sale price is built when no brand or product exception applies.</summary>
public enum BaseStrategy
{
    /// <summary>The markup depends on the cost range the product falls in.</summary>
    Ranges,

    /// <summary>The same percentage markup applies to every product.</summary>
    Percent,
}

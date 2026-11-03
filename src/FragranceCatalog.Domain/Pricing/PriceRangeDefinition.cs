namespace FragranceCatalog.Domain.Pricing;

/// <summary>The values of a price range before it exists, so a whole table can be checked without creating any range.</summary>
/// <param name="MinCost">Lowest cost covered, inclusive.</param>
/// <param name="MaxCost">First cost not covered, exclusive, or null for no limit.</param>
/// <param name="Action">Markup kind.</param>
/// <param name="Value">Amount in MXN or percentage added to the cost.</param>
public sealed record PriceRangeDefinition(decimal MinCost, decimal? MaxCost, PriceAction Action, decimal Value);

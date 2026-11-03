namespace FragranceCatalog.Domain.Pricing;

/// <summary>A problem found in a price range table.</summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Cost">The cost where the problem starts, so the admin panel can point at it; null when the table is empty.</param>
public sealed record PriceRangeError(PriceRangeErrorCode Code, decimal? Cost);

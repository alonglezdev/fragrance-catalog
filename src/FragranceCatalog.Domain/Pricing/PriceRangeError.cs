namespace FragranceCatalog.Domain.Pricing;

/// <summary>A problem found in a price range or in the table of ranges.</summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Value">The cost or markup that caused the problem, so the admin panel can point at it; null when the table is empty.</param>
/// <param name="Position">Zero-based position of the range in the table as it was submitted, so the panel can mark the row; null when the problem belongs to the whole table.</param>
public sealed record PriceRangeError(PriceRangeErrorCode Code, decimal? Value, int? Position = null);

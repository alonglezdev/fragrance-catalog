namespace FragranceCatalog.Domain.Pricing;

/// <summary>Result of pricing one product: what the customer pays and how much the store earns.</summary>
/// <param name="SalePrice">Final price for the customer, after the rule and rounding.</param>
/// <param name="Profit">Sale price minus supplier cost; may be negative.</param>
/// <param name="IsLowProfit">Profit is below the configured minimum; it only raises an alert and never blocks the sale.</param>
/// <param name="AppliedRule">Which rule produced the price.</param>
public sealed record PriceQuote(decimal SalePrice, decimal Profit, bool IsLowProfit, AppliedRule AppliedRule);

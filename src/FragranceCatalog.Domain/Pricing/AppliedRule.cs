namespace FragranceCatalog.Domain.Pricing;

/// <summary>Identifies the part of the pricing configuration that produced a sale price.</summary>
/// <param name="Type">Kind of rule that applied.</param>
/// <param name="Id">The pricing rule id for exceptions, the price range id for ranges, or null for the global percentage.</param>
public sealed record AppliedRule(AppliedRuleType Type, Guid? Id);

using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>A rule the administrator wants to create.</summary>
/// <param name="Scope">Whether it targets a brand or a product.</param>
/// <param name="TargetId">Targeted brand or product.</param>
/// <param name="Action">How the cost becomes a price.</param>
/// <param name="Value">Amount, percentage or fixed price.</param>
/// <param name="StartsAt">Optional start.</param>
/// <param name="EndsAt">Optional end.</param>
public sealed record NewPricingRule(PricingRuleScope Scope, Guid TargetId, PriceAction Action, decimal Value, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);

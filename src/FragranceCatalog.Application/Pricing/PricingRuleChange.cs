using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>New behavior for an existing rule; its target cannot change.</summary>
/// <param name="RuleId">Rule to change.</param>
/// <param name="Action">How the cost becomes a price.</param>
/// <param name="Value">Amount, percentage or fixed price.</param>
/// <param name="StartsAt">Optional start.</param>
/// <param name="EndsAt">Optional end.</param>
public sealed record PricingRuleChange(Guid RuleId, PriceAction Action, decimal Value, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);

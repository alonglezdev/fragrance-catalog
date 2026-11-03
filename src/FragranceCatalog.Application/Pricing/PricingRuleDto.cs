using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>A pricing exception as the admin panel shows it.</summary>
/// <param name="Id">Identifier of the rule.</param>
/// <param name="Scope">Whether it targets a brand or a product.</param>
/// <param name="TargetId">Targeted brand or product.</param>
/// <param name="Action">How the cost becomes a price.</param>
/// <param name="Value">Amount, percentage or fixed price.</param>
/// <param name="StartsAt">Optional start.</param>
/// <param name="EndsAt">Optional end.</param>
/// <param name="IsActive">Whether the administrator has it switched on.</param>
/// <param name="CreatedAt">Creation instant.</param>
public sealed record PricingRuleDto(
    Guid Id,
    PricingRuleScope Scope,
    Guid TargetId,
    PriceAction Action,
    decimal Value,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool IsActive,
    DateTimeOffset CreatedAt)
{
    /// <summary>Copies the rule entity.</summary>
    /// <param name="rule">The rule.</param>
    public static PricingRuleDto From(PricingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new PricingRuleDto(rule.Id, rule.Scope, rule.TargetId, rule.Action, rule.Value, rule.StartsAt, rule.EndsAt, rule.IsActive, rule.CreatedAt);
    }
}

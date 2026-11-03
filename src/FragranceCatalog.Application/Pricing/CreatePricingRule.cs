using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Creates a brand or product exception, active from the start. A target can only have one active rule.</summary>
/// <param name="store">Pricing data.</param>
/// <param name="timeProvider">Clock of the creation date.</param>
public sealed class CreatePricingRule(IPricingStore store, TimeProvider timeProvider)
{
    /// <summary>Validates and stores the rule; every problem is reported together.</summary>
    /// <param name="rule">The rule to create.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<PricingRuleDto>> ExecuteAsync(NewPricingRule rule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var errors = await NewPricingRuleChecks.FindErrorsAsync(store, rule, cancellationToken);
        if (errors.Count > 0)
        {
            return Result.Failure<PricingRuleDto>(errors);
        }

        var pricingRule = PricingRule.ForTarget(rule.Scope, rule.TargetId, rule.Action, rule.Value, rule.StartsAt, rule.EndsAt, timeProvider.GetUtcNow());
        return await store.TryAddRuleAsync(pricingRule, cancellationToken)
            ? Result.Success(PricingRuleDto.From(pricingRule))
            : Result.Failure<PricingRuleDto>([PricingErrors.DuplicateRule(rule.TargetId)]);
    }
}

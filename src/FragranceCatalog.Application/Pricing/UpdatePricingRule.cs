using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Changes what a rule does and when it applies. The target stays, so the rule keeps its place as the active rule of that target.</summary>
/// <param name="store">Pricing data.</param>
public sealed class UpdatePricingRule(IPricingStore store)
{
    /// <summary>Validates and saves the change; invalid values change nothing and are all reported together.</summary>
    /// <param name="change">The rule and its new behavior.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<PricingRuleDto>> ExecuteAsync(PricingRuleChange change, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);

        var errors = PricingRule.Validate(change.Action, change.Value, change.StartsAt, change.EndsAt);
        if (errors.Count > 0)
        {
            return Result.Failure<PricingRuleDto>(PricingErrors.From(errors));
        }

        var rule = await store.FindRuleForUpdateAsync(change.RuleId, cancellationToken);
        if (rule is null)
        {
            return Result.Failure<PricingRuleDto>([PricingErrors.RuleNotFound(change.RuleId)]);
        }

        rule.Update(change.Action, change.Value, change.StartsAt, change.EndsAt);
        return await store.TrySaveRuleChangesAsync(cancellationToken)
            ? Result.Success(PricingRuleDto.From(rule))
            : Result.Failure<PricingRuleDto>([PricingErrors.DuplicateRule(rule.TargetId)]);
    }
}

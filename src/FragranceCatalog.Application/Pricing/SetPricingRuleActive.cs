using FragranceCatalog.Application.Common;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Switches a rule on or off without deleting it. Switching on is refused if its target already has an active rule.</summary>
/// <param name="store">Pricing data.</param>
public sealed class SetPricingRuleActive(IPricingStore store)
{
    /// <summary>Changes whether the rule is active.</summary>
    /// <param name="ruleId">Rule to change.</param>
    /// <param name="isActive">Whether it should be active.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<PricingRuleDto>> ExecuteAsync(Guid ruleId, bool isActive, CancellationToken cancellationToken)
    {
        var rule = await store.FindRuleForUpdateAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            return Result.Failure<PricingRuleDto>([PricingErrors.RuleNotFound(ruleId)]);
        }

        if (isActive && await store.HasOtherActiveRuleAsync(rule.Scope, rule.TargetId, rule.Id, cancellationToken))
        {
            return Result.Failure<PricingRuleDto>([PricingErrors.DuplicateRule(rule.TargetId)]);
        }

        if (isActive)
        {
            rule.Activate();
        }
        else
        {
            rule.Deactivate();
        }

        return await store.TrySaveRuleChangesAsync(cancellationToken)
            ? Result.Success(PricingRuleDto.From(rule))
            : Result.Failure<PricingRuleDto>([PricingErrors.DuplicateRule(rule.TargetId)]);
    }
}

using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>What a new rule must pass, shared by saving it and previewing it so both reject the same rules.</summary>
internal static class NewPricingRuleChecks
{
    public static async Task<List<Error>> FindErrorsAsync(IPricingStore store, NewPricingRule rule, CancellationToken cancellationToken)
    {
        var errors = PricingErrors.From(PricingRule.Validate(rule.Scope, rule.TargetId, rule.Action, rule.Value, rule.StartsAt, rule.EndsAt)).ToList();
        if (rule.TargetId == Guid.Empty || !Enum.IsDefined(rule.Scope))
        {
            return errors;
        }

        if (!await store.TargetExistsAsync(rule.Scope, rule.TargetId, cancellationToken))
        {
            errors.Add(PricingErrors.TargetNotFound(rule.Scope, rule.TargetId));
        }
        else if (await store.HasOtherActiveRuleAsync(rule.Scope, rule.TargetId, null, cancellationToken))
        {
            errors.Add(PricingErrors.DuplicateRule(rule.TargetId));
        }

        return errors;
    }
}

using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>The pricing data the administrator changes. Entities it returns are saved by <see cref="SaveChangesAsync"/>.</summary>
public interface IPricingStore
{
    /// <summary>Loads the settings row to change it.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<PricingSettings> GetSettingsForUpdateAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the whole table of ranges in one transaction.</summary>
    /// <param name="ranges">The new table.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task ReplaceRangesAsync(IReadOnlyList<PriceRange> ranges, CancellationToken cancellationToken);

    /// <summary>Loads a rule to change it, or returns null if it does not exist.</summary>
    /// <param name="ruleId">Identifier of the rule.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<PricingRule?> FindRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a new rule. Returns false, storing nothing, if the database refuses it because its target already has an active rule:
    /// the last defense when two requests pass <see cref="HasOtherActiveRuleAsync"/> at the same time.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<bool> TryAddRuleAsync(PricingRule rule, CancellationToken cancellationToken);

    /// <summary>Saves the changes made to a loaded rule. Returns false, storing nothing, if the database refuses them because the target already has another active rule.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<bool> TrySaveRuleChangesAsync(CancellationToken cancellationToken);

    /// <summary>Whether an active rule other than the given one already targets the brand or product.</summary>
    /// <param name="scope">Whether the target is a brand or a product.</param>
    /// <param name="targetId">Targeted brand or product.</param>
    /// <param name="excludedRuleId">Rule to ignore, or null to consider all of them.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<bool> HasOtherActiveRuleAsync(PricingRuleScope scope, Guid targetId, Guid? excludedRuleId, CancellationToken cancellationToken);

    /// <summary>Whether the brand or product a rule wants to target exists.</summary>
    /// <param name="scope">Whether the target is a brand or a product.</param>
    /// <param name="targetId">Targeted brand or product.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<bool> TargetExistsAsync(PricingRuleScope scope, Guid targetId, CancellationToken cancellationToken);

    /// <summary>Saves the changes made to the loaded entities.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

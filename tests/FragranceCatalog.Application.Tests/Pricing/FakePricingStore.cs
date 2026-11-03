using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Tests.Pricing;

internal sealed class FakePricingStore : IPricingStore, IPricingReader
{
    public PricingSettings Settings { get; } = PricingSettings.CreateDefault();

    public List<PriceRange> Ranges { get; private set; } =
    [
        PriceRange.Create(0m, 1000m, PriceAction.AddAmount, 200m),
        PriceRange.Create(1000m, null, PriceAction.AddAmount, 300m),
    ];

    public List<PricingRule> Rules { get; } = [];

    public List<ProductCost> Products { get; } = [];

    public HashSet<Guid> ExistingTargets { get; } = [];

    public bool IsRefusingRuleSaves { get; set; }

    public int SaveCount { get; private set; }

    public Task<PricingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new PricingConfiguration(Settings, Ranges, Rules));

    public Task<IReadOnlyList<ProductCost>> GetProductCostsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProductCost>>(Products);

    public Task<PricingSettings> GetSettingsForUpdateAsync(CancellationToken cancellationToken) => Task.FromResult(Settings);

    public Task ReplaceRangesAsync(IReadOnlyList<PriceRange> ranges, CancellationToken cancellationToken)
    {
        Ranges = ranges.ToList();
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<PricingRule?> FindRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken) =>
        Task.FromResult(Rules.SingleOrDefault(rule => rule.Id == ruleId));

    public Task<bool> TryAddRuleAsync(PricingRule rule, CancellationToken cancellationToken)
    {
        if (!IsRefusingRuleSaves)
        {
            Rules.Add(rule);
            SaveCount++;
        }

        return Task.FromResult(!IsRefusingRuleSaves);
    }

    public Task<bool> TrySaveRuleChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount += IsRefusingRuleSaves ? 0 : 1;
        return Task.FromResult(!IsRefusingRuleSaves);
    }

    public Task<bool> HasOtherActiveRuleAsync(PricingRuleScope scope, Guid targetId, Guid? excludedRuleId, CancellationToken cancellationToken) =>
        Task.FromResult(Rules.Any(rule => rule.IsActive && rule.Id != excludedRuleId && rule.Scope == scope && rule.TargetId == targetId));

    public Task<bool> TargetExistsAsync(PricingRuleScope scope, Guid targetId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingTargets.Contains(targetId));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

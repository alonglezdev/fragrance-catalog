using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>A pricing change to try out. Any combination of parts can be proposed; what is left null stays as it is now.</summary>
/// <param name="Settings">Proposed settings.</param>
/// <param name="Ranges">Proposed complete table of ranges.</param>
/// <param name="NewRule">Proposed new rule.</param>
/// <param name="RuleChange">Proposed change to an existing rule.</param>
public sealed record PreviewPricingChangeRequest(
    PricingSettingsValues? Settings = null,
    IReadOnlyCollection<PriceRangeDefinition>? Ranges = null,
    NewPricingRule? NewRule = null,
    PricingRuleChange? RuleChange = null);

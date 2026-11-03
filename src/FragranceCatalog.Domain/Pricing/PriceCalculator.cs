namespace FragranceCatalog.Domain.Pricing;

/// <summary>
/// Calculates the sale price of a product from its supplier cost. Pure function: the same inputs always give the same quote,
/// which is why the sale price is never stored and the admin preview matches the real price.
/// </summary>
public static class PriceCalculator
{
    private const int PercentDecimals = 2;
    private const decimal PercentBase = 100m;

    /// <summary>
    /// Picks the applicable rule (product rule, else brand rule, else base strategy), applies it, rounds the result
    /// unless it is a fixed price, and reports the profit.
    /// </summary>
    /// <param name="cost">Supplier cost in MXN, zero or more.</param>
    /// <param name="brandId">Brand of the product.</param>
    /// <param name="productId">Product being priced.</param>
    /// <param name="configuration">Settings, base ranges and exceptions.</param>
    /// <param name="now">Instant used to decide which time-limited rules are in effect.</param>
    public static PriceQuote Calculate(decimal cost, Guid brandId, Guid productId, PricingConfiguration configuration, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cost);
        ArgumentNullException.ThrowIfNull(configuration);
        var settings = configuration.Settings;

        var rule = FindRuleInEffect(configuration.Rules, brandId, productId, now);
        var (calculatedPrice, appliedRule) = rule is null
            ? PriceWithBaseStrategy(cost, configuration)
            : PriceWithRule(cost, rule);
        var salePrice = rule?.Action == PriceAction.FixedPrice
            ? calculatedPrice
            : PriceRounding.Apply(calculatedPrice, settings.RoundingMode);

        var profit = salePrice - cost;
        return new PriceQuote(salePrice, profit, profit < settings.MinProfit, appliedRule);
    }

    private static PricingRule? FindRuleInEffect(IEnumerable<PricingRule> rules, Guid brandId, Guid productId, DateTimeOffset now)
    {
        var rulesInEffect = rules.Where(rule => rule.IsInEffectAt(now)).OrderByDescending(rule => rule.CreatedAt).ToList();
        return rulesInEffect.FirstOrDefault(rule => rule.TargetsProduct(productId))
            ?? rulesInEffect.FirstOrDefault(rule => rule.TargetsBrand(brandId));
    }

    private static (decimal Price, AppliedRule AppliedRule) PriceWithRule(decimal cost, PricingRule rule)
    {
        var type = rule.Scope == PricingRuleScope.Product ? AppliedRuleType.ProductRule : AppliedRuleType.BrandRule;
        return (ApplyAction(cost, rule.Action, rule.Value), new AppliedRule(type, rule.Id));
    }

    private static (decimal Price, AppliedRule AppliedRule) PriceWithBaseStrategy(decimal cost, PricingConfiguration configuration)
    {
        var settings = configuration.Settings;
        return settings.ActiveBaseStrategy switch
        {
            BaseStrategy.Ranges => PriceWithRange(cost, configuration.Ranges),
            BaseStrategy.Percent => (ApplyAction(cost, PriceAction.AddPercent, settings.BasePercent), new AppliedRule(AppliedRuleType.BasePercent, null)),
            _ => throw new InvalidOperationException($"Unknown base strategy {settings.ActiveBaseStrategy}."),
        };
    }

    private static (decimal Price, AppliedRule AppliedRule) PriceWithRange(decimal cost, IEnumerable<PriceRange> ranges)
    {
        var range = ranges.FirstOrDefault(candidate => candidate.Contains(cost))
            ?? throw new InvalidOperationException($"No price range covers a cost of {cost}.");
        return (ApplyAction(cost, range.Action, range.Value), new AppliedRule(AppliedRuleType.BaseRange, range.Id));
    }

    private static decimal ApplyAction(decimal cost, PriceAction action, decimal value) => action switch
    {
        PriceAction.AddAmount => cost + value,
        PriceAction.AddPercent => cost + Math.Round(cost * value / PercentBase, PercentDecimals, MidpointRounding.AwayFromZero),
        PriceAction.FixedPrice => value,
        _ => throw new InvalidOperationException($"Unknown price action {action}."),
    };
}

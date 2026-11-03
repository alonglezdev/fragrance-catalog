using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PriceCalculatorTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _brandId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly IReadOnlyList<PriceRange> _initialRanges =
    [
        PriceRange.Create(0m, 1000m, PriceAction.AddAmount, 200m),
        PriceRange.Create(1000m, null, PriceAction.AddAmount, 300m),
    ];

    [Fact]
    public void Calculate_CostInFirstInitialRange_AddsTwoHundred()
    {
        var quote = Calculate(775m, Settings());

        quote.SalePrice.ShouldBe(975m);
    }

    [Fact]
    public void Calculate_CostInSecondInitialRange_AddsThreeHundred()
    {
        var quote = Calculate(1089m, Settings());

        quote.SalePrice.ShouldBe(1389m);
    }

    [Fact]
    public void Calculate_CostEqualToRangeLowerBound_UsesThatRange()
    {
        var quote = Calculate(1000m, Settings());

        quote.SalePrice.ShouldBe(1300m);
        quote.AppliedRule.ShouldBe(new AppliedRule(AppliedRuleType.BaseRange, _initialRanges[1].Id));
    }

    [Fact]
    public void Calculate_CostJustBelowRangeUpperBound_UsesLowerRange()
    {
        var quote = Calculate(999.99m, Settings());

        quote.SalePrice.ShouldBe(1199.99m);
        quote.AppliedRule.ShouldBe(new AppliedRule(AppliedRuleType.BaseRange, _initialRanges[0].Id));
    }

    [Fact]
    public void Calculate_BasePercentTen_AddsTenPercentRoundedToCents()
    {
        var quote = Calculate(1089m, Settings(BaseStrategy.Percent));

        quote.SalePrice.ShouldBe(1197.90m);
    }

    [Fact]
    public void Calculate_BasePercent_ReportsBasePercentWithoutId()
    {
        var quote = Calculate(1089m, Settings(BaseStrategy.Percent));

        quote.AppliedRule.ShouldBe(new AppliedRule(AppliedRuleType.BasePercent, null));
    }

    [Fact]
    public void Calculate_BasePercentTenWithUpToNearestTen_RoundsUpToTwelveHundred()
    {
        var quote = Calculate(1089m, Settings(BaseStrategy.Percent, RoundingMode.UpToNearestTen));

        quote.SalePrice.ShouldBe(1200m);
    }

    [Fact]
    public void Calculate_InitialRangesWithUpToNearestTen_RoundsUpToMultipleOfTen()
    {
        var quote = Calculate(775m, Settings(rounding: RoundingMode.UpToNearestTen));

        quote.SalePrice.ShouldBe(980m);
    }

    [Fact]
    public void Calculate_InitialRangesWithEndsInNine_RoundsUpToPriceEndingInNine()
    {
        var quote = Calculate(775m, Settings(rounding: RoundingMode.EndsInNine));

        quote.SalePrice.ShouldBe(979m);
    }

    [Fact]
    public void Calculate_InitialRangesWithoutRounding_KeepsCalculatedPrice()
    {
        var quote = Calculate(1172m, Settings(rounding: RoundingMode.None));

        quote.SalePrice.ShouldBe(1472m);
    }

    [Fact]
    public void Calculate_ProductFixedPriceAndBrandRuleBothInEffect_ProductRuleWins()
    {
        var productRule = ProductRule(PriceAction.FixedPrice, 3299m);
        var brandRule = BrandRule(PriceAction.AddPercent, 8m);

        var quote = Calculate(2949m, Settings(), brandRule, productRule);

        quote.SalePrice.ShouldBe(3299m);
        quote.AppliedRule.ShouldBe(new AppliedRule(AppliedRuleType.ProductRule, productRule.Id));
    }

    [Fact]
    public void Calculate_BrandAddPercentEight_AddsEightPercentToCost()
    {
        var brandRule = BrandRule(PriceAction.AddPercent, 8m);

        var quote = Calculate(2500m, Settings(), brandRule);

        quote.SalePrice.ShouldBe(2700m);
        quote.AppliedRule.ShouldBe(new AppliedRule(AppliedRuleType.BrandRule, brandRule.Id));
    }

    [Fact]
    public void Calculate_BrandRuleAlreadyEnded_FallsBackToBaseRanges()
    {
        var expiredBrandRule = BrandRule(PriceAction.AddPercent, 8m, startsAt: _now.AddDays(-10), endsAt: _now.AddDays(-1));

        var quote = Calculate(2500m, Settings(), expiredBrandRule);

        quote.SalePrice.ShouldBe(2800m);
        quote.AppliedRule.Type.ShouldBe(AppliedRuleType.BaseRange);
    }

    [Fact]
    public void Calculate_BrandRuleStartingInFuture_FallsBackToBaseRanges()
    {
        var futureBrandRule = BrandRule(PriceAction.AddPercent, 8m, startsAt: _now.AddDays(1));

        var quote = Calculate(2500m, Settings(), futureBrandRule);

        quote.SalePrice.ShouldBe(2800m);
        quote.AppliedRule.Type.ShouldBe(AppliedRuleType.BaseRange);
    }

    [Fact]
    public void Calculate_InactiveProductRule_FallsBackToBrandRule()
    {
        var inactiveProductRule = ProductRule(PriceAction.FixedPrice, 3299m);
        inactiveProductRule.Deactivate();
        var brandRule = BrandRule(PriceAction.AddPercent, 8m);

        var quote = Calculate(2500m, Settings(), inactiveProductRule, brandRule);

        quote.SalePrice.ShouldBe(2700m);
        quote.AppliedRule.ShouldBe(new AppliedRule(AppliedRuleType.BrandRule, brandRule.Id));
    }

    [Fact]
    public void Calculate_InactiveBrandRule_FallsBackToBaseRanges()
    {
        var inactiveBrandRule = BrandRule(PriceAction.AddPercent, 8m);
        inactiveBrandRule.Deactivate();

        var quote = Calculate(2500m, Settings(), inactiveBrandRule);

        quote.SalePrice.ShouldBe(2800m);
    }

    [Fact]
    public void Calculate_BrandRuleForAnotherBrand_IsIgnored()
    {
        var otherBrandRule = PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddPercent, 8m, null, null, _now.AddDays(-30));

        var quote = Calculate(2500m, Settings(), otherBrandRule);

        quote.SalePrice.ShouldBe(2800m);
    }

    [Fact]
    public void Calculate_FixedPriceWithRoundingConfigured_IsNotRounded()
    {
        var productRule = ProductRule(PriceAction.FixedPrice, 3295m);

        var quote = Calculate(2949m, Settings(rounding: RoundingMode.UpToNearestTen), productRule);

        quote.SalePrice.ShouldBe(3295m);
    }

    [Fact]
    public void Calculate_RuleAddingAmountWithRoundingConfigured_IsRounded()
    {
        var brandRule = BrandRule(PriceAction.AddAmount, 150m);

        var quote = Calculate(2950m, Settings(rounding: RoundingMode.EndsInNine), brandRule);

        quote.SalePrice.ShouldBe(3109m);
    }

    [Fact]
    public void Calculate_FixedPriceBelowCost_ReportsNegativeProfitAsLowProfit()
    {
        var productRule = ProductRule(PriceAction.FixedPrice, 2800m);

        var quote = Calculate(2949m, Settings(), productRule);

        quote.Profit.ShouldBe(-149m);
        quote.IsLowProfit.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_ProfitBelowMinProfit_IsLowProfit()
    {
        var quote = Calculate(775m, Settings(minProfit: 200.01m));

        quote.Profit.ShouldBe(200m);
        quote.IsLowProfit.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_ProfitEqualToMinProfit_IsNotLowProfit()
    {
        var quote = Calculate(775m, Settings(minProfit: 200m));

        quote.IsLowProfit.ShouldBeFalse();
    }

    [Fact]
    public void Calculate_ProfitAfterRounding_UsesRoundedPrice()
    {
        var quote = Calculate(775m, Settings(rounding: RoundingMode.UpToNearestTen));

        quote.Profit.ShouldBe(205m);
    }

    [Fact]
    public void Calculate_NoRangeCoversCost_ThrowsInvalidOperationException()
    {
        var configuration = new PricingConfiguration(Settings(), [PriceRange.Create(0m, 1000m, PriceAction.AddAmount, 200m)], []);

        Should.Throw<InvalidOperationException>(() => PriceCalculator.Calculate(1500m, _brandId, _productId, configuration, _now));
    }

    [Fact]
    public void Calculate_NegativeCost_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Calculate(-1m, Settings()));
    }

    private static PricingSettings Settings(
        BaseStrategy strategy = BaseStrategy.Ranges,
        RoundingMode rounding = RoundingMode.None,
        decimal minProfit = PricingSettings.DefaultMinProfit)
    {
        var settings = PricingSettings.CreateDefault();
        settings.Update(strategy, PricingSettings.DefaultBasePercent, rounding, minProfit, PricingSettings.DefaultCostChangeAlertPercent);
        return settings;
    }

    private PriceQuote Calculate(decimal cost, PricingSettings settings, params PricingRule[] rules) =>
        PriceCalculator.Calculate(cost, _brandId, _productId, new PricingConfiguration(settings, _initialRanges, rules), _now);

    private PricingRule BrandRule(PriceAction action, decimal value, DateTimeOffset? startsAt = null, DateTimeOffset? endsAt = null) =>
        PricingRule.ForBrand(_brandId, action, value, startsAt, endsAt, _now.AddDays(-30));

    private PricingRule ProductRule(PriceAction action, decimal value) =>
        PricingRule.ForProduct(_productId, action, value, null, null, _now.AddDays(-30));
}

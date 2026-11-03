using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PricingRuleTests
{
    private static readonly DateTimeOffset _startsAt = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _endsAt = new(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _createdAt = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ForBrand_ValidValues_CreatesActiveBrandRule()
    {
        var brandId = Guid.NewGuid();

        var rule = PricingRule.ForBrand(brandId, PriceAction.AddPercent, 8m, null, null, _createdAt);

        rule.IsActive.ShouldBeTrue();
        rule.Scope.ShouldBe(PricingRuleScope.Brand);
        rule.TargetsBrand(brandId).ShouldBeTrue();
        rule.TargetsProduct(brandId).ShouldBeFalse();
    }

    [Theory]
    [InlineData(PriceAction.AddAmount)]
    [InlineData(PriceAction.AddPercent)]
    public void ForProduct_AddingZero_IsAllowed(PriceAction action)
    {
        var rule = PricingRule.ForProduct(Guid.NewGuid(), action, 0m, null, null, _createdAt);

        rule.Value.ShouldBe(0m);
    }

    [Fact]
    public void ForProduct_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PricingRule.ForProduct(Guid.NewGuid(), PriceAction.AddAmount, -1m, null, null, _createdAt));
    }

    [Fact]
    public void ForProduct_FixedPriceOfZero_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PricingRule.ForProduct(Guid.NewGuid(), PriceAction.FixedPrice, 0m, null, null, _createdAt));
    }

    [Fact]
    public void ForProduct_FixedPriceAboveZero_IsAllowed()
    {
        var rule = PricingRule.ForProduct(Guid.NewGuid(), PriceAction.FixedPrice, 0.01m, null, null, _createdAt);

        rule.Value.ShouldBe(0.01m);
    }

    [Fact]
    public void ForBrand_PercentAtMarkupMaximum_IsAllowed()
    {
        var rule = PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddPercent, MarkupPercent.Maximum, null, null, _createdAt);

        rule.Value.ShouldBe(500m);
    }

    [Fact]
    public void ForBrand_PercentAboveMarkupMaximum_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddPercent, 500.01m, null, null, _createdAt));
    }

    [Fact]
    public void ForBrand_AmountAboveMarkupMaximum_IsAllowed()
    {
        var rule = PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddAmount, 800m, null, null, _createdAt);

        rule.Value.ShouldBe(800m);
    }

    [Fact]
    public void Update_ToFixedPriceOfZero_ThrowsAndKeepsPreviousAction()
    {
        var rule = PricingRule.ForProduct(Guid.NewGuid(), PriceAction.AddAmount, 0m, null, null, _createdAt);

        Should.Throw<ArgumentOutOfRangeException>(() => rule.Update(PriceAction.FixedPrice, 0m, null, null));

        rule.Action.ShouldBe(PriceAction.AddAmount);
    }

    [Fact]
    public void ForBrand_EndsBeforeItStarts_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddPercent, 8m, _endsAt, _startsAt, _createdAt));
    }

    [Fact]
    public void ForBrand_EmptyBrandId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => PricingRule.ForBrand(Guid.Empty, PriceAction.AddPercent, 8m, null, null, _createdAt));
    }

    [Fact]
    public void IsInEffectAt_BeforeStart_ReturnsFalse()
    {
        var rule = WindowedRule();

        rule.IsInEffectAt(_startsAt.AddTicks(-1)).ShouldBeFalse();
    }

    [Fact]
    public void IsInEffectAt_ExactlyAtStart_ReturnsTrue()
    {
        var rule = WindowedRule();

        rule.IsInEffectAt(_startsAt).ShouldBeTrue();
    }

    [Fact]
    public void IsInEffectAt_ExactlyAtEnd_ReturnsFalse()
    {
        var rule = WindowedRule();

        rule.IsInEffectAt(_endsAt).ShouldBeFalse();
    }

    [Fact]
    public void IsInEffectAt_InsideWindowButDeactivated_ReturnsFalse()
    {
        var rule = WindowedRule();

        rule.Deactivate();

        rule.IsInEffectAt(_startsAt.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void IsInEffectAt_ReactivatedRule_ReturnsTrue()
    {
        var rule = WindowedRule();
        rule.Deactivate();

        rule.Activate();

        rule.IsInEffectAt(_startsAt.AddDays(1)).ShouldBeTrue();
    }

    [Fact]
    public void Update_NewActionAndWindow_ReplacesThem()
    {
        var rule = WindowedRule();

        rule.Update(PriceAction.FixedPrice, 3299m, null, null);

        rule.Action.ShouldBe(PriceAction.FixedPrice);
        rule.Value.ShouldBe(3299m);
        rule.StartsAt.ShouldBeNull();
        rule.EndsAt.ShouldBeNull();
    }

    [Fact]
    public void ForBrand_NonUtcWindow_StoresItAsUtc()
    {
        var localStart = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(-6));

        var rule = PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddPercent, 8m, localStart, null, _createdAt);

        rule.StartsAt.ShouldBe(localStart);
        rule.StartsAt!.Value.Offset.ShouldBe(TimeSpan.Zero);
    }

    private static PricingRule WindowedRule() =>
        PricingRule.ForBrand(Guid.NewGuid(), PriceAction.AddPercent, 8m, _startsAt, _endsAt, _createdAt);
}

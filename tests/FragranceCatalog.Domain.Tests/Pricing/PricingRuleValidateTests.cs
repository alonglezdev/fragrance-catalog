using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PricingRuleValidateTests
{
    private static readonly Guid _targetId = Guid.CreateVersion7();
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(PriceAction.AddAmount, 0)]
    [InlineData(PriceAction.AddPercent, 500)]
    [InlineData(PriceAction.FixedPrice, 1)]
    public void Validate_ValidValues_ReturnsNoErrors(PriceAction action, int value)
    {
        var errors = PricingRule.Validate(action, value, _start, _start.AddDays(1));

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_NoWindow_ReturnsNoErrors()
    {
        var errors = PricingRule.Validate(PriceAction.AddAmount, 100m, null, null);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_UnknownAction_ReturnsItsCode()
    {
        var errors = PricingRule.Validate((PriceAction)99, 100m, null, null);

        errors.ShouldBe([new PricingRuleError(PricingRuleErrorCode.UnknownPriceAction, "100")]);
    }

    [Fact]
    public void Validate_NegativeAmount_ReturnsItsCode()
    {
        var errors = PricingRule.Validate(PriceAction.AddAmount, -1m, null, null);

        errors.ShouldBe([new PricingRuleError(PricingRuleErrorCode.NegativeAmount, "-1")]);
    }

    [Fact]
    public void Validate_PercentAboveMaximum_ReturnsItsCode()
    {
        var errors = PricingRule.Validate(PriceAction.AddPercent, 501m, null, null);

        errors.ShouldBe([new PricingRuleError(PricingRuleErrorCode.PercentOutOfRange, "501")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_FixedPriceNotAboveZero_ReturnsItsCode(int value)
    {
        var errors = PricingRule.Validate(PriceAction.FixedPrice, value, null, null);

        errors.ShouldHaveSingleItem().Code.ShouldBe(PricingRuleErrorCode.FixedPriceNotAboveZero);
    }

    [Fact]
    public void Validate_EndsAtItsStart_ReturnsItsCode()
    {
        var errors = PricingRule.Validate(PriceAction.AddAmount, 100m, _start, _start);

        errors.ShouldHaveSingleItem().Code.ShouldBe(PricingRuleErrorCode.EndsNotAfterStart);
    }

    [Fact]
    public void Validate_UnknownScope_ReturnsItsCode()
    {
        var errors = PricingRule.Validate((PricingRuleScope)99, _targetId, PriceAction.AddAmount, 100m, null, null);

        errors.ShouldBe([new PricingRuleError(PricingRuleErrorCode.UnknownRuleScope, "99")]);
    }

    [Fact]
    public void Validate_EmptyTarget_ReturnsItsCode()
    {
        var errors = PricingRule.Validate(PricingRuleScope.Brand, Guid.Empty, PriceAction.AddAmount, 100m, null, null);

        errors.ShouldHaveSingleItem().Code.ShouldBe(PricingRuleErrorCode.RuleTargetRequired);
    }

    [Fact]
    public void Validate_SeveralProblems_ReturnsEveryError()
    {
        var errors = PricingRule.Validate((PricingRuleScope)99, Guid.Empty, PriceAction.AddPercent, 501m, _start, _start.AddDays(-1));

        errors.Select(error => error.Code).ShouldBe(
        [
            PricingRuleErrorCode.UnknownRuleScope,
            PricingRuleErrorCode.RuleTargetRequired,
            PricingRuleErrorCode.PercentOutOfRange,
            PricingRuleErrorCode.EndsNotAfterStart,
        ]);
    }

    [Fact]
    public void ForTarget_ProductScope_TargetsTheProduct()
    {
        var rule = PricingRule.ForTarget(PricingRuleScope.Product, _targetId, PriceAction.AddAmount, 100m, null, null, _start);

        rule.ProductId.ShouldBe(_targetId);
        rule.BrandId.ShouldBeNull();
        rule.TargetId.ShouldBe(_targetId);
        rule.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ForTarget_InvalidValues_ThrowsWithTheSameValidation()
    {
        var exception = Should.Throw<ArgumentException>(
            () => PricingRule.ForTarget(PricingRuleScope.Brand, Guid.Empty, PriceAction.AddAmount, 100m, null, null, _start));

        exception.Message.ShouldContain(nameof(PricingRuleErrorCode.RuleTargetRequired));
    }
}

using FragranceCatalog.Application.Common;
using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Application.Tests.Sync;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Tests.Pricing;

public sealed class PricingUseCaseTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly FakePricingStore _store = new();
    private readonly Guid _brandId = Guid.CreateVersion7();
    private readonly Guid _productId = Guid.CreateVersion7();

    public PricingUseCaseTests()
    {
        _store.ExistingTargets.Add(_brandId);
        _store.ExistingTargets.Add(_productId);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetPricingConfiguration_StoredConfiguration_ReturnsSettingsRangesAndRules()
    {
        _store.Rules.Add(BrandRule(100m));
        var useCase = new GetPricingConfiguration(_store);

        var configuration = await useCase.ExecuteAsync(Token);

        configuration.Settings.BasePercent.ShouldBe(PricingSettings.DefaultBasePercent);
        configuration.Ranges.Select(range => range.MinCost).ShouldBe([0m, 1000m]);
        configuration.Rules.ShouldHaveSingleItem().TargetId.ShouldBe(_brandId);
    }

    [Fact]
    public async Task UpdatePricingSettings_ValidValues_SavesThem()
    {
        var useCase = new UpdatePricingSettings(_store);

        var result = await useCase.ExecuteAsync(new PricingSettingsValues(BaseStrategy.Percent, 12m, RoundingMode.EndsInNine, 150m, 20), Token);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BasePercent.ShouldBe(12m);
        _store.Settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Percent);
        _store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdatePricingSettings_ChangesStrategy_KeepsRanges()
    {
        var useCase = new UpdatePricingSettings(_store);

        await useCase.ExecuteAsync(new PricingSettingsValues(BaseStrategy.Percent, 12m, RoundingMode.None, 100m, 15), Token);

        _store.Ranges.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UpdatePricingSettings_SeveralInvalidValues_ReturnsAllCodesAndTouchesNothing()
    {
        var useCase = new UpdatePricingSettings(_store);

        var result = await useCase.ExecuteAsync(new PricingSettingsValues(BaseStrategy.Percent, 501m, RoundingMode.None, -1m, 15), Token);

        result.Errors.Select(error => error.Code).ShouldBe(["BasePercentOutOfRange", "NegativeMinProfit"]);
        result.Errors[0].Value.ShouldBe("501");
        _store.Settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Ranges);
        _store.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReplacePriceRanges_ValidTable_ReplacesTheWholeTable()
    {
        var useCase = new ReplacePriceRanges(_store);
        PriceRangeDefinition[] table = [new(500m, null, PriceAction.AddPercent, 10m), new(0m, 500m, PriceAction.AddAmount, 100m)];

        var result = await useCase.ExecuteAsync(table, Token);

        result.Value.Select(range => range.MinCost).ShouldBe([0m, 500m]);
        _store.Ranges.Select(range => range.MinCost).ShouldBe([0m, 500m]);
    }

    [Fact]
    public async Task ReplacePriceRanges_GapAndBadRange_ReturnsBothAndKeepsCurrentTable()
    {
        var useCase = new ReplacePriceRanges(_store);
        var currentRanges = _store.Ranges;
        PriceRangeDefinition[] table = [new(0m, 500m, PriceAction.FixedPrice, 100m), new(600m, null, PriceAction.AddAmount, 100m)];

        var result = await useCase.ExecuteAsync(table, Token);

        result.Errors.Select(error => error.Code).ShouldBe(["FixedPriceNotAllowedInRange", "RangesHaveGap"]);
        _store.Ranges.ShouldBeSameAs(currentRanges);
    }

    [Fact]
    public async Task ReplacePriceRanges_BadRangeAndBadTable_CarriesPositionOnlyForTheRange()
    {
        var useCase = new ReplacePriceRanges(_store);
        PriceRangeDefinition[] table =
        [
            new(0m, 500m, PriceAction.AddAmount, 100m),
            new(600m, 900m, PriceAction.AddAmount, -1m),
            new(900m, null, PriceAction.AddPercent, 501m),
        ];

        var result = await useCase.ExecuteAsync(table, Token);

        result.Errors.Select(error => (error.Code, error.Value, error.Position)).ShouldBe(
        [
            ("NegativeAmount", "-1", 1),
            ("PercentOutOfRange", "501", 2),
            ("RangesHaveGap", "500", (int?)null),
        ]);
    }

    [Fact]
    public async Task CreatePricingRule_ValidBrandRule_StoresItActive()
    {
        var useCase = CreateRuleUseCase();

        var result = await useCase.ExecuteAsync(NewBrandRule(), Token);

        result.Value.IsActive.ShouldBeTrue();
        result.Value.CreatedAt.ShouldBe(_now);
        _store.Rules.ShouldHaveSingleItem().BrandId.ShouldBe(_brandId);
    }

    [Fact]
    public async Task CreatePricingRule_BrandAlreadyHasActiveRule_ReturnsDuplicateActiveRule()
    {
        _store.Rules.Add(BrandRule(100m));
        var useCase = CreateRuleUseCase();

        var result = await useCase.ExecuteAsync(NewBrandRule(), Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.DuplicateActiveRule);
        _store.Rules.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreatePricingRule_BrandOnlyHasInactiveRule_CreatesIt()
    {
        var inactiveRule = BrandRule(100m);
        inactiveRule.Deactivate();
        _store.Rules.Add(inactiveRule);
        var useCase = CreateRuleUseCase();

        var result = await useCase.ExecuteAsync(NewBrandRule(), Token);

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(PricingRuleScope.Brand, PricingErrors.BrandNotFound)]
    [InlineData(PricingRuleScope.Product, PricingErrors.ProductNotFound)]
    public async Task CreatePricingRule_TargetDoesNotExist_ReturnsNotFoundCode(PricingRuleScope scope, string expectedCode)
    {
        var useCase = CreateRuleUseCase();
        var rule = new NewPricingRule(scope, Guid.CreateVersion7(), PriceAction.AddAmount, 100m, null, null);

        var result = await useCase.ExecuteAsync(rule, Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task CreatePricingRule_BadValueAndMissingTarget_ReturnsBothErrors()
    {
        var useCase = CreateRuleUseCase();
        var rule = new NewPricingRule(PricingRuleScope.Brand, Guid.CreateVersion7(), PriceAction.AddPercent, 501m, null, null);

        var result = await useCase.ExecuteAsync(rule, Token);

        result.Errors.Select(error => error.Code).ShouldBe(["PercentOutOfRange", PricingErrors.BrandNotFound]);
    }

    [Fact]
    public async Task CreatePricingRule_DatabaseRefusesDuplicate_ReturnsDuplicateActiveRuleConflict()
    {
        _store.IsRefusingRuleSaves = true;

        var result = await CreateRuleUseCase().ExecuteAsync(NewBrandRule(), Token);

        ShouldBeDuplicateConflict(result);
    }

    [Fact]
    public async Task UpdatePricingRule_DatabaseRefusesDuplicate_ReturnsDuplicateActiveRuleConflict()
    {
        var rule = BrandRule(100m);
        _store.Rules.Add(rule);
        _store.IsRefusingRuleSaves = true;

        var result = await new UpdatePricingRule(_store).ExecuteAsync(new PricingRuleChange(rule.Id, PriceAction.AddAmount, 1m, null, null), Token);

        ShouldBeDuplicateConflict(result);
    }

    [Fact]
    public async Task SetPricingRuleActive_DatabaseRefusesDuplicate_ReturnsDuplicateActiveRuleConflict()
    {
        var rule = BrandRule(100m);
        rule.Deactivate();
        _store.Rules.Add(rule);
        _store.IsRefusingRuleSaves = true;

        var result = await new SetPricingRuleActive(_store).ExecuteAsync(rule.Id, true, Token);

        ShouldBeDuplicateConflict(result);
    }

    [Fact]
    public async Task UpdatePricingRule_ValidChange_SavesItAndKeepsTarget()
    {
        var rule = BrandRule(100m);
        _store.Rules.Add(rule);
        var useCase = new UpdatePricingRule(_store);

        var result = await useCase.ExecuteAsync(new PricingRuleChange(rule.Id, PriceAction.FixedPrice, 999m, null, null), Token);

        result.Value.Action.ShouldBe(PriceAction.FixedPrice);
        rule.Value.ShouldBe(999m);
        rule.BrandId.ShouldBe(_brandId);
        _store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdatePricingRule_RuleDoesNotExist_ReturnsPricingRuleNotFound()
    {
        var useCase = new UpdatePricingRule(_store);

        var result = await useCase.ExecuteAsync(new PricingRuleChange(Guid.CreateVersion7(), PriceAction.AddAmount, 1m, null, null), Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.PricingRuleNotFound);
    }

    [Fact]
    public async Task UpdatePricingRule_SeveralInvalidValues_ReturnsAllCodesAndTouchesNothing()
    {
        var rule = BrandRule(100m);
        _store.Rules.Add(rule);
        var useCase = new UpdatePricingRule(_store);

        var result = await useCase.ExecuteAsync(new PricingRuleChange(rule.Id, PriceAction.AddAmount, -1m, _now, _now), Token);

        result.Errors.Select(error => error.Code).ShouldBe(["NegativeAmount", "EndsNotAfterStart"]);
        rule.Value.ShouldBe(100m);
        _store.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task SetPricingRuleActive_Deactivate_SwitchesItOff()
    {
        var rule = BrandRule(100m);
        _store.Rules.Add(rule);
        var useCase = new SetPricingRuleActive(_store);

        var result = await useCase.ExecuteAsync(rule.Id, false, Token);

        result.Value.IsActive.ShouldBeFalse();
        rule.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SetPricingRuleActive_ActivateWithAnotherActiveRule_ReturnsDuplicateActiveRule()
    {
        var inactiveRule = BrandRule(100m);
        inactiveRule.Deactivate();
        _store.Rules.Add(inactiveRule);
        _store.Rules.Add(BrandRule(200m));
        var useCase = new SetPricingRuleActive(_store);

        var result = await useCase.ExecuteAsync(inactiveRule.Id, true, Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.DuplicateActiveRule);
        inactiveRule.IsActive.ShouldBeFalse();
        _store.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task SetPricingRuleActive_ActivateAlreadyActiveRule_DoesNotCountItselfAsDuplicate()
    {
        var rule = BrandRule(100m);
        _store.Rules.Add(rule);
        var useCase = new SetPricingRuleActive(_store);

        var result = await useCase.ExecuteAsync(rule.Id, true, Token);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SetPricingRuleActive_RuleDoesNotExist_ReturnsPricingRuleNotFound()
    {
        var useCase = new SetPricingRuleActive(_store);

        var result = await useCase.ExecuteAsync(Guid.CreateVersion7(), true, Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.PricingRuleNotFound);
    }

    private static void ShouldBeDuplicateConflict(Result<PricingRuleDto> result)
    {
        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe(PricingErrors.DuplicateActiveRule);
        error.Kind.ShouldBe(ErrorKind.Conflict);
    }

    private static NewPricingRule NewRuleFor(Guid brandId) => new(PricingRuleScope.Brand, brandId, PriceAction.AddPercent, 8m, null, null);

    private NewPricingRule NewBrandRule() => NewRuleFor(_brandId);

    private CreatePricingRule CreateRuleUseCase() => new(_store, new FixedTimeProvider(_now));

    private PricingRule BrandRule(decimal value) => PricingRule.ForBrand(_brandId, PriceAction.AddAmount, value, null, null, _now);
}

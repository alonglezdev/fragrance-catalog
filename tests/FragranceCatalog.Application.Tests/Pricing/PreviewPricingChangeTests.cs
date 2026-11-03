using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Application.Tests.Sync;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Tests.Pricing;

public sealed class PreviewPricingChangeTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly FakePricingStore _store = new();
    private readonly Guid _brandId = Guid.CreateVersion7();
    private readonly ProductCost _cheapProduct;
    private readonly ProductCost _expensiveProduct;

    public PreviewPricingChangeTests()
    {
        _cheapProduct = new ProductCost(Guid.CreateVersion7(), "Asad", _brandId, "Lattafa", 775m);
        _expensiveProduct = new ProductCost(Guid.CreateVersion7(), "Sauvage", Guid.CreateVersion7(), "Dior", 2500m);
        _store.Products.AddRange([_cheapProduct, _expensiveProduct]);
        _store.ExistingTargets.Add(_brandId);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExecuteAsync_ProposedSettings_ReturnsCurrentAndProposedPrices()
    {
        var request = new PreviewPricingChangeRequest(Settings: new PricingSettingsValues(BaseStrategy.Percent, 10m, RoundingMode.None, 100m, 15));

        var result = await ExecuteAsync(request);

        result.ProductCount.ShouldBe(2);
        result.ChangedCount.ShouldBe(2);
        var change = result.Changes.Single(item => item.ProductId == _cheapProduct.ProductId);
        change.CurrentPrice.ShouldBe(975m);
        change.ProposedPrice.ShouldBe(852.50m);
        change.ProposedProfit.ShouldBe(77.50m);
        change.IsLowProfit.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ProposedRanges_OnlyListsProductsThatChange()
    {
        PriceRangeDefinition[] ranges = [new(0m, 1000m, PriceAction.AddAmount, 250m), new(1000m, null, PriceAction.AddAmount, 300m)];

        var result = await ExecuteAsync(new PreviewPricingChangeRequest(Ranges: ranges));

        result.ProductCount.ShouldBe(2);
        result.ChangedCount.ShouldBe(1);
        result.Changes.ShouldHaveSingleItem().ProductId.ShouldBe(_cheapProduct.ProductId);
    }

    [Fact]
    public async Task ExecuteAsync_ProposedNewBrandRule_OnlyChangesThatBrand()
    {
        var rule = new NewPricingRule(PricingRuleScope.Brand, _brandId, PriceAction.FixedPrice, 1500m, null, null);

        var result = await ExecuteAsync(new PreviewPricingChangeRequest(NewRule: rule));

        result.Changes.ShouldHaveSingleItem().ProposedPrice.ShouldBe(1500m);
    }

    [Fact]
    public async Task ExecuteAsync_ProposedChangeToExistingRule_ReplacesItInsteadOfAddingOne()
    {
        var rule = PricingRule.ForBrand(_brandId, PriceAction.FixedPrice, 1000m, null, null, _now);
        _store.Rules.Add(rule);
        var change = new PricingRuleChange(rule.Id, PriceAction.FixedPrice, 1100m, null, null);

        var result = await ExecuteAsync(new PreviewPricingChangeRequest(RuleChange: change));

        var item = result.Changes.ShouldHaveSingleItem();
        item.CurrentPrice.ShouldBe(1000m);
        item.ProposedPrice.ShouldBe(1100m);
    }

    [Fact]
    public async Task ExecuteAsync_ChangeToInactiveRule_StaysInactiveAndChangesNothing()
    {
        var rule = PricingRule.ForBrand(_brandId, PriceAction.FixedPrice, 1000m, null, null, _now);
        rule.Deactivate();
        _store.Rules.Add(rule);
        var change = new PricingRuleChange(rule.Id, PriceAction.FixedPrice, 1100m, null, null);

        var result = await ExecuteAsync(new PreviewPricingChangeRequest(RuleChange: change));

        result.ChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_NothingProposed_ReportsNoChanges()
    {
        var result = await ExecuteAsync(new PreviewPricingChangeRequest());

        result.ProductCount.ShouldBe(2);
        result.ChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_AnyProposal_SavesNothingAndKeepsLoadedConfiguration()
    {
        var rule = PricingRule.ForBrand(_brandId, PriceAction.FixedPrice, 1000m, null, null, _now);
        _store.Rules.Add(rule);
        var currentRanges = _store.Ranges;
        var request = new PreviewPricingChangeRequest(
            new PricingSettingsValues(BaseStrategy.Percent, 50m, RoundingMode.EndsInNine, 1m, 1),
            [new PriceRangeDefinition(0m, null, PriceAction.AddAmount, 1m)],
            RuleChange: new PricingRuleChange(rule.Id, PriceAction.AddAmount, 5m, null, null));

        await ExecuteAsync(request);

        _store.SaveCount.ShouldBe(0);
        _store.Settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Ranges);
        _store.Settings.BasePercent.ShouldBe(PricingSettings.DefaultBasePercent);
        _store.Ranges.ShouldBeSameAs(currentRanges);
        rule.Action.ShouldBe(PriceAction.FixedPrice);
        rule.Value.ShouldBe(1000m);
    }

    [Fact]
    public async Task ExecuteAsync_ProposalLaterSaved_MatchesTheRealPrice()
    {
        var values = new PricingSettingsValues(BaseStrategy.Percent, 10m, RoundingMode.UpToNearestTen, 100m, 15);
        var preview = await ExecuteAsync(new PreviewPricingChangeRequest(Settings: values));

        await new UpdatePricingSettings(_store).ExecuteAsync(values, Token);

        var configuration = new PricingConfiguration(_store.Settings, _store.Ranges, _store.Rules);
        foreach (var item in preview.Changes)
        {
            var product = _store.Products.Single(candidate => candidate.ProductId == item.ProductId);
            PriceCalculator.Calculate(product.Cost, product.BrandId, product.ProductId, configuration, _now).SalePrice.ShouldBe(item.ProposedPrice);
        }
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSettingsRangesAndRule_ReturnsEveryError()
    {
        var useCase = CreateUseCase();
        var request = new PreviewPricingChangeRequest(
            new PricingSettingsValues(BaseStrategy.Percent, 501m, RoundingMode.None, 100m, 15),
            [],
            new NewPricingRule(PricingRuleScope.Brand, _brandId, PriceAction.AddAmount, -1m, null, null),
            new PricingRuleChange(Guid.CreateVersion7(), PriceAction.AddAmount, 1m, null, null));

        var result = await useCase.ExecuteAsync(request, Token);

        result.Errors.Select(error => error.Code).ShouldBe(["BasePercentOutOfRange", "NoRanges", PricingErrors.PricingRuleNotFound, "NegativeAmount"]);
    }

    [Fact]
    public async Task ExecuteAsync_NewRuleForTargetWithActiveRule_ReturnsDuplicateActiveRule()
    {
        _store.Rules.Add(PricingRule.ForBrand(_brandId, PriceAction.AddAmount, 1m, null, null, _now));
        var request = new PreviewPricingChangeRequest(NewRule: new NewPricingRule(PricingRuleScope.Brand, _brandId, PriceAction.AddAmount, 2m, null, null));

        var result = await CreateUseCase().ExecuteAsync(request, Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.DuplicateActiveRule);
    }

    private async Task<PricingPreviewDto> ExecuteAsync(PreviewPricingChangeRequest request)
    {
        var result = await CreateUseCase().ExecuteAsync(request, Token);
        return result.Value;
    }

    private PreviewPricingChange CreateUseCase() => new(_store, _store, new FixedTimeProvider(_now));
}

using FragranceCatalog.Application.Alerts;
using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Application.Tests.Pricing;
using FragranceCatalog.Application.Tests.Sync;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Tests.Alerts;

public sealed class GetPricingAlertsTests : IPricingAlertStore
{
    private static readonly DateTimeOffset _now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly FakePricingStore _pricing = new();
    private readonly List<CostChange> _costChanges = [];

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public Task<IReadOnlyList<CostChange>> GetCostChangesOfLastSyncAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CostChange>>(_costChanges);

    [Fact]
    public async Task ExecuteAsync_ProfitBelowMinimum_RaisesLowProfitAlert()
    {
        var lowProfit = new ProductCost(Guid.CreateVersion7(), "Asad", Guid.CreateVersion7(), "Lattafa", 775m);
        _pricing.Products.AddRange([lowProfit, new ProductCost(Guid.CreateVersion7(), "Sauvage", Guid.CreateVersion7(), "Dior", 2500m)]);
        _pricing.Rules.Add(PricingRule.ForProduct(lowProfit.ProductId, PriceAction.FixedPrice, 800m, null, null, _now));

        var alerts = await ExecuteAsync();

        var alert = alerts.LowProfit.ShouldHaveSingleItem();
        alert.ProductId.ShouldBe(lowProfit.ProductId);
        alert.SalePrice.ShouldBe(800m);
        alert.Profit.ShouldBe(25m);
    }

    [Fact]
    public async Task ExecuteAsync_ProfitExactlyAtMinimum_RaisesNoAlert()
    {
        var product = new ProductCost(Guid.CreateVersion7(), "Asad", Guid.CreateVersion7(), "Lattafa", 700m);
        _pricing.Products.Add(product);
        _pricing.Rules.Add(PricingRule.ForProduct(product.ProductId, PriceAction.FixedPrice, 800m, null, null, _now));

        var alerts = await ExecuteAsync();

        alerts.LowProfit.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1000, 1160, 16)]
    [InlineData(1000, 840, -16)]
    public async Task ExecuteAsync_CostMovedMoreThanPercent_RaisesAlertWithSignedChange(int previousCost, int currentCost, int expectedPercent)
    {
        _costChanges.Add(ChangeOf(previousCost, currentCost));

        var alerts = await ExecuteAsync();

        alerts.CostChanges.ShouldHaveSingleItem().ChangePercent.ShouldBe(expectedPercent);
    }

    [Theory]
    [InlineData(1000, 1150)]
    [InlineData(1000, 850)]
    [InlineData(1000, 1000)]
    public async Task ExecuteAsync_CostMovedUpToPercent_RaisesNoAlert(int previousCost, int currentCost)
    {
        _costChanges.Add(ChangeOf(previousCost, currentCost));

        var alerts = await ExecuteAsync();

        alerts.CostChanges.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ThresholdChanged_UsesConfiguredPercent()
    {
        _pricing.Settings.Update(BaseStrategy.Ranges, 10m, RoundingMode.None, 100m, 5);
        _costChanges.Add(ChangeOf(1000m, 1060m));

        var alerts = await ExecuteAsync();

        alerts.CostChanges.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ExecuteAsync_FractionalChange_ComparesExactValueAndRoundsForDisplay()
    {
        _costChanges.Add(ChangeOf(3000m, 3450.30m));

        var alerts = await ExecuteAsync();

        alerts.CostChanges.ShouldHaveSingleItem().ChangePercent.ShouldBe(15.01m);
    }

    private static CostChange ChangeOf(decimal previousCost, decimal currentCost) =>
        new(Guid.CreateVersion7(), "Asad", "Lattafa", previousCost, currentCost);

    private Task<PricingAlertsDto> ExecuteAsync() =>
        new GetPricingAlerts(_pricing, this, new FixedTimeProvider(_now)).ExecuteAsync(Token);
}

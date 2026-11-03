using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Alerts;
using FragranceCatalog.Infrastructure.Persistence;
using FragranceCatalog.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using static FragranceCatalog.Infrastructure.Tests.Persistence.CatalogTestData;

namespace FragranceCatalog.Infrastructure.Tests.Alerts;

public sealed class PricingAlertStoreTests(PostgresDatabaseFixture database)
{
    private static readonly DateTimeOffset _syncStart = new(2090, 1, 1, 4, 0, 0, TimeSpan.Zero);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetCostChangesOfLastSyncAsync_PriceRecordedInLastRun_ReturnsPreviousAndCurrentCost()
    {
        await using var context = database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(Token);
        var brand = NewBrand();
        var product = NewProduct(brand, regularPrice: 1000m, capturedAt: _syncStart.AddDays(-1));
        product.RecordSupplierPrice(1300m, 1200m, _syncStart.AddMinutes(5));
        context.Brands.Add(brand);
        context.Products.Add(product);
        context.SyncRuns.Add(SucceededRun(_syncStart));
        await context.SaveChangesAsync(Token);

        var changes = await new PricingAlertStore(context).GetCostChangesOfLastSyncAsync(Token);

        var change = changes.ShouldHaveSingleItem();
        change.ProductId.ShouldBe(product.Id);
        change.PreviousCost.ShouldBe(1000m);
        change.CurrentCost.ShouldBe(1200m);
        change.BrandName.ShouldBe(brand.Name);
    }

    [Fact]
    public async Task GetCostChangesOfLastSyncAsync_ChangeOnlyInAnEarlierRun_ReturnsNothing()
    {
        await using var context = database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(Token);
        var brand = NewBrand();
        var product = NewProduct(brand, capturedAt: _syncStart.AddDays(-2));
        product.RecordSupplierPrice(1300m, null, _syncStart.AddDays(-1));
        context.Brands.Add(brand);
        context.Products.Add(product);
        context.SyncRuns.AddRange(SucceededRun(_syncStart.AddDays(-1).AddMinutes(-1)), SucceededRun(_syncStart));
        await context.SaveChangesAsync(Token);

        var changes = await new PricingAlertStore(context).GetCostChangesOfLastSyncAsync(Token);

        changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCostChangesOfLastSyncAsync_LaterRunFailed_UsesLastSuccessfulRun()
    {
        await using var context = database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(Token);
        var brand = NewBrand();
        var product = NewProduct(brand, capturedAt: _syncStart.AddDays(-1));
        product.RecordSupplierPrice(1300m, null, _syncStart.AddMinutes(5));
        context.Brands.Add(brand);
        context.Products.Add(product);
        var failedRun = SyncRun.Start(SyncTrigger.Scheduled, _syncStart.AddDays(1));
        failedRun.Fail(_syncStart.AddDays(1).AddHours(1));
        context.SyncRuns.AddRange(SucceededRun(_syncStart), failedRun);
        await context.SaveChangesAsync(Token);

        var changes = await new PricingAlertStore(context).GetCostChangesOfLastSyncAsync(Token);

        changes.ShouldHaveSingleItem().ProductId.ShouldBe(product.Id);
    }

    [Fact]
    public async Task GetCostChangesOfLastSyncAsync_ProductCreatedInLastRun_HasNoPreviousCostToCompare()
    {
        await using var context = database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(Token);
        var brand = NewBrand();
        context.Brands.Add(brand);
        context.Products.Add(NewProduct(brand, capturedAt: _syncStart.AddMinutes(5)));
        context.SyncRuns.Add(SucceededRun(_syncStart));
        await context.SaveChangesAsync(Token);

        var changes = await new PricingAlertStore(context).GetCostChangesOfLastSyncAsync(Token);

        changes.ShouldBeEmpty();
    }

    private static SyncRun SucceededRun(DateTimeOffset startedAt)
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, startedAt);
        run.Complete(new SyncChangeCounts(0, 0, 0, 0), startedAt.AddHours(1));
        return run;
    }
}

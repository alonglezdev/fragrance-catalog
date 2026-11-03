using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Store;
using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Tests.Persistence;

public sealed class MigrationTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migrate_EmptyDatabase_LeavesNoPendingMigrations()
    {
        await using var context = database.CreateDbContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(Token);
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(Token);

        pendingMigrations.ShouldBeEmpty();
        appliedMigrations.ShouldContain(migration => migration.EndsWith("_InitialCreate"));
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsMagnaSupplier()
    {
        await using var context = database.CreateDbContext();

        var magna = await context.Suppliers.SingleAsync(supplier => supplier.Id == SeedData.MagnaSupplierId, Token);

        magna.Name.ShouldBe("Magna Perfumes");
        magna.BaseUrl.ShouldBe("https://magnaperfumes.com");
        magna.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsDefaultPricingSettings()
    {
        await using var context = database.CreateDbContext();

        var settings = await context.PricingSettings.SingleAsync(Token);

        settings.Id.ShouldBe(PricingSettings.SingletonId);
        settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Ranges);
        settings.BasePercent.ShouldBe(10m);
        settings.RoundingMode.ShouldBe(RoundingMode.None);
        settings.MinProfit.ShouldBe(100m);
        settings.CostChangeAlertPercent.ShouldBe(15);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsInitialPriceRangesThatPassValidation()
    {
        await using var context = database.CreateDbContext();

        var ranges = await context.PriceRanges.OrderBy(range => range.MinCost).ToListAsync(Token);

        ranges.Select(range => (range.MinCost, range.MaxCost, range.Action, range.Value)).ShouldBe(
        [
            (0m, 1000m, PriceAction.AddAmount, 200m),
            (1000m, null, PriceAction.AddAmount, 300m),
        ]);
        PriceRangeValidator.Validate(ranges).ShouldBeEmpty();
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsDefaultSyncSettings()
    {
        await using var context = database.CreateDbContext();

        var settings = await context.SyncSettings.SingleAsync(Token);

        settings.IsDailySyncEnabled.ShouldBeTrue();
        settings.DailySyncTime.ShouldBe(new TimeOnly(4, 0));
        settings.TimeZoneId.ShouldBe("America/Mexico_City");
        settings.FailureThresholdPercent.ShouldBe(20);
        settings.DelayBetweenRequestsMs.ShouldBe(1000);
        settings.RequestTimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsDefaultStoreSettings()
    {
        await using var context = database.CreateDbContext();

        var settings = await context.StoreSettings.SingleAsync(Token);

        settings.StoreName.ShouldBe("Dolce Aroma");
        settings.WhatsAppNumber.ShouldBeEmpty();
        settings.WhatsAppMessageTemplate.ShouldBe("Hola, me interesa el {product} ({price})");
    }

    [Theory]
    [InlineData(typeof(Product), nameof(Product.CurrentSupplierPrice))]
    [InlineData(typeof(SupplierPrice), nameof(SupplierPrice.Cost))]
    [InlineData(typeof(PriceRange), nameof(PriceRange.IsOpenEnded))]
    [InlineData(typeof(StoreSettings), nameof(StoreSettings.HasWhatsAppNumber))]
    [InlineData(typeof(SyncRun), nameof(SyncRun.IsFinished))]
    public void Model_CalculatedProperty_IsNotMappedToColumnOrNavigation(Type entityType, string propertyName)
    {
        using var context = database.CreateDbContext();

        var entity = context.Model.FindEntityType(entityType)!;

        entity.FindProperty(propertyName).ShouldBeNull();
        entity.FindNavigation(propertyName).ShouldBeNull();
    }
}

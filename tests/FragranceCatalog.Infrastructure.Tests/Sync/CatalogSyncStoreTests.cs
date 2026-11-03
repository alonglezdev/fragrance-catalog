using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using FragranceCatalog.Infrastructure.Sync;
using FragranceCatalog.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Tests.Sync;

public sealed class CatalogSyncStoreTests(PostgresDatabaseFixture database)
{
    private const string MainImageUrl = "https://cdn.example.com/products/1/main.jpg";
    private const string SideImageUrl = "https://cdn.example.com/products/1/side.jpg";

    private static readonly DateTimeOffset _previousSyncAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _seenAt = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly SyncChangeCounts _noChanges = new(0, 0, 0, 0);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetSettingsAsync_MigratedDatabase_ReturnsTheSettingsRow()
    {
        await using var context = database.CreateDbContext();

        var settings = await new CatalogSyncStore(context).GetSettingsAsync(Token);

        settings.Id.ShouldBe(SyncSettings.SingletonId);
    }

    [Fact]
    public async Task SaveStartedRunAsync_NewRun_StoresItAsRunning()
    {
        var run = SyncRun.Start(SyncTrigger.Manual, _previousSyncAt);
        await using var context = database.CreateDbContext();

        await new CatalogSyncStore(context).SaveStartedRunAsync(run, Token);

        var stored = await LoadRunAsync(run.Id);
        stored.Status.ShouldBe(SyncStatus.Running);
        stored.FinishedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetRunningRunsAsync_RunningAndFinishedRuns_ReturnsOnlyRunning()
    {
        var running = await SaveStartedRunAsync();
        var finished = await SaveStartedRunAsync();
        finished.Fail(_seenAt);
        await SaveFailedRunAsync(finished);
        await using var context = database.CreateDbContext();

        var runs = await new CatalogSyncStore(context).GetRunningRunsAsync(Token);

        runs.Select(run => run.Id).ShouldContain(running.Id);
        runs.Select(run => run.Id).ShouldNotContain(finished.Id);
    }

    [Fact]
    public async Task SaveFailedRunAsync_RunInterruptedByRestart_UpdatesItsRowFromAnotherContext()
    {
        var interrupted = await SaveStartedRunAsync();
        await using var context = database.CreateDbContext();
        var store = new CatalogSyncStore(context);
        var loaded = (await store.GetRunningRunsAsync(Token)).Single(run => run.Id == interrupted.Id);
        loaded.RecordError("sync", "The sync was interrupted before it finished.");
        loaded.Fail(_seenAt);

        await store.SaveFailedRunAsync(loaded, Token);

        var stored = await LoadRunAsync(interrupted.Id);
        stored.Status.ShouldBe(SyncStatus.Failed);
        stored.FinishedAt.ShouldBe(_seenAt);
        stored.ErrorCount.ShouldBe(1);
        stored.Errors.ShouldHaveSingleItem().Url.ShouldBe("sync");
    }

    [Fact]
    public async Task SaveFailedRunAsync_RunWithPageErrors_UpdatesTheSameRow()
    {
        await using var context = database.CreateDbContext();
        var store = new CatalogSyncStore(context);
        var run = SyncRun.Start(SyncTrigger.Scheduled, _previousSyncAt);
        await store.SaveStartedRunAsync(run, Token);
        run.RecordError("https://magnaperfumes.com/a/", "Timeout");
        run.RecordError("https://magnaperfumes.com/b/", "Missing price");
        run.Fail(_seenAt);

        await store.SaveFailedRunAsync(run, CancellationToken.None);

        var stored = await LoadRunAsync(run.Id);
        stored.Status.ShouldBe(SyncStatus.Failed);
        stored.ErrorCount.ShouldBe(2);
        stored.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCatalogSnapshotAsync_ProductWithPriceHistory_ReturnsCurrentPrice()
    {
        var brand = await SaveBrandAsync();
        var stored = await SaveProductAsync(brand, product => product.RecordSupplierPrice(1500m, 1400m, _seenAt));
        await using var context = database.CreateDbContext();

        var snapshot = await new CatalogSyncStore(context).GetCatalogSnapshotAsync(Token);

        var existing = snapshot.Products.Single(product => product.Id == stored.Id);
        snapshot.SupplierId.ShouldBe(SeedData.MagnaSupplierId);
        existing.SupplierProductId.ShouldBe(stored.SupplierProductId);
        existing.SourceUrl.ShouldBe(stored.SourceUrl);
        existing.RegularPrice.ShouldBe(1500m);
        existing.SalePrice.ShouldBe(1400m);
        snapshot.Brands.ShouldContain(candidate => candidate.Id == brand.Id);
        snapshot.TakenProductSlugs.ShouldContain(stored.Slug);
    }

    [Fact]
    public async Task ApplyPlanAsync_FullPlan_StoresEveryChangeAndTheRun()
    {
        var brand = await SaveBrandAsync();
        var updated = await SaveProductAsync(brand, product =>
        {
            product.MarkAsDiscontinued();
            product.Hide();
            product.Feature();
        });
        var discontinued = await SaveProductAsync(brand);
        var newBrand = NewBrand();
        var newProduct = NewProduct(newBrand);
        var plan = PlanOf(
            newBrands: [newBrand],
            newProducts: [newProduct],
            updates: [UpdateOf(updated, regularPrice: 1200m, images: [SideImageUrl], isReactivated: true)],
            discontinuedIds: [discontinued.Id]);
        await using var context = database.CreateDbContext();
        var store = new CatalogSyncStore(context);
        var run = SyncRun.Start(SyncTrigger.Manual, _previousSyncAt);
        await store.SaveStartedRunAsync(run, Token);
        run.RecordError("https://magnaperfumes.com/unreadable/", "Timeout");
        run.Complete(new SyncChangeCounts(1, 1, 1, 1), _seenAt);

        await store.ApplyPlanAsync(plan, run, Token);

        await using var verification = database.CreateDbContext();
        var storedUpdate = await LoadProductAsync(verification, updated.Id);
        storedUpdate.IsDiscontinued.ShouldBeFalse();
        storedUpdate.IsHidden.ShouldBeTrue();
        storedUpdate.IsFeatured.ShouldBeTrue();
        storedUpdate.LastSeenAt.ShouldBe(_seenAt);
        storedUpdate.SupplierPrices.Count.ShouldBe(2);
        storedUpdate.CurrentSupplierPrice.RegularPrice.ShouldBe(1200m);
        storedUpdate.Images.ShouldHaveSingleItem().Url.ShouldBe(SideImageUrl);
        (await LoadProductAsync(verification, discontinued.Id)).IsDiscontinued.ShouldBeTrue();
        (await LoadProductAsync(verification, newProduct.Id)).BrandId.ShouldBe(newBrand.Id);
        var storedRun = await LoadRunAsync(run.Id);
        storedRun.Status.ShouldBe(SyncStatus.Succeeded);
        storedRun.CreatedCount.ShouldBe(1);
        storedRun.Errors.ShouldHaveSingleItem().Url.ShouldBe("https://magnaperfumes.com/unreadable/");
    }

    [Fact]
    public async Task ApplyPlanAsync_SamePriceAndImages_AddsNoPriceRowAndKeepsImageRows()
    {
        var brand = await SaveBrandAsync();
        var stored = await SaveProductAsync(brand);
        var originalImageId = stored.Images.Single().Id;
        var plan = PlanOf(updates: [UpdateOf(stored, regularPrice: 1000m, images: [MainImageUrl])]);

        var run = await SaveStartedRunAsync();
        run.Complete(_noChanges, _seenAt);

        await using (var context = database.CreateDbContext())
        {
            await new CatalogSyncStore(context).ApplyPlanAsync(plan, run, Token);
        }

        await using var verification = database.CreateDbContext();
        var reloaded = await LoadProductAsync(verification, stored.Id);
        reloaded.SupplierPrices.Count.ShouldBe(1);
        reloaded.Images.ShouldHaveSingleItem().Id.ShouldBe(originalImageId);
    }

    [Fact]
    public async Task ApplyPlanAsync_FailureHalfway_LeavesNoChangeBehind()
    {
        var failedPlan = await CreatePlanThatViolatesSlugUniquenessAsync();
        var run = await SaveStartedRunAsync();
        run.Complete(_noChanges, _seenAt);
        await using var context = database.CreateDbContext();

        await Should.ThrowAsync<DbUpdateException>(() => new CatalogSyncStore(context).ApplyPlanAsync(failedPlan.Plan, run, Token));

        await using var verification = database.CreateDbContext();
        var updated = await LoadProductAsync(verification, failedPlan.UpdatedProductId);
        updated.Name.ShouldBe(failedPlan.OriginalName);
        updated.SupplierPrices.Count.ShouldBe(1);
        (await LoadProductAsync(verification, failedPlan.DiscontinuedProductId)).IsDiscontinued.ShouldBeFalse();
        (await verification.Brands.AnyAsync(brand => brand.Id == failedPlan.NewBrandId, Token)).ShouldBeFalse();
        (await verification.Products.AnyAsync(product => product.Id == failedPlan.ValidNewProductId, Token)).ShouldBeFalse();
        (await LoadRunAsync(run.Id)).Status.ShouldBe(SyncStatus.Running);
    }

    [Fact]
    public async Task SaveFailedRunAsync_AfterFailedApplyOnSameStore_UpdatesSameRowAndNothingElse()
    {
        var failedPlan = await CreatePlanThatViolatesSlugUniquenessAsync();
        await using var context = database.CreateDbContext();
        var store = new CatalogSyncStore(context);
        var run = SyncRun.Start(SyncTrigger.Scheduled, _previousSyncAt);
        await store.SaveStartedRunAsync(run, Token);
        run.RecordError("https://magnaperfumes.com/unreadable/", "Timeout");
        run.Complete(_noChanges, _seenAt);
        await Should.ThrowAsync<DbUpdateException>(() => store.ApplyPlanAsync(failedPlan.Plan, run, Token));
        run.ReopenAfterFailedSave();
        run.RecordError("sync", "The database rejected the changes.");
        run.Fail(_seenAt);

        await store.SaveFailedRunAsync(run, Token);

        await using var verification = database.CreateDbContext();
        var storedRun = await LoadRunAsync(run.Id);
        storedRun.Status.ShouldBe(SyncStatus.Failed);
        storedRun.ErrorCount.ShouldBe(2);
        storedRun.Errors.Count.ShouldBe(2);
        storedRun.Errors.ShouldContain(error => error.Message == "The database rejected the changes.");
        (await LoadProductAsync(verification, failedPlan.UpdatedProductId)).Name.ShouldBe(failedPlan.OriginalName);
        (await verification.Brands.AnyAsync(brand => brand.Id == failedPlan.NewBrandId, Token)).ShouldBeFalse();
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static Brand NewBrand()
    {
        var name = Unique("sync-brand");
        return Brand.Create(name, Slug.FromText(name));
    }

    private static Product NewProduct(Brand brand, Slug? slug = null)
    {
        var name = Unique("sync-product");
        var product = Product.Create(
            SeedData.MagnaSupplierId,
            Unique("supplier-id"),
            slug ?? Slug.FromText(name),
            DetailsOf(name, brand.Id),
            1000m,
            null,
            _previousSyncAt);
        product.ReplaceImages([MainImageUrl]);
        return product;
    }

    private static SupplierProductDetails DetailsOf(string name, Guid brandId) =>
        new($"https://magnaperfumes.com/{name}/", name, brandId, Gender.Men, 100, "Fragancia", true);

    private static ProductUpdate UpdateOf(Product product, decimal regularPrice, IReadOnlyList<string> images, bool isReactivated = false, string? name = null) =>
        new(product.Id, DetailsOf(name ?? product.Name, product.BrandId), regularPrice, null, images, isReactivated);

    private static CatalogSyncPlan PlanOf(
        IReadOnlyList<Brand>? newBrands = null,
        IReadOnlyList<Product>? newProducts = null,
        IReadOnlyList<ProductUpdate>? updates = null,
        IReadOnlyList<Guid>? discontinuedIds = null) =>
        new(_seenAt, newBrands ?? [], newProducts ?? [], updates ?? [], discontinuedIds ?? [], [], _noChanges);

    private static Task<Product> LoadProductAsync(FragranceCatalogDbContext context, Guid productId) =>
        context.Products
            .Include(product => product.Images)
            .Include(product => product.SupplierPrices)
            .AsSplitQuery()
            .SingleAsync(product => product.Id == productId, Token);

    private async Task<SyncRun> LoadRunAsync(Guid runId)
    {
        await using var context = database.CreateDbContext();
        return await context.SyncRuns.AsNoTracking().Include(run => run.Errors).SingleAsync(run => run.Id == runId, Token);
    }

    private async Task<SyncRun> SaveStartedRunAsync()
    {
        var run = SyncRun.Start(SyncTrigger.Manual, _previousSyncAt);
        await using var context = database.CreateDbContext();
        await new CatalogSyncStore(context).SaveStartedRunAsync(run, Token);
        return run;
    }

    private async Task SaveFailedRunAsync(SyncRun run)
    {
        await using var context = database.CreateDbContext();
        await new CatalogSyncStore(context).SaveFailedRunAsync(run, Token);
    }

    private async Task<FailedPlan> CreatePlanThatViolatesSlugUniquenessAsync()
    {
        var brand = await SaveBrandAsync();
        var updated = await SaveProductAsync(brand);
        var discontinued = await SaveProductAsync(brand);
        var newBrand = NewBrand();
        var validNewProduct = NewProduct(newBrand);
        var plan = PlanOf(
            newBrands: [newBrand],
            newProducts: [validNewProduct, NewProduct(newBrand, updated.Slug)],
            updates: [UpdateOf(updated, regularPrice: 1300m, images: [MainImageUrl], name: Unique("renamed"))],
            discontinuedIds: [discontinued.Id]);
        return new FailedPlan(plan, updated.Id, updated.Name, discontinued.Id, newBrand.Id, validNewProduct.Id);
    }

    private async Task<Brand> SaveBrandAsync()
    {
        var brand = NewBrand();
        await using var context = database.CreateDbContext();
        context.Brands.Add(brand);
        await context.SaveChangesAsync(Token);
        return brand;
    }

    private async Task<Product> SaveProductAsync(Brand brand, Action<Product>? prepare = null)
    {
        var product = NewProduct(brand);
        prepare?.Invoke(product);
        await using var context = database.CreateDbContext();
        context.Products.Add(product);
        await context.SaveChangesAsync(Token);
        return product;
    }

    private sealed record FailedPlan(
        CatalogSyncPlan Plan,
        Guid UpdatedProductId,
        string OriginalName,
        Guid DiscontinuedProductId,
        Guid NewBrandId,
        Guid ValidNewProductId);
}

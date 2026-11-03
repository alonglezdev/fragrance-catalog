using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Sync;
using static FragranceCatalog.Application.Tests.Sync.SyncTestData;

namespace FragranceCatalog.Application.Tests.Sync;

public sealed class SyncSupplierCatalogTests
{
    private const string InvalidBrand = "لطافة";

    private readonly FakeCatalogSyncStore _store = new();
    private readonly FakeSupplierCatalogSource _source = new();
    private readonly Brand _brand = ExistingBrand();

    public SyncSupplierCatalogTests() => _store.Brands.Add(_brand);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExecuteAsync_NewProductAndBrand_CreatesBothAndCountsProduct()
    {
        _source.Publish(SupplierProductOf("1", brand: "Señorita Parfums", salePrice: 900m));

        var run = await ExecuteAsync();

        var product = _store.Products.ShouldHaveSingleItem();
        var brand = _store.Brands.Single(candidate => candidate.Name == "Señorita Parfums");
        product.BrandId.ShouldBe(brand.Id);
        brand.Slug.Value.ShouldBe("senorita-parfums");
        product.SupplierId.ShouldBe(FakeCatalogSyncStore.SupplierId);
        product.CurrentSupplierPrice.Cost.ShouldBe(900m);
        product.CreatedAt.ShouldBe(Now);
        product.Images.ShouldHaveSingleItem().Url.ShouldBe(ImageUrl);
        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.CreatedCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_PriceChanged_AddsPriceRowAndCountsIt()
    {
        var product = AddExistingProduct("1", regularPrice: 1000m);
        _source.Publish(SupplierProductOf("1", regularPrice: 1100m));

        var run = await ExecuteAsync();

        product.SupplierPrices.Count.ShouldBe(2);
        product.CurrentSupplierPrice.RegularPrice.ShouldBe(1100m);
        product.CurrentSupplierPrice.CapturedAt.ShouldBe(Now);
        run.PriceChangedCount.ShouldBe(1);
        run.CreatedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_SamePrice_AddsNoPriceRowButRefreshesData()
    {
        var product = AddExistingProduct("1", regularPrice: 1000m);
        _source.Publish(SupplierProductOf("1", regularPrice: 1000m, name: "Asad Renamed EDP 100ml Caballero"));

        var run = await ExecuteAsync();

        product.SupplierPrices.Count.ShouldBe(1);
        product.Name.ShouldBe("Asad Renamed EDP 100ml Caballero");
        product.LastSeenAt.ShouldBe(Now);
        run.PriceChangedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_RenamedProduct_KeepsPublicSlug()
    {
        var product = AddExistingProduct("1");
        var originalSlug = product.Slug;
        _source.Publish(SupplierProductOf("1", name: "Asad Renamed EDP 100ml Caballero"));

        await ExecuteAsync();

        product.Slug.ShouldBe(originalSlug);
    }

    [Fact]
    public async Task ExecuteAsync_ProductLeftSitemap_DiscontinuesItWithoutDeleting()
    {
        var product = AddExistingProduct("1");
        _source.Publish(SupplierProductOf("2"));

        var run = await ExecuteAsync();

        product.IsDiscontinued.ShouldBeTrue();
        _store.Products.ShouldContain(product);
        run.DiscontinuedCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_DiscontinuedProductReturns_ReactivatesAndCountsIt()
    {
        var product = AddExistingProduct("1");
        product.MarkAsDiscontinued();
        _source.Publish(SupplierProductOf("1"));

        var run = await ExecuteAsync();

        product.IsDiscontinued.ShouldBeFalse();
        run.ReactivatedCount.ShouldBe(1);
        run.DiscontinuedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_PageOfExistingProductFails_RecordsErrorAndDoesNotDiscontinueIt()
    {
        var product = AddExistingProduct("1");
        _source.PublishUnreadablePage(UrlOf("1"));
        _source.Publish(Enumerable.Range(2, 9).Select(number => SupplierProductOf(number.ToString())).ToArray());

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.ErrorCount.ShouldBe(1);
        run.Errors.ShouldHaveSingleItem().Url.ShouldBe(UrlOf("1"));
        product.IsDiscontinued.ShouldBeFalse();
        run.DiscontinuedCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_FailuresAboveThreshold_FailsRunAndChangesNothing()
    {
        var product = AddExistingProduct("catalog", regularPrice: 1000m);
        PublishProducts(readable: 79, unreadable: 21);

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Failed);
        run.ErrorCount.ShouldBe(21);
        run.FinishedAt.ShouldBe(Now);
        _store.AppliedPlanCount.ShouldBe(0);
        _store.Products.ShouldBe([product]);
        product.IsDiscontinued.ShouldBeFalse();
        _store.Runs.ShouldBe([run]);
    }

    [Fact]
    public async Task ExecuteAsync_FailuresBelowThreshold_AppliesChanges()
    {
        PublishProducts(readable: 81, unreadable: 19);

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.ErrorCount.ShouldBe(19);
        run.CreatedCount.ShouldBe(81);
        _store.AppliedPlanCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_FailuresExactlyAtThreshold_AppliesChanges()
    {
        PublishProducts(readable: 80, unreadable: 20);

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_ThresholdRaisedToFifty_AppliesWithFortyPercentFailures()
    {
        SetFailureThreshold(50);
        PublishProducts(readable: 60, unreadable: 40);

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.CreatedCount.ShouldBe(60);
    }

    [Fact]
    public async Task ExecuteAsync_HiddenAndFeaturedProduct_StaysHiddenAndFeatured()
    {
        var product = AddExistingProduct("1");
        product.Hide();
        product.Feature();
        _source.Publish(SupplierProductOf("1", regularPrice: 1200m));

        await ExecuteAsync();

        product.IsHidden.ShouldBeTrue();
        product.IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_HiddenProductDiscontinuedAndReactivated_StaysHidden()
    {
        var product = AddExistingProduct("1");
        product.Hide();
        _source.Publish(SupplierProductOf("2"));
        await ExecuteAsync();
        var reappearedSource = new FakeSupplierCatalogSource().Publish(SupplierProductOf("1"), SupplierProductOf("2"));

        await new SyncSupplierCatalog(_store, reappearedSource, new FixedTimeProvider(Now)).ExecuteAsync(SyncTrigger.Scheduled, Token);

        product.IsDiscontinued.ShouldBeFalse();
        product.IsHidden.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SitemapFails_FailsRunWithItsErrorAndChangesNothing()
    {
        var product = AddExistingProduct("1");
        _source.SitemapFailure = new SupplierCatalogException("Could not download the sitemap.");

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Failed);
        run.Errors.ShouldHaveSingleItem().Message.ShouldBe("Could not download the sitemap.");
        product.IsDiscontinued.ShouldBeFalse();
        _store.AppliedPlanCount.ShouldBe(0);
        _store.Runs.ShouldBe([run]);
    }

    [Fact]
    public async Task ExecuteAsync_SitemapListsNoProducts_FailsRunInsteadOfDiscontinuingEverything()
    {
        var product = AddExistingProduct("1");

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Failed);
        run.ErrorCount.ShouldBe(1);
        product.IsDiscontinued.ShouldBeFalse();
        _store.AppliedPlanCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_SyncCancelled_MarksRunFailedAndRethrows()
    {
        using var cancellation = new CancellationTokenSource();
        _source.CancelOnProductRead = cancellation;
        _source.Publish(SupplierProductOf("1"));

        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateUseCase().ExecuteAsync(SyncTrigger.Manual, cancellation.Token));

        var run = _store.Runs.ShouldHaveSingleItem();
        run.Status.ShouldBe(SyncStatus.Failed);
        _store.PersistedStatusOf(run).ShouldBe(SyncStatus.Failed);
        var error = run.Errors.ShouldHaveSingleItem();
        error.Url.ShouldBe("sync");
        error.Message.ShouldContain("cancelled");
        _store.AppliedPlanCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_SyncCancelledDuringApply_MarksSameRunFailed()
    {
        using var cancellation = new CancellationTokenSource();
        _store.CancelOnApply = cancellation;
        _source.Publish(SupplierProductOf("1"));

        await Should.ThrowAsync<OperationCanceledException>(
            () => CreateUseCase().ExecuteAsync(SyncTrigger.Manual, cancellation.Token));

        var run = _store.Runs.ShouldHaveSingleItem();
        _store.PersistedStatusOf(run).ShouldBe(SyncStatus.Failed);
        run.CreatedCount.ShouldBe(0);
        run.Errors.ShouldHaveSingleItem().Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_CancelledWhenSitemapFailure_StillSavesFailedRun()
    {
        using var cancellation = new CancellationTokenSource();
        _source.SitemapFailure = new SupplierCatalogException("Could not download the sitemap.");
        cancellation.Cancel();
        _store.AllowCancelledStartOfRun = true;

        var run = await CreateUseCase().ExecuteAsync(SyncTrigger.Manual, cancellation.Token);

        _store.PersistedStatusOf(run).ShouldBe(SyncStatus.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_RunningSync_IsVisibleAsRunningWhileReadingSupplier()
    {
        SyncStatus? statusWhileReading = null;
        _source.OnProductRead = () => statusWhileReading = _store.PersistedStatusOf(_store.Runs.Single());
        _source.Publish(SupplierProductOf("1"));

        var run = await ExecuteAsync();

        statusWhileReading.ShouldBe(SyncStatus.Running);
        _store.PersistedStatusOf(run).ShouldBe(SyncStatus.Succeeded);
        _store.Runs.ShouldBe([run]);
    }

    [Fact]
    public async Task ExecuteAsync_EarlierRunStillRunning_MarksItFailedAsInterrupted()
    {
        var interruptedRun = _store.AddRunningRun(SyncTestData.PreviousSyncAt);
        _source.Publish(SupplierProductOf("1"));

        var run = await ExecuteAsync();

        interruptedRun.Status.ShouldBe(SyncStatus.Failed);
        _store.PersistedStatusOf(interruptedRun).ShouldBe(SyncStatus.Failed);
        interruptedRun.FinishedAt.ShouldBe(Now);
        var error = interruptedRun.Errors.ShouldHaveSingleItem();
        error.Url.ShouldBe("sync");
        error.Message.ShouldContain("interrupted");
        run.Status.ShouldBe(SyncStatus.Succeeded);
        _store.Runs.ShouldBe([interruptedRun, run]);
    }

    [Fact]
    public async Task ExecuteAsync_FinishedEarlierRun_IsLeftAlone()
    {
        var earlierRun = _store.AddRunningRun(SyncTestData.PreviousSyncAt);
        earlierRun.Complete(new SyncChangeCounts(1, 0, 0, 0), SyncTestData.PreviousSyncAt);
        _source.Publish(SupplierProductOf("1"));

        await ExecuteAsync();

        earlierRun.Status.ShouldBe(SyncStatus.Succeeded);
        earlierRun.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ProductWithInvalidBrandAmongValidOnes_AppliesValidOnesAndRecordsError()
    {
        _source.Publish(SupplierProductOf("invalid", brand: InvalidBrand));
        _source.Publish(Enumerable.Range(1, 9).Select(number => SupplierProductOf(number.ToString())).ToArray());

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.CreatedCount.ShouldBe(9);
        _store.Products.Count.ShouldBe(9);
        run.ErrorCount.ShouldBe(1);
        var error = run.Errors.ShouldHaveSingleItem();
        error.Url.ShouldBe(UrlOf("invalid"));
        error.Message.ShouldContain(InvalidBrand);
    }

    [Fact]
    public async Task ExecuteAsync_RejectedProductsAboveThreshold_FailsRunAndChangesNothing()
    {
        _source.Publish(Enumerable.Range(0, 21).Select(number => SupplierProductOf($"invalid-{number}", brand: InvalidBrand)).ToArray());
        _source.Publish(Enumerable.Range(0, 79).Select(number => SupplierProductOf(number.ToString())).ToArray());

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Failed);
        run.ErrorCount.ShouldBe(21);
        _store.AppliedPlanCount.ShouldBe(0);
        _store.Products.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_RejectedAndUnreadableTogether_CountForThresholdAsOne()
    {
        _source.Publish(Enumerable.Range(0, 11).Select(number => SupplierProductOf($"invalid-{number}", brand: InvalidBrand)).ToArray());
        PublishProducts(readable: 79, unreadable: 10);

        var run = await ExecuteAsync();

        run.Status.ShouldBe(SyncStatus.Failed);
        run.ErrorCount.ShouldBe(21);
        _store.AppliedPlanCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_ApplyFails_UpdatesSameRunAsFailedWithPageErrorsAndRethrows()
    {
        _store.ApplyFailure = new InvalidOperationException("The database rejected the changes.");
        _source.PublishUnreadablePage(UrlOf("broken"));
        _source.Publish(Enumerable.Range(1, 9).Select(number => SupplierProductOf(number.ToString())).ToArray());

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ExecuteAsync());

        var run = _store.Runs.ShouldHaveSingleItem();
        exception.ShouldBeSameAs(_store.ApplyFailure);
        run.Status.ShouldBe(SyncStatus.Failed);
        _store.PersistedStatusOf(run).ShouldBe(SyncStatus.Failed);
        run.Trigger.ShouldBe(SyncTrigger.Manual);
        run.CreatedCount.ShouldBe(0);
        run.Errors.Select(error => error.Message).ShouldBe([$"Could not read {UrlOf("broken")}.", "The database rejected the changes."]);
    }

    [Fact]
    public async Task ExecuteAsync_SettingsCannotBeRead_MarksRunFailedAndRethrows()
    {
        _store.SettingsFailure = new InvalidOperationException("The database is unavailable.");

        await Should.ThrowAsync<InvalidOperationException>(() => ExecuteAsync());

        var run = _store.Runs.ShouldHaveSingleItem();
        _store.PersistedStatusOf(run).ShouldBe(SyncStatus.Failed);
        run.Errors.ShouldHaveSingleItem().Message.ShouldBe("The database is unavailable.");
    }

    [Fact]
    public async Task ExecuteAsync_FailedRunCannotBeSaved_PropagatesStoreError()
    {
        _store.FailedRunSaveFailure = new InvalidOperationException("The database is unavailable.");
        _source.SitemapFailure = new SupplierCatalogException("Could not download the sitemap.");

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => ExecuteAsync());

        exception.Message.ShouldBe("The database is unavailable.");
    }

    [Fact]
    public async Task ExecuteAsync_ConfiguredPacing_ReadsSettingsOnceAndUsesThemForEveryRequest()
    {
        _store.Settings.Update(true, new TimeOnly(4, 0), SyncSettings.DefaultTimeZoneId, 20, 1500, 45);
        _source.Publish(SupplierProductOf("1"), SupplierProductOf("2"));

        await ExecuteAsync();

        _store.SettingsReadCount.ShouldBe(1);
        _source.ReceivedPacings.Count.ShouldBe(3);
        _source.ReceivedPacings.ShouldAllBe(pacing =>
            pacing.DelayBetweenRequests == TimeSpan.FromMilliseconds(1500) && pacing.RequestTimeout == TimeSpan.FromSeconds(45));
    }

    [Theory]
    [InlineData(SyncTrigger.Scheduled)]
    [InlineData(SyncTrigger.Manual)]
    public async Task ExecuteAsync_AnyTrigger_RecordsItWithStartAndEnd(SyncTrigger trigger)
    {
        _source.Publish(SupplierProductOf("1"));

        var run = await CreateUseCase().ExecuteAsync(trigger, Token);

        run.Trigger.ShouldBe(trigger);
        run.StartedAt.ShouldBe(Now);
        run.FinishedAt.ShouldBe(Now);
        _store.Runs.ShouldBe([run]);
    }

    private Task<SyncRun> ExecuteAsync() => CreateUseCase().ExecuteAsync(SyncTrigger.Manual, Token);

    private SyncSupplierCatalog CreateUseCase() => new(_store, _source, new FixedTimeProvider(Now));

    private Product AddExistingProduct(string supplierProductId, decimal regularPrice = 1000m)
    {
        var product = ExistingProductOf(_brand, supplierProductId, regularPrice);
        _store.Products.Add(product);
        return product;
    }

    private void PublishProducts(int readable, int unreadable)
    {
        _source.Publish(Enumerable.Range(0, readable).Select(number => SupplierProductOf($"readable-{number}")).ToArray());
        foreach (var number in Enumerable.Range(0, unreadable))
        {
            _source.PublishUnreadablePage(UrlOf($"unreadable-{number}"));
        }
    }

    private void SetFailureThreshold(int failureThresholdPercent)
    {
        var settings = _store.Settings;
        settings.Update(
            settings.IsDailySyncEnabled,
            settings.DailySyncTime,
            settings.TimeZoneId,
            failureThresholdPercent,
            settings.DelayBetweenRequestsMs,
            settings.RequestTimeoutSeconds);
    }
}

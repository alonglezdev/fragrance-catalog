using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Sync;

/// <summary>
/// Brings the catalog up to date with the supplier: reads every product page, then applies all the changes at once or none of them.
/// One instance serves one sync, because the supplier source remembers its last request to keep the pause between requests.
/// Callers must guarantee that only one sync runs at a time: starting one closes any run still marked as running, even a live one.
/// </summary>
/// <param name="store">Catalog data.</param>
/// <param name="source">Supplier catalog, the same instance for the whole sync.</param>
/// <param name="timeProvider">Clock of the run and of the prices it captures.</param>
public sealed class SyncSupplierCatalog(ICatalogSyncStore store, ISupplierCatalogSource source, TimeProvider timeProvider)
{
    private const string SitemapErrorLocation = "sitemap";
    private const string SyncErrorLocation = "sync";
    private const string EmptyCatalogMessage = "The supplier catalog lists no products.";
    private const string CancelledMessage = "The sync was cancelled.";
    private const string InterruptedMessage = "The sync was interrupted before it finished.";

    /// <summary>
    /// Runs one sync. The run is stored as running from the start and that same row is updated when it ends.
    /// Pages that fail and products the domain rejects are recorded and skipped; too many of them, or a catalog that cannot be listed,
    /// end the run as failed without changing anything. A cancelled sync or an unexpected error also ends the run as failed
    /// and is then rethrown. Earlier runs left as running are closed as failed.
    /// </summary>
    /// <param name="trigger">What started the sync.</param>
    /// <param name="cancellationToken">Stops the sync.</param>
    /// <returns>The finished run, whether it succeeded or failed.</returns>
    public async Task<SyncRun> ExecuteAsync(SyncTrigger trigger, CancellationToken cancellationToken)
    {
        var run = await StartRunAsync(trigger, cancellationToken);
        try
        {
            return await RunAsync(run, cancellationToken);
        }
        catch (Exception exception) when (!run.IsFinished)
        {
            var isCancelled = exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
            run.RecordError(SyncErrorLocation, isCancelled ? CancelledMessage : exception.Message);
            await SaveAsFailedAsync(run);
            throw;
        }
    }

    private async Task<SyncRun> StartRunAsync(SyncTrigger trigger, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        foreach (var interruptedRun in await store.GetRunningRunsAsync(cancellationToken))
        {
            interruptedRun.RecordError(SyncErrorLocation, InterruptedMessage);
            interruptedRun.Fail(startedAt);
            await store.SaveFailedRunAsync(interruptedRun, cancellationToken);
        }

        var run = SyncRun.Start(trigger, startedAt);
        await store.SaveStartedRunAsync(run, cancellationToken);
        return run;
    }

    private async Task<SyncRun> RunAsync(SyncRun run, CancellationToken cancellationToken)
    {
        var settings = await store.GetSettingsAsync(cancellationToken);
        var pacing = SupplierRequestPacing.From(settings);
        var productUrls = await ReadProductUrlsAsync(run, pacing, cancellationToken);
        if (productUrls.Count == 0)
        {
            return await SaveAsFailedAsync(run);
        }

        var supplierProducts = await ReadProductsAsync(run, productUrls, pacing, cancellationToken);
        var plan = await CreatePlanAsync(run, supplierProducts, cancellationToken);
        if (settings.IsFailureThresholdExceeded(run.ErrorCount, productUrls.Count))
        {
            return await SaveAsFailedAsync(run);
        }

        await ApplyPlanAsync(run, plan, cancellationToken);
        return run;
    }

    private async Task<IReadOnlyList<string>> ReadProductUrlsAsync(SyncRun run, SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        try
        {
            var productUrls = await source.GetProductUrlsAsync(pacing, cancellationToken);
            if (productUrls.Count == 0)
            {
                run.RecordError(SitemapErrorLocation, EmptyCatalogMessage);
            }

            return productUrls;
        }
        catch (SupplierCatalogException exception)
        {
            run.RecordError(SitemapErrorLocation, exception.Message);
            return [];
        }
    }

    private async Task<List<SupplierProduct>> ReadProductsAsync(
        SyncRun run,
        IReadOnlyList<string> productUrls,
        SupplierRequestPacing pacing,
        CancellationToken cancellationToken)
    {
        var supplierProducts = new List<SupplierProduct>(productUrls.Count);
        foreach (var productUrl in productUrls)
        {
            try
            {
                supplierProducts.Add(await source.GetProductAsync(productUrl, pacing, cancellationToken));
            }
            catch (SupplierCatalogException exception)
            {
                run.RecordError(productUrl, exception.Message);
            }
        }

        return supplierProducts;
    }

    private async Task<CatalogSyncPlan> CreatePlanAsync(SyncRun run, IReadOnlyList<SupplierProduct> supplierProducts, CancellationToken cancellationToken)
    {
        var snapshot = await store.GetCatalogSnapshotAsync(cancellationToken);
        var unreadProductUrls = run.Errors.Select(error => error.Url).ToList();
        var plan = CatalogSyncPlanner.Create(snapshot, supplierProducts, unreadProductUrls, timeProvider.GetUtcNow());
        foreach (var rejectedProduct in plan.RejectedProducts)
        {
            run.RecordError(rejectedProduct.SourceUrl, rejectedProduct.Reason);
        }

        return plan;
    }

    private async Task ApplyPlanAsync(SyncRun run, CatalogSyncPlan plan, CancellationToken cancellationToken)
    {
        run.Complete(plan.Counts, timeProvider.GetUtcNow());
        try
        {
            await store.ApplyPlanAsync(plan, run, cancellationToken);
        }
        catch
        {
            run.ReopenAfterFailedSave();
            throw;
        }
    }

    private async Task<SyncRun> SaveAsFailedAsync(SyncRun run)
    {
        run.Fail(timeProvider.GetUtcNow());
        await store.SaveFailedRunAsync(run, CancellationToken.None);
        return run;
    }
}

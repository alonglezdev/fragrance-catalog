using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Tests.Sync;

internal sealed class FakeCatalogSyncStore : ICatalogSyncStore
{
    public static readonly Guid SupplierId = Guid.CreateVersion7();

    private readonly Dictionary<Guid, SyncStatus> _persistedStatuses = [];

    public SyncSettings Settings { get; } = SyncSettings.CreateDefault();

    public List<Brand> Brands { get; } = [];

    public List<Product> Products { get; } = [];

    public List<SyncRun> Runs { get; } = [];

    public Exception? SettingsFailure { get; set; }

    public Exception? ApplyFailure { get; set; }

    public Exception? FailedRunSaveFailure { get; set; }

    public CancellationTokenSource? CancelOnApply { get; set; }

    public bool AllowCancelledStartOfRun { get; set; }

    public int SettingsReadCount { get; private set; }

    public int AppliedPlanCount { get; private set; }

    public SyncStatus PersistedStatusOf(SyncRun run) => _persistedStatuses[run.Id];

    public SyncRun AddRunningRun(DateTimeOffset startedAt)
    {
        var run = SyncRun.Start(SyncTrigger.Scheduled, startedAt);
        Runs.Add(run);
        _persistedStatuses[run.Id] = run.Status;
        return run;
    }

    public Task<IReadOnlyList<SyncRun>> GetRunningRunsAsync(CancellationToken cancellationToken)
    {
        ThrowIfCancelled(cancellationToken);
        return Task.FromResult<IReadOnlyList<SyncRun>>(Runs.Where(run => run.Status == SyncStatus.Running).ToList());
    }

    public Task SaveStartedRunAsync(SyncRun run, CancellationToken cancellationToken)
    {
        ThrowIfCancelled(cancellationToken);
        Runs.Add(run);
        _persistedStatuses[run.Id] = run.Status;
        return Task.CompletedTask;
    }

    public Task<SyncSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        ThrowIfCancelled(cancellationToken);
        SettingsReadCount++;
        return SettingsFailure is null ? Task.FromResult(Settings) : Task.FromException<SyncSettings>(SettingsFailure);
    }

    public Task<CatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existingProducts = Products.Select(ToExistingProduct).ToList();
        var takenSlugs = Products.Select(product => product.Slug).ToList();
        return Task.FromResult(new CatalogSnapshot(SupplierId, existingProducts, Brands.ToList(), takenSlugs));
    }

    public Task ApplyPlanAsync(CatalogSyncPlan plan, SyncRun run, CancellationToken cancellationToken)
    {
        CancelOnApply?.Cancel();
        cancellationToken.ThrowIfCancellationRequested();
        if (ApplyFailure is not null)
        {
            throw ApplyFailure;
        }

        Brands.AddRange(plan.NewBrands);
        Products.AddRange(plan.NewProducts);
        foreach (var update in plan.ProductUpdates)
        {
            update.ApplyTo(Products.Single(product => product.Id == update.ProductId), plan.SeenAt);
        }

        foreach (var productId in plan.DiscontinuedProductIds)
        {
            Products.Single(product => product.Id == productId).MarkAsDiscontinued();
        }

        UpdateExistingRun(run);
        AppliedPlanCount++;
        return Task.CompletedTask;
    }

    public Task SaveFailedRunAsync(SyncRun run, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailedRunSaveFailure is not null)
        {
            throw FailedRunSaveFailure;
        }

        UpdateExistingRun(run);
        return Task.CompletedTask;
    }

    private static ExistingProduct ToExistingProduct(Product product) => new(
        product.Id,
        product.SupplierProductId,
        product.SourceUrl,
        product.IsDiscontinued,
        product.CurrentSupplierPrice.RegularPrice,
        product.CurrentSupplierPrice.SalePrice);

    private void ThrowIfCancelled(CancellationToken cancellationToken)
    {
        if (!AllowCancelledStartOfRun)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void UpdateExistingRun(SyncRun run)
    {
        Runs.ShouldContain(run);
        _persistedStatuses[run.Id] = run.Status;
    }
}

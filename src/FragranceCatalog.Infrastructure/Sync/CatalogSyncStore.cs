using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Sync;

/// <summary>
/// Reads and writes the sync's data with EF Core. Each save is one SaveChanges call, which EF Core runs in a single transaction.
/// Magna is the only supplier at launch, so the sync works on its products.
/// </summary>
/// <param name="dbContext">Unit of work of the sync.</param>
internal sealed class CatalogSyncStore(FragranceCatalogDbContext dbContext) : ICatalogSyncStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncRun>> GetRunningRunsAsync(CancellationToken cancellationToken) =>
        await dbContext.SyncRuns.AsNoTracking()
            .Where(run => run.Status == SyncStatus.Running)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task SaveStartedRunAsync(SyncRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        dbContext.SyncRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<SyncSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        dbContext.SyncSettings.AsNoTracking().SingleAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<CatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products.AsNoTracking()
            .Where(product => product.SupplierId == SeedData.MagnaSupplierId)
            .Select(product => new
            {
                product.Id,
                product.SupplierProductId,
                product.SourceUrl,
                product.IsDiscontinued,
                CurrentPrice = product.SupplierPrices
                    .OrderByDescending(price => price.CapturedAt)
                    .Select(price => new { price.RegularPrice, price.SalePrice })
                    .First(),
            })
            .ToListAsync(cancellationToken);
        var brands = await dbContext.Brands.AsNoTracking().ToListAsync(cancellationToken);
        var takenProductSlugs = await dbContext.Products.AsNoTracking().Select(product => product.Slug).ToListAsync(cancellationToken);

        var existingProducts = products
            .Select(product => new ExistingProduct(
                product.Id,
                product.SupplierProductId,
                product.SourceUrl,
                product.IsDiscontinued,
                product.CurrentPrice.RegularPrice,
                product.CurrentPrice.SalePrice))
            .ToList();
        return new CatalogSnapshot(SeedData.MagnaSupplierId, existingProducts, brands, takenProductSlugs);
    }

    /// <inheritdoc />
    public async Task ApplyPlanAsync(CatalogSyncPlan plan, SyncRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(run);

        dbContext.Brands.AddRange(plan.NewBrands);
        dbContext.Products.AddRange(plan.NewProducts);
        await ApplyProductUpdatesAsync(plan, cancellationToken);
        await MarkAsDiscontinuedAsync(plan.DiscontinuedProductIds, cancellationToken);
        TrackFinishedRun(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveFailedRunAsync(SyncRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        dbContext.ChangeTracker.Clear();
        TrackFinishedRun(run);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void TrackFinishedRun(SyncRun run)
    {
        dbContext.Entry(run).State = EntityState.Modified;
        dbContext.AddRange(run.Errors);
    }

    private async Task ApplyProductUpdatesAsync(CatalogSyncPlan plan, CancellationToken cancellationToken)
    {
        var productIds = plan.ProductUpdates.Select(update => update.ProductId).ToList();
        var productsById = await dbContext.Products
            .Include(product => product.Images)
            .Include(product => product.SupplierPrices)
            .AsSplitQuery()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var update in plan.ProductUpdates)
        {
            update.ApplyTo(productsById[update.ProductId], plan.SeenAt);
        }
    }

    private async Task MarkAsDiscontinuedAsync(IReadOnlyList<Guid> productIds, CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.MarkAsDiscontinued();
        }
    }
}

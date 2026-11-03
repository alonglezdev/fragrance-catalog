using FragranceCatalog.Application.Alerts;
using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Alerts;

/// <summary>Reads the supplier price history with EF Core. A price belongs to a sync when it was captured while that run was open.</summary>
/// <param name="dbContext">Unit of work of the request.</param>
internal sealed class PricingAlertStore(FragranceCatalogDbContext dbContext) : IPricingAlertStore
{
    private const int CurrentAndPreviousPrices = 2;

    /// <inheritdoc />
    public async Task<IReadOnlyList<CostChange>> GetCostChangesOfLastSyncAsync(CancellationToken cancellationToken)
    {
        var lastRun = await dbContext.SyncRuns.AsNoTracking()
            .Where(run => run.Status == SyncStatus.Succeeded)
            .OrderByDescending(run => run.FinishedAt)
            .Select(run => new { run.StartedAt, run.FinishedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (lastRun is null)
        {
            return [];
        }

        var products = await (
            from product in dbContext.Products.AsNoTracking()
            join brand in dbContext.Brands on product.BrandId equals brand.Id
            where product.SupplierPrices.Any(price => price.CapturedAt >= lastRun.StartedAt && price.CapturedAt <= lastRun.FinishedAt)
            select new
            {
                product.Id,
                product.Name,
                BrandName = brand.Name,
                Costs = product.SupplierPrices
                    .OrderByDescending(price => price.CapturedAt)
                    .Take(CurrentAndPreviousPrices)
                    .Select(price => price.SalePrice ?? price.RegularPrice)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return products
            .Where(product => product.Costs.Count == CurrentAndPreviousPrices)
            .Select(product => new CostChange(product.Id, product.Name, product.BrandName, product.Costs[1], product.Costs[0]))
            .ToList();
    }
}

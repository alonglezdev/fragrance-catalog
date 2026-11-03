using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Infrastructure.Persistence;
using FragranceCatalog.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FragranceCatalog.Infrastructure.Pricing;

/// <summary>Reads and changes pricing data with EF Core. Reads for display are not tracked, so nothing they return can be saved by accident.</summary>
/// <param name="dbContext">Unit of work of the request.</param>
internal sealed class PricingStore(FragranceCatalogDbContext dbContext) : IPricingStore, IPricingReader
{
    /// <inheritdoc />
    public async Task<PricingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.PricingSettings.AsNoTracking().SingleAsync(cancellationToken);
        var ranges = await dbContext.PriceRanges.AsNoTracking().OrderBy(range => range.MinCost).ToListAsync(cancellationToken);
        var rules = await dbContext.PricingRules.AsNoTracking().OrderByDescending(rule => rule.CreatedAt).ToListAsync(cancellationToken);
        return new PricingConfiguration(settings, ranges, rules);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductCost>> GetProductCostsAsync(CancellationToken cancellationToken) =>
        await (
            from product in dbContext.Products.AsNoTracking()
            join brand in dbContext.Brands on product.BrandId equals brand.Id
            where !product.IsDiscontinued
            let currentPrice = product.SupplierPrices.OrderByDescending(price => price.CapturedAt).First()
            orderby product.Name
            select new ProductCost(product.Id, product.Name, product.BrandId, brand.Name, currentPrice.SalePrice ?? currentPrice.RegularPrice))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PricingSettings> GetSettingsForUpdateAsync(CancellationToken cancellationToken) =>
        dbContext.PricingSettings.SingleAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ReplaceRangesAsync(IReadOnlyList<PriceRange> ranges, CancellationToken cancellationToken)
    {
        var currentRanges = await dbContext.PriceRanges.ToListAsync(cancellationToken);
        dbContext.PriceRanges.RemoveRange(currentRanges);
        dbContext.PriceRanges.AddRange(ranges);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<PricingRule?> FindRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken) =>
        dbContext.PricingRules.SingleOrDefaultAsync(rule => rule.Id == ruleId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> TryAddRuleAsync(PricingRule rule, CancellationToken cancellationToken)
    {
        dbContext.PricingRules.Add(rule);
        return TrySaveRuleChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TrySaveRuleChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsDuplicateActiveRule(exception))
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> HasOtherActiveRuleAsync(PricingRuleScope scope, Guid targetId, Guid? excludedRuleId, CancellationToken cancellationToken) =>
        dbContext.PricingRules.AnyAsync(
            rule => rule.IsActive
                && rule.Id != excludedRuleId
                && (scope == PricingRuleScope.Brand ? rule.BrandId == targetId : rule.ProductId == targetId),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> TargetExistsAsync(PricingRuleScope scope, Guid targetId, CancellationToken cancellationToken) =>
        scope == PricingRuleScope.Brand
            ? dbContext.Brands.AnyAsync(brand => brand.Id == targetId, cancellationToken)
            : dbContext.Products.AnyAsync(product => product.Id == targetId, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private static bool IsDuplicateActiveRule(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: PricingRuleConfiguration.ActiveBrandRuleIndex or PricingRuleConfiguration.ActiveProductRuleIndex,
        };
}

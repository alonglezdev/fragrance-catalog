using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Products;

/// <summary>Lists products for the administrator with their cost, calculated price and profit.</summary>
/// <param name="store">Product data.</param>
/// <param name="reader">Pricing configuration.</param>
/// <param name="timeProvider">Instant the prices are calculated for.</param>
public sealed class ListAdminProducts(IAdminProductStore store, IPricingReader reader, TimeProvider timeProvider)
{
    /// <summary>Applies the filters; low profit is filtered after pricing because the price is never stored.</summary>
    /// <param name="filter">The filters.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    public async Task<IReadOnlyList<AdminProductDto>> ExecuteAsync(AdminProductFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var configuration = await reader.GetConfigurationAsync(cancellationToken);
        var records = await store.ListAsync(filter, cancellationToken);
        var now = timeProvider.GetUtcNow();

        return records
            .Select(record => ToDto(record, PriceCalculator.Calculate(record.Cost, record.BrandId, record.Id, configuration, now)))
            .Where(product => !filter.IsLowProfitOnly || product.IsLowProfit)
            .ToList();
    }

    private static AdminProductDto ToDto(AdminProductRecord record, PriceQuote quote) => new(
        record.Id,
        record.Name,
        record.BrandName,
        record.Cost,
        quote.SalePrice,
        quote.Profit,
        quote.IsLowProfit,
        quote.AppliedRule.Type,
        quote.AppliedRule.Id,
        record.IsAvailable,
        record.IsHidden,
        record.IsFeatured,
        record.IsDiscontinued);
}

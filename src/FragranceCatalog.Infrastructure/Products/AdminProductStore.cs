using FragranceCatalog.Application.Products;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Products;

/// <summary>Reads and changes products for the admin panel with EF Core.</summary>
/// <param name="dbContext">Unit of work of the request.</param>
internal sealed class AdminProductStore(FragranceCatalogDbContext dbContext) : IAdminProductStore
{
    private const string LikeEscape = "\\";

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminProductRecord>> ListAsync(AdminProductFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var pattern = string.IsNullOrWhiteSpace(filter.Search) ? null : $"%{EscapeLikePattern(filter.Search.Trim())}%";
        return await (
            from product in dbContext.Products.AsNoTracking()
            join brand in dbContext.Brands on product.BrandId equals brand.Id
            where filter.IncludeDiscontinued || !product.IsDiscontinued
            where filter.BrandId == null || product.BrandId == filter.BrandId
            where pattern == null || EF.Functions.ILike(product.Name, pattern, LikeEscape) || EF.Functions.ILike(brand.Name, pattern, LikeEscape)
            let currentPrice = product.SupplierPrices.OrderByDescending(price => price.CapturedAt).First()
            orderby product.Name
            select new AdminProductRecord(
                product.Id,
                product.Name,
                product.BrandId,
                brand.Name,
                currentPrice.SalePrice ?? currentPrice.RegularPrice,
                product.IsAvailable,
                product.IsHidden,
                product.IsFeatured,
                product.IsDiscontinued))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Product?> FindForUpdateAsync(Guid productId, CancellationToken cancellationToken) =>
        dbContext.Products.SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private static string EscapeLikePattern(string text) =>
        text.Replace(LikeEscape, $"{LikeEscape}{LikeEscape}", StringComparison.Ordinal)
            .Replace("%", $"{LikeEscape}%", StringComparison.Ordinal)
            .Replace("_", $"{LikeEscape}_", StringComparison.Ordinal);
}

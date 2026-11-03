using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Sync;

/// <summary>Fresh supplier data for a product that already exists. It carries only supplier-owned data, so a sync cannot change what the administrator controls.</summary>
/// <param name="ProductId">Product to update.</param>
/// <param name="Details">Latest supplier-owned data.</param>
/// <param name="RegularPrice">Latest supplier list price.</param>
/// <param name="SalePrice">Latest supplier offer price, or null when there is no offer.</param>
/// <param name="ImageUrls">Latest photos, main photo first.</param>
/// <param name="IsReactivated">Whether the product was discontinued and came back.</param>
public sealed record ProductUpdate(
    Guid ProductId,
    SupplierProductDetails Details,
    decimal RegularPrice,
    decimal? SalePrice,
    IReadOnlyList<string> ImageUrls,
    bool IsReactivated)
{
    /// <summary>Applies the update to the loaded product; a price history entry is added only if the price changed.</summary>
    /// <param name="product">The product, loaded with its images and supplier prices.</param>
    /// <param name="seenAt">Instant the sync read the product.</param>
    public void ApplyTo(Product product, DateTimeOffset seenAt)
    {
        ArgumentNullException.ThrowIfNull(product);

        product.UpdateFromSupplier(Details, seenAt);
        product.RecordSupplierPrice(RegularPrice, SalePrice, seenAt);
        product.ReplaceImages(ImageUrls);
        if (IsReactivated)
        {
            product.Reactivate();
        }
    }
}

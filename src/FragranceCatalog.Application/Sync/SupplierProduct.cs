using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Sync;

/// <summary>A product as read from the supplier site, before it is matched against the catalog.</summary>
/// <param name="SupplierProductId">The supplier's internal product number, stable even if the supplier edits the SKU.</param>
/// <param name="SourceUrl">Product page the data was read from.</param>
/// <param name="Name">Product name as the supplier writes it.</param>
/// <param name="Brand">Brand name as the supplier shows it.</param>
/// <param name="RegularPrice">Supplier list price in MXN.</param>
/// <param name="SalePrice">Supplier offer price in MXN, or null when there is no offer.</param>
/// <param name="Images">Absolute photo URLs, main photo first, without duplicates.</param>
/// <param name="Description">Plain-text description; may be empty.</param>
/// <param name="Gender">Audience the fragrance is marketed to.</param>
/// <param name="SizeMl">Bottle size in milliliters, or null when the name does not state it.</param>
/// <param name="IsAvailable">Whether the supplier currently has stock.</param>
public sealed record SupplierProduct(
    string SupplierProductId,
    string SourceUrl,
    string Name,
    string Brand,
    decimal RegularPrice,
    decimal? SalePrice,
    IReadOnlyList<string> Images,
    string Description,
    Gender Gender,
    int? SizeMl,
    bool IsAvailable);

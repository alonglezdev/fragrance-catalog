namespace FragranceCatalog.Domain.Products;

/// <summary>
/// Product data owned by the supplier, read on every sync. Fields controlled by the administrator are deliberately absent.
/// </summary>
/// <param name="SourceUrl">Absolute URL of the product page on the supplier site.</param>
/// <param name="Name">Product name as the supplier writes it.</param>
/// <param name="BrandId">Brand the product belongs to.</param>
/// <param name="Gender">Audience inferred from the name.</param>
/// <param name="SizeMl">Bottle size in milliliters, or null when the name does not state it.</param>
/// <param name="Description">Plain-text description; may be empty.</param>
/// <param name="IsAvailable">Whether the supplier currently has stock.</param>
public sealed record SupplierProductDetails(
    string SourceUrl,
    string Name,
    Guid BrandId,
    Gender Gender,
    int? SizeMl,
    string Description,
    bool IsAvailable);

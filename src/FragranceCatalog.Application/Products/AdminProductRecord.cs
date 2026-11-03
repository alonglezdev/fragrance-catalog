namespace FragranceCatalog.Application.Products;

/// <summary>A stored product before its price is calculated.</summary>
/// <param name="Id">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandId">Brand of the product.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="Cost">Current supplier cost.</param>
/// <param name="IsAvailable">Whether the supplier has stock.</param>
/// <param name="IsHidden">Whether the administrator hid it.</param>
/// <param name="IsFeatured">Whether the administrator featured it.</param>
/// <param name="IsDiscontinued">Whether it left the supplier catalog.</param>
public sealed record AdminProductRecord(
    Guid Id,
    string Name,
    Guid BrandId,
    string BrandName,
    decimal Cost,
    bool IsAvailable,
    bool IsHidden,
    bool IsFeatured,
    bool IsDiscontinued);

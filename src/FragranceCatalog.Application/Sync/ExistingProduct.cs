namespace FragranceCatalog.Application.Sync;

/// <summary>What the sync needs to know about a product already imported from the supplier. Fields owned by the administrator are deliberately absent.</summary>
/// <param name="Id">Identifier of the product.</param>
/// <param name="SupplierProductId">The supplier's own identifier, used to match the product across syncs.</param>
/// <param name="SourceUrl">Supplier page the product was last read from.</param>
/// <param name="IsDiscontinued">Whether the product had already left the supplier catalog.</param>
/// <param name="RegularPrice">Current supplier list price.</param>
/// <param name="SalePrice">Current supplier offer price, or null when there is no offer.</param>
public sealed record ExistingProduct(
    Guid Id,
    string SupplierProductId,
    string SourceUrl,
    bool IsDiscontinued,
    decimal RegularPrice,
    decimal? SalePrice);

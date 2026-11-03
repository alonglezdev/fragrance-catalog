using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Sync;

/// <summary>The catalog as it is before a sync changes it.</summary>
/// <param name="SupplierId">Supplier being synced; new products are created under it.</param>
/// <param name="Products">Every product of that supplier, discontinued ones included.</param>
/// <param name="Brands">Every known brand.</param>
/// <param name="TakenProductSlugs">Slugs in use by any product, because public URLs are unique across suppliers.</param>
public sealed record CatalogSnapshot(
    Guid SupplierId,
    IReadOnlyList<ExistingProduct> Products,
    IReadOnlyList<Brand> Brands,
    IReadOnlyCollection<Slug> TakenProductSlugs);

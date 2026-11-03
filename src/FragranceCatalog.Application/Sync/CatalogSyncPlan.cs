using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Sync;

/// <summary>Every change a sync will make to the catalog, calculated before anything is written.</summary>
/// <param name="SeenAt">Instant recorded as the moment the products were last seen and their new prices captured.</param>
/// <param name="NewBrands">Brands to create.</param>
/// <param name="NewProducts">Products to create, with their first price and photos.</param>
/// <param name="ProductUpdates">Products still in the supplier catalog.</param>
/// <param name="DiscontinuedProductIds">Products that left the supplier catalog.</param>
/// <param name="RejectedProducts">Products read from the supplier that stay out of the plan because the domain rejects them.</param>
/// <param name="Counts">How many products each kind of change touches.</param>
public sealed record CatalogSyncPlan(
    DateTimeOffset SeenAt,
    IReadOnlyList<Brand> NewBrands,
    IReadOnlyList<Product> NewProducts,
    IReadOnlyList<ProductUpdate> ProductUpdates,
    IReadOnlyList<Guid> DiscontinuedProductIds,
    IReadOnlyList<RejectedProduct> RejectedProducts,
    SyncChangeCounts Counts);

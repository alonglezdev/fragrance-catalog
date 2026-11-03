using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Sync;

/// <summary>
/// Compares what the supplier publishes with the catalog and decides what changes, without any I/O.
/// Brands are matched by slug, so "LATTAFA" and "Lattafa" are one brand. A product whose page could not be read
/// is left untouched: it is still in the supplier catalog, so it must not be discontinued.
/// A product the domain rejects is reported instead of failing the sync, and leaves no trace in the plan.
/// </summary>
public sealed class CatalogSyncPlanner
{
    private const int FirstSlugSuffix = 2;

    private readonly Dictionary<Slug, Guid> _brandIdsBySlug;
    private readonly HashSet<Slug> _takenProductSlugs;
    private readonly List<Brand> _newBrands = [];
    private readonly List<Product> _newProducts = [];
    private readonly List<ProductUpdate> _productUpdates = [];
    private readonly List<RejectedProduct> _rejectedProducts = [];
    private readonly Guid _supplierId;
    private readonly DateTimeOffset _seenAt;
    private int _priceChangedCount;

    private CatalogSyncPlanner(CatalogSnapshot snapshot, DateTimeOffset seenAt)
    {
        _brandIdsBySlug = snapshot.Brands.ToDictionary(brand => brand.Slug, brand => brand.Id);
        _takenProductSlugs = [.. snapshot.TakenProductSlugs];
        _supplierId = snapshot.SupplierId;
        _seenAt = seenAt;
    }

    /// <summary>Calculates the changes of a sync. If two pages report the same supplier product, the first one is used.</summary>
    /// <param name="snapshot">The catalog before the sync.</param>
    /// <param name="supplierProducts">Products read from the supplier.</param>
    /// <param name="unreadProductUrls">Pages listed by the supplier that could not be read.</param>
    /// <param name="seenAt">Instant the sync read the products.</param>
    public static CatalogSyncPlan Create(
        CatalogSnapshot snapshot,
        IReadOnlyList<SupplierProduct> supplierProducts,
        IReadOnlyCollection<string> unreadProductUrls,
        DateTimeOffset seenAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(supplierProducts);
        ArgumentNullException.ThrowIfNull(unreadProductUrls);

        var uniqueSupplierProducts = supplierProducts.DistinctBy(product => product.SupplierProductId, StringComparer.Ordinal).ToList();
        var planner = new CatalogSyncPlanner(snapshot, seenAt);
        planner.AddSupplierProducts(snapshot.Products, uniqueSupplierProducts);
        var discontinuedProductIds = FindDiscontinuedProductIds(snapshot.Products, uniqueSupplierProducts, unreadProductUrls);
        return planner.ToPlan(discontinuedProductIds);
    }

    private static List<Guid> FindDiscontinuedProductIds(
        IReadOnlyList<ExistingProduct> existingProducts,
        IReadOnlyList<SupplierProduct> supplierProducts,
        IReadOnlyCollection<string> unreadProductUrls)
    {
        var readSupplierProductIds = supplierProducts.Select(product => product.SupplierProductId).ToHashSet(StringComparer.Ordinal);
        var unreadUrls = unreadProductUrls.ToHashSet(StringComparer.Ordinal);

        return existingProducts
            .Where(product => !product.IsDiscontinued)
            .Where(product => !readSupplierProductIds.Contains(product.SupplierProductId))
            .Where(product => !unreadUrls.Contains(product.SourceUrl))
            .Select(product => product.Id)
            .ToList();
    }

    private static bool HasPriceChanged(ExistingProduct existingProduct, SupplierProduct supplierProduct) =>
        existingProduct.RegularPrice != supplierProduct.RegularPrice || existingProduct.SalePrice != supplierProduct.SalePrice;

    private static SupplierProductDetails ToDetails(SupplierProduct product, Guid brandId) => new(
        product.SourceUrl,
        product.Name,
        brandId,
        product.Gender,
        product.SizeMl,
        product.Description,
        product.IsAvailable);

    private void AddSupplierProducts(IReadOnlyList<ExistingProduct> existingProducts, IReadOnlyList<SupplierProduct> supplierProducts)
    {
        var existingBySupplierId = existingProducts.ToDictionary(product => product.SupplierProductId, StringComparer.Ordinal);
        foreach (var supplierProduct in supplierProducts)
        {
            try
            {
                AddSupplierProduct(supplierProduct, existingBySupplierId.GetValueOrDefault(supplierProduct.SupplierProductId));
            }
            catch (ArgumentException exception)
            {
                _rejectedProducts.Add(new RejectedProduct(supplierProduct.SourceUrl, exception.Message));
            }
        }
    }

    private void AddSupplierProduct(SupplierProduct supplierProduct, ExistingProduct? existingProduct)
    {
        var brand = ResolveBrand(supplierProduct.Brand);
        var details = ToDetails(supplierProduct, brand.Id);
        Product.ValidateSupplierData(details, supplierProduct.RegularPrice, supplierProduct.SalePrice, supplierProduct.Images);

        if (existingProduct is null)
        {
            AddNewProduct(supplierProduct, details);
        }
        else
        {
            AddUpdate(existingProduct, supplierProduct, details);
        }

        RememberBrand(brand.NewBrand);
    }

    private ResolvedBrand ResolveBrand(string brandName)
    {
        var slug = Slug.FromText(brandName);
        if (_brandIdsBySlug.TryGetValue(slug, out var brandId))
        {
            return new ResolvedBrand(brandId, null);
        }

        var brand = Brand.Create(brandName, slug);
        return new ResolvedBrand(brand.Id, brand);
    }

    private void RememberBrand(Brand? newBrand)
    {
        if (newBrand is null)
        {
            return;
        }

        _newBrands.Add(newBrand);
        _brandIdsBySlug.Add(newBrand.Slug, newBrand.Id);
    }

    private void AddUpdate(ExistingProduct existingProduct, SupplierProduct supplierProduct, SupplierProductDetails details)
    {
        if (HasPriceChanged(existingProduct, supplierProduct))
        {
            _priceChangedCount++;
        }

        _productUpdates.Add(new ProductUpdate(
            existingProduct.Id,
            details,
            supplierProduct.RegularPrice,
            supplierProduct.SalePrice,
            supplierProduct.Images,
            existingProduct.IsDiscontinued));
    }

    private void AddNewProduct(SupplierProduct supplierProduct, SupplierProductDetails details)
    {
        var product = Product.Create(
            _supplierId,
            supplierProduct.SupplierProductId,
            ReserveProductSlug(supplierProduct.Name),
            details,
            supplierProduct.RegularPrice,
            supplierProduct.SalePrice,
            _seenAt);
        product.ReplaceImages(supplierProduct.Images);
        _newProducts.Add(product);
    }

    private Slug ReserveProductSlug(string productName)
    {
        var baseSlug = Slug.FromText(productName);
        var slug = baseSlug;
        for (var suffix = FirstSlugSuffix; !_takenProductSlugs.Add(slug); suffix++)
        {
            slug = Slug.FromText($"{baseSlug.Value}-{suffix}");
        }

        return slug;
    }

    private CatalogSyncPlan ToPlan(IReadOnlyList<Guid> discontinuedProductIds)
    {
        var counts = new SyncChangeCounts(
            _newProducts.Count,
            _priceChangedCount,
            discontinuedProductIds.Count,
            _productUpdates.Count(update => update.IsReactivated));

        return new CatalogSyncPlan(_seenAt, _newBrands, _newProducts, _productUpdates, discontinuedProductIds, _rejectedProducts, counts);
    }

    private readonly record struct ResolvedBrand(Guid Id, Brand? NewBrand);
}

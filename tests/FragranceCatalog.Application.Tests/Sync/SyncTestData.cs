using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Tests.Sync;

internal static class SyncTestData
{
    public const string BrandName = "Lattafa";
    public const string ImageUrl = "https://cdn.example.com/products/1/main.jpg";

    public static readonly DateTimeOffset PreviousSyncAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset Now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    public static string UrlOf(string supplierProductId) => $"https://magnaperfumes.com/product-{supplierProductId}/";

    public static SupplierProduct SupplierProductOf(
        string supplierProductId,
        decimal regularPrice = 1000m,
        decimal? salePrice = null,
        string brand = BrandName,
        string? name = null) => new(
            supplierProductId,
            UrlOf(supplierProductId),
            name ?? $"Asad {supplierProductId} EDP 100ml Caballero",
            brand,
            regularPrice,
            salePrice,
            [ImageUrl],
            "Fragancia masculina",
            Gender.Men,
            100,
            true);

    public static Brand ExistingBrand(string name = BrandName) => Brand.Create(name, Slug.FromText(name));

    public static Product ExistingProductOf(Brand brand, string supplierProductId, decimal regularPrice = 1000m, decimal? salePrice = null)
    {
        var details = new SupplierProductDetails(
            UrlOf(supplierProductId),
            $"Asad {supplierProductId} EDP 100ml Caballero",
            brand.Id,
            Gender.Men,
            100,
            "Fragancia masculina",
            true);
        var product = Product.Create(
            FakeCatalogSyncStore.SupplierId,
            supplierProductId,
            Slug.FromText(details.Name),
            details,
            regularPrice,
            salePrice,
            PreviousSyncAt);
        product.ReplaceImages([ImageUrl]);
        return product;
    }
}

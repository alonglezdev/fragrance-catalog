using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Persistence;

namespace FragranceCatalog.Infrastructure.Tests.Persistence;

internal static class CatalogTestData
{
    public static readonly DateTimeOffset CapturedAt = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    public static Brand NewBrand(string? name = null)
    {
        var brandName = name ?? Unique("brand");
        return Brand.Create(brandName, Slug.FromText(brandName));
    }

    public static Product NewProduct(Brand brand, string? name = null, decimal regularPrice = 1000m, decimal? salePrice = null, DateTimeOffset? capturedAt = null)
    {
        var productName = name ?? Unique("product");
        var details = new SupplierProductDetails($"https://magnaperfumes.com/{Guid.NewGuid():N}/", productName, brand.Id, Gender.Men, 100, "Fragancia", true);
        return Product.Create(SeedData.MagnaSupplierId, Unique("id"), Slug.FromText(Unique("slug")), details, regularPrice, salePrice, capturedAt ?? CapturedAt);
    }
}

using FragranceCatalog.Infrastructure.Suppliers.Magna;
using Microsoft.Extensions.Options;

namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

internal static class MagnaFixtures
{
    public const string ArabicBrandPage = "product-arabic-brand.html";
    public const string RegularPricePage = "product-regular-price.html";
    public const string GiftSetPage = "product-gift-set.html";
    public const string TrioSetPage = "product-trio-set.html";
    public const string OutOfStockPage = "product-out-of-stock.modified.html";
    public const string SitemapIndex = "sitemap-index.xml";
    public const string ProductSitemapPage = "sitemap-products-page-1.xml";

    public static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Magna", fileName));

    public static MagnaProductPageParser CreateParser() => new(Options.Create(new MagnaOptions()));
}

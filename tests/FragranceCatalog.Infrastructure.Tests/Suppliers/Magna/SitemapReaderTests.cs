using FragranceCatalog.Infrastructure.Suppliers.Magna;

namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

public sealed class SitemapReaderTests
{
    private const string ProductsType = "products";

    private const string IndexWithTwoProductPages = """
        <sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=pages&amp;page=1</loc></sitemap>
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=products&amp;page=1</loc></sitemap>
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=categories&amp;page=1</loc></sitemap>
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=products&amp;page=2</loc></sitemap>
        </sitemapindex>
        """;

    private const string UrlSetWithDuplicate = """
        <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
          <url><loc> https://magnaperfumes.com/a/ </loc></url>
          <url><loc>https://magnaperfumes.com/b/</loc></url>
          <url><loc>https://magnaperfumes.com/a/</loc></url>
          <url><loc></loc></url>
        </urlset>
        """;

    private readonly SitemapReader _reader = new();

    [Fact]
    public void ReadProductSitemapUrls_RealMagnaIndex_ReturnsOnlyProductSitemap()
    {
        var xml = MagnaFixtures.Read(MagnaFixtures.SitemapIndex);

        var urls = _reader.ReadProductSitemapUrls(xml, ProductsType);

        urls.ShouldBe(["https://magnaperfumes.com/sitemap.xml?type=products&page=1"]);
    }

    [Fact]
    public void ReadProductSitemapUrls_IndexWithSeveralProductPages_ReturnsEveryPageInOrder()
    {
        var urls = _reader.ReadProductSitemapUrls(IndexWithTwoProductPages, ProductsType);

        urls.ShouldBe(
        [
            "https://magnaperfumes.com/sitemap.xml?type=products&page=1",
            "https://magnaperfumes.com/sitemap.xml?type=products&page=2",
        ]);
    }

    [Fact]
    public void ReadPageUrls_RealMagnaProductSitemap_ReturnsEveryProductPage()
    {
        var xml = MagnaFixtures.Read(MagnaFixtures.ProductSitemapPage);

        var urls = _reader.ReadPageUrls(xml);

        urls.Count.ShouldBe(1047);
        urls[0].ShouldBe("https://magnaperfumes.com/Can_Can_Burlesque_100ml");
        urls.ShouldContain("https://magnaperfumes.com/lattafa-asad-elixir-edp-100ml-caballero/");
    }

    [Fact]
    public void ReadPageUrls_DuplicateAndBlankLocations_ReturnsEachUrlOnceTrimmed()
    {
        var urls = _reader.ReadPageUrls(UrlSetWithDuplicate);

        urls.ShouldBe(["https://magnaperfumes.com/a/", "https://magnaperfumes.com/b/"]);
    }
}

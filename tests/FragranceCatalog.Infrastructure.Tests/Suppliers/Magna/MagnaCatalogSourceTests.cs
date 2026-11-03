using System.Net;
using FragranceCatalog.Application.Sync;
using FragranceCatalog.Infrastructure.Suppliers.Magna;
using Microsoft.Extensions.Options;

namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

public sealed class MagnaCatalogSourceTests
{
    private const string SitemapIndexUrl = "https://magnaperfumes.com/sitemap.xml";
    private const string ProductPageUrl = "https://magnaperfumes.com/lattafa-asad-elixir-edp-100ml-caballero/";

    private const string IndexWithTwoProductPages = """
        <sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=pages&amp;page=1</loc></sitemap>
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=products&amp;page=1</loc></sitemap>
          <sitemap><loc>https://magnaperfumes.com/sitemap.xml?type=products&amp;page=2</loc></sitemap>
        </sitemapindex>
        """;

    private static readonly SupplierRequestPacing _noDelay = new(TimeSpan.Zero, TimeSpan.FromSeconds(5));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetProductUrlsAsync_IndexWithTwoProductPages_ReadsBothPagesAndSkipsOtherSitemaps()
    {
        var handler = new StubHttpHandler()
            .Respond(SitemapIndexUrl, IndexWithTwoProductPages)
            .Respond("https://magnaperfumes.com/sitemap.xml?type=products&page=1", UrlSet("https://magnaperfumes.com/a/", "https://magnaperfumes.com/b/"))
            .Respond("https://magnaperfumes.com/sitemap.xml?type=products&page=2", UrlSet("https://magnaperfumes.com/b/", "https://magnaperfumes.com/c/"));
        var source = CreateSource(handler);

        var urls = await source.GetProductUrlsAsync(_noDelay, Token);

        urls.ShouldBe(["https://magnaperfumes.com/a/", "https://magnaperfumes.com/b/", "https://magnaperfumes.com/c/"]);
        handler.RequestedUrls.ShouldNotContain("https://magnaperfumes.com/sitemap.xml?type=pages&page=1");
    }

    [Fact]
    public async Task GetProductAsync_SavedProductPage_ParsesDownloadedHtml()
    {
        var handler = new StubHttpHandler().Respond(ProductPageUrl, MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage));
        var source = CreateSource(handler);

        var product = await source.GetProductAsync(ProductPageUrl, _noDelay, Token);

        product.SupplierProductId.ShouldBe("4909");
        product.SourceUrl.ShouldBe(ProductPageUrl);
    }

    [Fact]
    public async Task GetProductAsync_ServerError_ThrowsHttpRequestException()
    {
        var handler = new StubHttpHandler().Fail(ProductPageUrl, HttpStatusCode.InternalServerError);
        var source = CreateSource(handler);

        await Should.ThrowAsync<HttpRequestException>(() => source.GetProductAsync(ProductPageUrl, _noDelay, Token));
    }

    [Fact]
    public async Task GetProductAsync_ServerSlowerThanTimeout_ThrowsTimeoutException()
    {
        var handler = new StubHttpHandler().Hang(ProductPageUrl);
        var source = CreateSource(handler);
        var shortTimeout = new SupplierRequestPacing(TimeSpan.Zero, TimeSpan.FromMilliseconds(50));

        await Should.ThrowAsync<TimeoutException>(() => source.GetProductAsync(ProductPageUrl, shortTimeout, Token));
    }

    [Fact]
    public async Task GetProductAsync_SecondRequestRightAfterFirst_WaitsConfiguredDelay()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage);
        var handler = new StubHttpHandler().Respond(ProductPageUrl, html);
        var clock = new RecordingTimeProvider();
        var source = CreateSource(handler, clock);
        var pacing = new SupplierRequestPacing(TimeSpan.FromMilliseconds(1000), TimeSpan.FromSeconds(5));

        await source.GetProductAsync(ProductPageUrl, pacing, Token);
        await source.GetProductAsync(ProductPageUrl, pacing, Token);

        clock.RequestedDelays.ShouldBe([TimeSpan.FromMilliseconds(1000)]);
    }

    private static MagnaCatalogSource CreateSource(StubHttpHandler handler, TimeProvider? timeProvider = null)
    {
        var options = Options.Create(new MagnaOptions());
        return new MagnaCatalogSource(
            new HttpClient(handler),
            options,
            new SitemapReader(),
            new MagnaProductPageParser(options),
            timeProvider ?? TimeProvider.System);
    }

    private static string UrlSet(params string[] urls) =>
        $"""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">{string.Concat(urls.Select(url => $"<url><loc>{url}</loc></url>"))}</urlset>""";
}

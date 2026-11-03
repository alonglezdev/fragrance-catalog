using FragranceCatalog.Application.Sync;
using Microsoft.Extensions.Options;
using Polly;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

/// <summary>
/// Reads the Magna Perfumes catalog over HTTP: every product sitemap page listed in the index, then one page per product.
/// Requests are spaced and timed out as the sync's pacing says; transient failures are retried by the HTTP pipeline.
/// One instance should serve one sync, because it remembers when its last request started.
/// </summary>
/// <param name="httpClient">Client configured with the User-Agent and the standard resilience pipeline.</param>
/// <param name="options">Sitemap URL and product sitemap marker.</param>
/// <param name="sitemapReader">Reads the downloaded sitemaps.</param>
/// <param name="productPageParser">Reads the downloaded product pages.</param>
/// <param name="timeProvider">Clock used to space requests.</param>
public sealed class MagnaCatalogSource(
    HttpClient httpClient,
    IOptions<MagnaOptions> options,
    SitemapReader sitemapReader,
    MagnaProductPageParser productPageParser,
    TimeProvider timeProvider) : ISupplierCatalogSource
{
    private DateTimeOffset? _lastRequestStartedAt;

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetProductUrlsAsync(SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pacing);

        var sitemapIndex = await DownloadAsync(options.Value.SitemapUrl, pacing, cancellationToken);
        var productUrls = new List<string>();
        foreach (var productSitemapUrl in sitemapReader.ReadProductSitemapUrls(sitemapIndex, options.Value.ProductSitemapType))
        {
            var productSitemap = await DownloadAsync(productSitemapUrl, pacing, cancellationToken);
            productUrls.AddRange(sitemapReader.ReadPageUrls(productSitemap));
        }

        return productUrls.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc />
    public async Task<SupplierProduct> GetProductAsync(string productUrl, SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productUrl);
        ArgumentNullException.ThrowIfNull(pacing);

        var html = await DownloadAsync(productUrl, pacing, cancellationToken);
        return productPageParser.Parse(html, productUrl);
    }

    private async Task<string> DownloadAsync(string url, SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        try
        {
            return await SendAsync(url, pacing, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutException or ExecutionRejectedException)
        {
            throw new SupplierCatalogException($"Could not download {url}: {exception.Message}", exception);
        }
    }

    private async Task<string> SendAsync(string url, SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        await WaitForTurnAsync(pacing.DelayBetweenRequests, cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(pacing.RequestTimeout);
        try
        {
            using var response = await httpClient.GetAsync(url, timeout.Token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(timeout.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"{url} did not respond within {pacing.RequestTimeout}.", exception);
        }
    }

    private async Task WaitForTurnAsync(TimeSpan delayBetweenRequests, CancellationToken cancellationToken)
    {
        if (_lastRequestStartedAt is { } lastRequestStartedAt)
        {
            var remainingDelay = delayBetweenRequests - (timeProvider.GetUtcNow() - lastRequestStartedAt);
            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay, timeProvider, cancellationToken);
            }
        }

        _lastRequestStartedAt = timeProvider.GetUtcNow();
    }
}

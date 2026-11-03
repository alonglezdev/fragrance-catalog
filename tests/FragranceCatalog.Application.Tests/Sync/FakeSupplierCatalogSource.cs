using FragranceCatalog.Application.Sync;

namespace FragranceCatalog.Application.Tests.Sync;

internal sealed class FakeSupplierCatalogSource : ISupplierCatalogSource
{
    private readonly List<string> _productUrls = [];
    private readonly Dictionary<string, SupplierProduct> _products = new(StringComparer.Ordinal);

    public Exception? SitemapFailure { get; set; }

    public CancellationTokenSource? CancelOnProductRead { get; set; }

    public Action? OnProductRead { get; set; }

    public List<SupplierRequestPacing> ReceivedPacings { get; } = [];

    public FakeSupplierCatalogSource Publish(params SupplierProduct[] products)
    {
        foreach (var product in products)
        {
            _productUrls.Add(product.SourceUrl);
            _products[product.SourceUrl] = product;
        }

        return this;
    }

    public FakeSupplierCatalogSource PublishUnreadablePage(string productUrl)
    {
        _productUrls.Add(productUrl);
        return this;
    }

    public Task<IReadOnlyList<string>> GetProductUrlsAsync(SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        ReceivedPacings.Add(pacing);
        return SitemapFailure is null
            ? Task.FromResult<IReadOnlyList<string>>(_productUrls)
            : Task.FromException<IReadOnlyList<string>>(SitemapFailure);
    }

    public Task<SupplierProduct> GetProductAsync(string productUrl, SupplierRequestPacing pacing, CancellationToken cancellationToken)
    {
        ReceivedPacings.Add(pacing);
        OnProductRead?.Invoke();
        CancelOnProductRead?.Cancel();
        cancellationToken.ThrowIfCancellationRequested();

        return _products.TryGetValue(productUrl, out var product)
            ? Task.FromResult(product)
            : Task.FromException<SupplierProduct>(new SupplierCatalogException($"Could not read {productUrl}."));
    }
}

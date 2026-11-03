namespace FragranceCatalog.Application.Sync;

/// <summary>Reads a supplier's online catalog. Implementations wait between requests and give up on slow pages as the pacing says.</summary>
public interface ISupplierCatalogSource
{
    /// <summary>Lists every product page currently published by the supplier, without duplicates.</summary>
    /// <param name="pacing">Pause between requests and timeout per request.</param>
    /// <param name="cancellationToken">Stops the sync.</param>
    /// <exception cref="SupplierCatalogException">The catalog could not be downloaded or read.</exception>
    Task<IReadOnlyList<string>> GetProductUrlsAsync(SupplierRequestPacing pacing, CancellationToken cancellationToken);

    /// <summary>Downloads and reads one product page.</summary>
    /// <param name="productUrl">A URL returned by <see cref="GetProductUrlsAsync"/>.</param>
    /// <param name="pacing">Pause between requests and timeout per request.</param>
    /// <param name="cancellationToken">Stops the sync.</param>
    /// <exception cref="SupplierCatalogException">The page could not be downloaded or does not contain the expected product data.</exception>
    Task<SupplierProduct> GetProductAsync(string productUrl, SupplierRequestPacing pacing, CancellationToken cancellationToken);
}

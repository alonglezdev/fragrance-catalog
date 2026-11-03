using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Read-only pricing data, shared by every feature that shows calculated prices. What it returns is never saved back.</summary>
public interface IPricingReader
{
    /// <summary>Reads the current settings, ranges and rules.</summary>
    /// <param name="cancellationToken">Stops the read.</param>
    Task<PricingConfiguration> GetConfigurationAsync(CancellationToken cancellationToken);

    /// <summary>Reads the current cost of every product that is not discontinued.</summary>
    /// <param name="cancellationToken">Stops the read.</param>
    Task<IReadOnlyList<ProductCost>> GetProductCostsAsync(CancellationToken cancellationToken);
}

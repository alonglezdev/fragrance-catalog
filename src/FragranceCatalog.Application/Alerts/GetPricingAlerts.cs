using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Alerts;

/// <summary>Finds products with a profit below the minimum and products whose cost moved too much in the last sync, up or down.</summary>
/// <param name="reader">Pricing data.</param>
/// <param name="store">Price history.</param>
/// <param name="timeProvider">Instant the prices are calculated for.</param>
public sealed class GetPricingAlerts(IPricingReader reader, IPricingAlertStore store, TimeProvider timeProvider)
{
    private const decimal WholePercent = 100m;
    private const int ChangePercentDecimals = 2;

    /// <summary>Calculates both alert lists. Alerts only inform: they never block a sale.</summary>
    /// <param name="cancellationToken">Stops the read.</param>
    public async Task<PricingAlertsDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var configuration = await reader.GetConfigurationAsync(cancellationToken);
        var products = await reader.GetProductCostsAsync(cancellationToken);
        var costChanges = await store.GetCostChangesOfLastSyncAsync(cancellationToken);

        return new PricingAlertsDto(
            FindLowProfit(products, configuration, timeProvider.GetUtcNow()),
            FindCostChanges(costChanges, configuration.Settings.CostChangeAlertPercent));
    }

    private static List<LowProfitAlertDto> FindLowProfit(IReadOnlyList<ProductCost> products, PricingConfiguration configuration, DateTimeOffset now) =>
        products
            .Select(product => (Product: product, Quote: PriceCalculator.Calculate(product.Cost, product.BrandId, product.ProductId, configuration, now)))
            .Where(priced => priced.Quote.IsLowProfit)
            .Select(priced => new LowProfitAlertDto(priced.Product.ProductId, priced.Product.Name, priced.Product.BrandName, priced.Product.Cost, priced.Quote.SalePrice, priced.Quote.Profit))
            .ToList();

    private static List<CostChangeAlertDto> FindCostChanges(IReadOnlyList<CostChange> costChanges, int alertPercent) =>
        costChanges
            .Where(change => Math.Abs(ChangePercentOf(change)) > alertPercent)
            .Select(change => new CostChangeAlertDto(
                change.ProductId,
                change.Name,
                change.BrandName,
                change.PreviousCost,
                change.CurrentCost,
                Math.Round(ChangePercentOf(change), ChangePercentDecimals, MidpointRounding.AwayFromZero)))
            .ToList();

    private static decimal ChangePercentOf(CostChange change) =>
        (change.CurrentCost - change.PreviousCost) / change.PreviousCost * WholePercent;
}

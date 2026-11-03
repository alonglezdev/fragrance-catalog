namespace FragranceCatalog.Application.Pricing;

/// <summary>A product whose price would change.</summary>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="Cost">Current supplier cost.</param>
/// <param name="CurrentPrice">Sale price today.</param>
/// <param name="ProposedPrice">Sale price with the change.</param>
/// <param name="ProposedProfit">Profit with the change.</param>
/// <param name="IsLowProfit">Whether the change would leave the profit below the minimum.</param>
public sealed record PricingPreviewItemDto(
    Guid ProductId,
    string Name,
    string BrandName,
    decimal Cost,
    decimal CurrentPrice,
    decimal ProposedPrice,
    decimal ProposedProfit,
    bool IsLowProfit);

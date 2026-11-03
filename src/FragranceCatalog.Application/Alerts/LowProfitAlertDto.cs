namespace FragranceCatalog.Application.Alerts;

/// <summary>A product earning less than the configured minimum.</summary>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="Cost">Current supplier cost.</param>
/// <param name="SalePrice">Calculated sale price.</param>
/// <param name="Profit">Sale price minus cost; may be negative.</param>
public sealed record LowProfitAlertDto(Guid ProductId, string Name, string BrandName, decimal Cost, decimal SalePrice, decimal Profit);

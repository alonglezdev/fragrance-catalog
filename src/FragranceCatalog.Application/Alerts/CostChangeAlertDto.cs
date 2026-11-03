namespace FragranceCatalog.Application.Alerts;

/// <summary>A product whose cost moved more than the configured percentage in the last sync.</summary>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="PreviousCost">Cost before that sync.</param>
/// <param name="CurrentCost">Cost after that sync.</param>
/// <param name="ChangePercent">Signed change, to 2 decimals; negative when the cost went down.</param>
public sealed record CostChangeAlertDto(Guid ProductId, string Name, string BrandName, decimal PreviousCost, decimal CurrentCost, decimal ChangePercent);

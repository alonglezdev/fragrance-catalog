namespace FragranceCatalog.Application.Alerts;

/// <summary>A cost that changed in the last successful sync.</summary>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="PreviousCost">Cost before that sync, above zero.</param>
/// <param name="CurrentCost">Cost after that sync.</param>
public sealed record CostChange(Guid ProductId, string Name, string BrandName, decimal PreviousCost, decimal CurrentCost);

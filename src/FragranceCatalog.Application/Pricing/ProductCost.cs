namespace FragranceCatalog.Application.Pricing;

/// <summary>What is needed to price a product still in the supplier catalog.</summary>
/// <param name="ProductId">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandId">Brand of the product.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="Cost">Current supplier cost.</param>
public sealed record ProductCost(Guid ProductId, string Name, Guid BrandId, string BrandName, decimal Cost);

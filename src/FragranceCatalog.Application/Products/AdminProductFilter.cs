namespace FragranceCatalog.Application.Products;

/// <summary>Filters of the admin product list.</summary>
/// <param name="Search">Text to find in the product or brand name, or null for all.</param>
/// <param name="BrandId">Only this brand, or null for all.</param>
/// <param name="IsLowProfitOnly">Only products with a profit below the minimum.</param>
/// <param name="IncludeDiscontinued">Also show products that left the supplier catalog.</param>
public sealed record AdminProductFilter(string? Search = null, Guid? BrandId = null, bool IsLowProfitOnly = false, bool IncludeDiscontinued = false);

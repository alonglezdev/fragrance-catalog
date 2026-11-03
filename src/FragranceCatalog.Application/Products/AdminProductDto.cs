using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Products;

/// <summary>A product as the admin panel lists it, with internal data that must never reach the public catalog.</summary>
/// <param name="Id">Identifier of the product.</param>
/// <param name="Name">Product name.</param>
/// <param name="BrandName">Brand name.</param>
/// <param name="Cost">Current supplier cost.</param>
/// <param name="SalePrice">Calculated sale price.</param>
/// <param name="Profit">Sale price minus cost.</param>
/// <param name="IsLowProfit">Whether the profit is below the minimum.</param>
/// <param name="AppliedRuleType">Kind of rule that produced the price.</param>
/// <param name="AppliedRuleId">Identifier of that rule or range; null for the global percentage.</param>
/// <param name="IsAvailable">Whether the supplier has stock.</param>
/// <param name="IsHidden">Whether the administrator hid it.</param>
/// <param name="IsFeatured">Whether the administrator featured it.</param>
/// <param name="IsDiscontinued">Whether it left the supplier catalog.</param>
public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string BrandName,
    decimal Cost,
    decimal SalePrice,
    decimal Profit,
    bool IsLowProfit,
    AppliedRuleType AppliedRuleType,
    Guid? AppliedRuleId,
    bool IsAvailable,
    bool IsHidden,
    bool IsFeatured,
    bool IsDiscontinued);

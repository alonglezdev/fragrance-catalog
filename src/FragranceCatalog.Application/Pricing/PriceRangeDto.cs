using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>One step of the base pricing table as the admin panel shows it.</summary>
/// <param name="Id">Identifier of the range.</param>
/// <param name="MinCost">Lowest cost covered, inclusive.</param>
/// <param name="MaxCost">First cost not covered, exclusive, or null for no limit.</param>
/// <param name="Action">Markup kind.</param>
/// <param name="Value">Amount in MXN or percentage added to the cost.</param>
public sealed record PriceRangeDto(Guid Id, decimal MinCost, decimal? MaxCost, PriceAction Action, decimal Value)
{
    /// <summary>Copies the range entity.</summary>
    /// <param name="range">The range.</param>
    public static PriceRangeDto From(PriceRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        return new PriceRangeDto(range.Id, range.MinCost, range.MaxCost, range.Action, range.Value);
    }
}

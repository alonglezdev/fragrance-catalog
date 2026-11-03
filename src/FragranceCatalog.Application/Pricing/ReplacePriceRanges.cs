using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Replaces the whole base pricing table, so the panel never leaves it half edited.</summary>
/// <param name="store">Pricing data.</param>
public sealed class ReplacePriceRanges(IPricingStore store)
{
    /// <summary>Validates each range and the table they form, reporting every problem together, and saves them if there are none.</summary>
    /// <param name="ranges">The complete new table, in any order.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<IReadOnlyList<PriceRangeDto>>> ExecuteAsync(IReadOnlyCollection<PriceRangeDefinition> ranges, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        var errors = PriceRangeValidator.ValidateDefinitions(ranges);
        if (errors.Count > 0)
        {
            return Result.Failure<IReadOnlyList<PriceRangeDto>>(PricingErrors.From(errors));
        }

        var newRanges = ranges
            .OrderBy(range => range.MinCost)
            .Select(range => PriceRange.Create(range.MinCost, range.MaxCost, range.Action, range.Value))
            .ToList();
        await store.ReplaceRangesAsync(newRanges, cancellationToken);
        return Result.Success<IReadOnlyList<PriceRangeDto>>(newRanges.Select(PriceRangeDto.From).ToList());
    }
}

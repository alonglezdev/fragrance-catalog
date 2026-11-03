namespace FragranceCatalog.Domain.Pricing;

/// <summary>
/// Checks that a price range table prices every cost exactly once: it starts at 0, has no gaps or overlaps,
/// and only the last range is open-ended.
/// </summary>
public static class PriceRangeValidator
{
    /// <summary>Lists every problem in the table; an empty list means the table can be saved.</summary>
    /// <param name="ranges">The complete table, in any order.</param>
    public static IReadOnlyList<PriceRangeError> Validate(IEnumerable<PriceRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        var ordered = ranges.OrderBy(range => range.MinCost).ThenBy(range => range.MaxCost ?? decimal.MaxValue).ToList();
        if (ordered.Count == 0)
        {
            return [new PriceRangeError(PriceRangeErrorCode.NoRanges, null)];
        }

        var errors = new List<PriceRangeError>();
        if (ordered[0].MinCost != 0m)
        {
            errors.Add(new PriceRangeError(PriceRangeErrorCode.DoesNotStartAtZero, ordered[0].MinCost));
        }

        for (var position = 1; position < ordered.Count; position++)
        {
            if (FindProblemBetween(ordered[position - 1], ordered[position]) is { } error)
            {
                errors.Add(error);
            }
        }

        var last = ordered[^1];
        if (!last.IsOpenEnded)
        {
            errors.Add(new PriceRangeError(PriceRangeErrorCode.LastRangeNotOpen, last.MaxCost));
        }

        return errors;
    }

    private static PriceRangeError? FindProblemBetween(PriceRange lower, PriceRange upper)
    {
        if (lower.MaxCost is not { } lowerEnd)
        {
            return new PriceRangeError(PriceRangeErrorCode.OpenRangeNotLast, lower.MinCost);
        }

        if (upper.MinCost > lowerEnd)
        {
            return new PriceRangeError(PriceRangeErrorCode.Gap, lowerEnd);
        }

        if (upper.MinCost < lowerEnd)
        {
            return new PriceRangeError(PriceRangeErrorCode.Overlap, upper.MinCost);
        }

        return null;
    }
}

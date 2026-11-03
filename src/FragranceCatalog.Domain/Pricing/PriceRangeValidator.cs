namespace FragranceCatalog.Domain.Pricing;

/// <summary>
/// Checks that a price range table prices every cost exactly once: it starts at 0, has no gaps or overlaps,
/// and only the last range is open-ended. A table can be checked before its ranges exist, so every problem is reported at once.
/// </summary>
public static class PriceRangeValidator
{
    /// <summary>Lists every problem in the table; an empty list means the table can be saved.</summary>
    /// <param name="ranges">The complete table, in any order.</param>
    public static IReadOnlyList<PriceRangeError> Validate(IEnumerable<PriceRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        return ValidateTable(ranges.Select(range => new PriceRangeDefinition(range.MinCost, range.MaxCost, range.Action, range.Value)));
    }

    /// <summary>Lists every problem of each proposed range, with its position, and of the table they form; an empty list means the table can be saved.</summary>
    /// <param name="definitions">The complete proposed table, in any order.</param>
    public static IReadOnlyList<PriceRangeError> ValidateDefinitions(IReadOnlyCollection<PriceRangeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var rangeErrors = definitions.SelectMany((definition, position) =>
            PriceRange.Validate(definition.MinCost, definition.MaxCost, definition.Action, definition.Value)
                .Select(error => error with { Position = position }));
        return [.. rangeErrors, .. ValidateTable(definitions)];
    }

    private static IReadOnlyList<PriceRangeError> ValidateTable(IEnumerable<PriceRangeDefinition> definitions)
    {
        var ordered = definitions.OrderBy(range => range.MinCost).ThenBy(range => range.MaxCost ?? decimal.MaxValue).ToList();
        if (ordered.Count == 0)
        {
            return [new PriceRangeError(PriceRangeErrorCode.NoRanges, null)];
        }

        var errors = new List<PriceRangeError>();
        if (ordered[0].MinCost != 0m)
        {
            errors.Add(new PriceRangeError(PriceRangeErrorCode.RangesDoNotStartAtZero, ordered[0].MinCost));
        }

        for (var position = 1; position < ordered.Count; position++)
        {
            if (FindProblemBetween(ordered[position - 1], ordered[position]) is { } error)
            {
                errors.Add(error);
            }
        }

        var last = ordered[^1];
        if (last.MaxCost is not null)
        {
            errors.Add(new PriceRangeError(PriceRangeErrorCode.LastRangeNotOpen, last.MaxCost));
        }

        return errors;
    }

    private static PriceRangeError? FindProblemBetween(PriceRangeDefinition lower, PriceRangeDefinition upper)
    {
        if (lower.MaxCost is not { } lowerEnd)
        {
            return new PriceRangeError(PriceRangeErrorCode.OpenRangeNotLast, lower.MinCost);
        }

        if (upper.MinCost > lowerEnd)
        {
            return new PriceRangeError(PriceRangeErrorCode.RangesHaveGap, lowerEnd);
        }

        if (upper.MinCost < lowerEnd)
        {
            return new PriceRangeError(PriceRangeErrorCode.RangesOverlap, upper.MinCost);
        }

        return null;
    }
}

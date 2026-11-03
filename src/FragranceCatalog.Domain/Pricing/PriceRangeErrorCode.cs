namespace FragranceCatalog.Domain.Pricing;

/// <summary>Reasons a price range is invalid, or a table of ranges cannot price every possible cost exactly once.</summary>
public enum PriceRangeErrorCode
{
    /// <summary>The table is empty.</summary>
    NoRanges,

    /// <summary>The lowest range does not start at 0, so cheap products have no price.</summary>
    RangesDoNotStartAtZero,

    /// <summary>Some costs between two ranges are not covered by any range.</summary>
    RangesHaveGap,

    /// <summary>Some costs are covered by two ranges.</summary>
    RangesOverlap,

    /// <summary>A range without upper limit is followed by another range.</summary>
    OpenRangeNotLast,

    /// <summary>The highest range has an upper limit, so expensive products have no price.</summary>
    LastRangeNotOpen,

    /// <summary>A range starts below zero.</summary>
    NegativeMinCost,

    /// <summary>A range ends at or before its own start.</summary>
    MaxCostNotAboveMinCost,

    /// <summary>A range tries to set a fixed price; only pricing rules may do that.</summary>
    FixedPriceNotAllowedInRange,

    /// <summary>A range uses an action that does not exist.</summary>
    UnknownPriceAction,

    /// <summary>A range adds a negative amount.</summary>
    NegativeAmount,

    /// <summary>A range adds a percentage outside the allowed markup bounds.</summary>
    PercentOutOfRange,
}

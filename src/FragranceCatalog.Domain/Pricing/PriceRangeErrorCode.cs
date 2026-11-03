namespace FragranceCatalog.Domain.Pricing;

/// <summary>Reasons a price range table cannot price every possible cost exactly once.</summary>
public enum PriceRangeErrorCode
{
    /// <summary>The table is empty.</summary>
    NoRanges,

    /// <summary>The lowest range does not start at 0, so cheap products have no price.</summary>
    DoesNotStartAtZero,

    /// <summary>Some costs between two ranges are not covered by any range.</summary>
    Gap,

    /// <summary>Some costs are covered by two ranges.</summary>
    Overlap,

    /// <summary>A range without upper limit is followed by another range.</summary>
    OpenRangeNotLast,

    /// <summary>The highest range has an upper limit, so expensive products have no price.</summary>
    LastRangeNotOpen,
}

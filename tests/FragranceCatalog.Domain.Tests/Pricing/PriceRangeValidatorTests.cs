using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PriceRangeValidatorTests
{
    [Fact]
    public void Validate_InitialRanges_ReturnsNoErrors()
    {
        PriceRange[] ranges = [Range(0m, 1000m), Range(1000m, null)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_RangesOutOfOrder_ReturnsNoErrors()
    {
        PriceRange[] ranges = [Range(2000m, null), Range(0m, 1000m), Range(1000m, 2000m)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_NoRanges_ReturnsNoRangesError()
    {
        var errors = PriceRangeValidator.Validate([]);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.NoRanges, null)]);
    }

    [Fact]
    public void Validate_FirstRangeStartsAboveZero_ReturnsDoesNotStartAtZero()
    {
        PriceRange[] ranges = [Range(100m, 1000m), Range(1000m, null)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.DoesNotStartAtZero, 100m)]);
    }

    [Fact]
    public void Validate_GapBetweenRanges_ReturnsGapWhereCoverageStops()
    {
        PriceRange[] ranges = [Range(0m, 1000m), Range(1200m, null)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.Gap, 1000m)]);
    }

    [Fact]
    public void Validate_OverlappingRanges_ReturnsOverlapWhereSecondRangeStarts()
    {
        PriceRange[] ranges = [Range(0m, 1000m), Range(900m, null)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.Overlap, 900m)]);
    }

    [Fact]
    public void Validate_TwoOpenRanges_ReturnsOpenRangeNotLast()
    {
        PriceRange[] ranges = [Range(0m, 1000m), Range(1000m, null), Range(2000m, null)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.OpenRangeNotLast, 1000m)]);
    }

    [Fact]
    public void Validate_LastRangeHasUpperLimit_ReturnsLastRangeNotOpen()
    {
        PriceRange[] ranges = [Range(0m, 1000m), Range(1000m, 5000m)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.LastRangeNotOpen, 5000m)]);
    }

    [Fact]
    public void Validate_SeveralProblems_ReturnsEveryError()
    {
        PriceRange[] ranges = [Range(100m, 1000m), Range(1200m, 3000m)];

        var errors = PriceRangeValidator.Validate(ranges);

        errors.Select(error => error.Code).ShouldBe(
        [
            PriceRangeErrorCode.DoesNotStartAtZero,
            PriceRangeErrorCode.Gap,
            PriceRangeErrorCode.LastRangeNotOpen,
        ]);
    }

    private static PriceRange Range(decimal minCost, decimal? maxCost) =>
        PriceRange.Create(minCost, maxCost, PriceAction.AddAmount, 200m);
}

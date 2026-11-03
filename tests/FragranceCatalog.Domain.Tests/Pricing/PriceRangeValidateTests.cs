using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PriceRangeValidateTests
{
    [Theory]
    [InlineData(PriceAction.AddAmount, 0)]
    [InlineData(PriceAction.AddPercent, 500)]
    public void Validate_ValidRange_ReturnsNoErrors(PriceAction action, int value)
    {
        var errors = PriceRange.Validate(0m, 1000m, action, value);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_OpenEndedRange_ReturnsNoErrors()
    {
        var errors = PriceRange.Validate(1000m, null, PriceAction.AddAmount, 300m);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_NegativeMinCost_ReturnsItsCode()
    {
        var errors = PriceRange.Validate(-1m, 1000m, PriceAction.AddAmount, 200m);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.NegativeMinCost, -1m)]);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(900)]
    public void Validate_MaxCostNotAboveMinCost_ReturnsItsCode(int maxCost)
    {
        var errors = PriceRange.Validate(1000m, maxCost, PriceAction.AddAmount, 200m);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.MaxCostNotAboveMinCost, maxCost)]);
    }

    [Fact]
    public void Validate_FixedPrice_ReturnsItsCode()
    {
        var errors = PriceRange.Validate(0m, 1000m, PriceAction.FixedPrice, 200m);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.FixedPriceNotAllowedInRange, 200m)]);
    }

    [Fact]
    public void Validate_UnknownAction_ReturnsItsCode()
    {
        var errors = PriceRange.Validate(0m, 1000m, (PriceAction)99, 200m);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.UnknownPriceAction, 200m)]);
    }

    [Fact]
    public void Validate_NegativeAmount_ReturnsItsCode()
    {
        var errors = PriceRange.Validate(0m, 1000m, PriceAction.AddAmount, -1m);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.NegativeAmount, -1m)]);
    }

    [Fact]
    public void Validate_PercentAboveMaximum_ReturnsItsCode()
    {
        var errors = PriceRange.Validate(0m, 1000m, PriceAction.AddPercent, 501m);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.PercentOutOfRange, 501m)]);
    }

    [Fact]
    public void Validate_SeveralProblems_ReturnsEveryError()
    {
        var errors = PriceRange.Validate(-5m, -10m, PriceAction.AddPercent, 501m);

        errors.Select(error => error.Code).ShouldBe(
        [
            PriceRangeErrorCode.NegativeMinCost,
            PriceRangeErrorCode.MaxCostNotAboveMinCost,
            PriceRangeErrorCode.PercentOutOfRange,
        ]);
    }

    [Fact]
    public void ValidateDefinitions_ValidTable_ReturnsNoErrors()
    {
        PriceRangeDefinition[] table = [new(0m, 1000m, PriceAction.AddAmount, 200m), new(1000m, null, PriceAction.AddAmount, 300m)];

        var errors = PriceRangeValidator.ValidateDefinitions(table);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateDefinitions_BadRangeAndBadTable_ReturnsBothKindsOfError()
    {
        PriceRangeDefinition[] table = [new(0m, 1000m, PriceAction.AddPercent, 501m), new(1200m, null, PriceAction.AddAmount, -1m)];

        var errors = PriceRangeValidator.ValidateDefinitions(table);

        errors.Select(error => error.Code).ShouldBe(
        [
            PriceRangeErrorCode.PercentOutOfRange,
            PriceRangeErrorCode.NegativeAmount,
            PriceRangeErrorCode.RangesHaveGap,
        ]);
    }

    [Fact]
    public void ValidateDefinitions_BadRanges_ReportsPositionInSubmittedOrder()
    {
        PriceRangeDefinition[] table =
        [
            new(1000m, null, PriceAction.AddAmount, -1m),
            new(0m, 1000m, PriceAction.FixedPrice, 5m),
        ];

        var errors = PriceRangeValidator.ValidateDefinitions(table);

        errors.ShouldBe(
        [
            new PriceRangeError(PriceRangeErrorCode.NegativeAmount, -1m, 0),
            new PriceRangeError(PriceRangeErrorCode.FixedPriceNotAllowedInRange, 5m, 1),
        ]);
    }

    [Fact]
    public void ValidateDefinitions_SeveralProblemsInOneRange_GivesEachTheSamePosition()
    {
        PriceRangeDefinition[] table =
        [
            new(0m, 1000m, PriceAction.AddAmount, 200m),
            new(1000m, 900m, PriceAction.AddPercent, 501m),
            new(1000m, null, PriceAction.AddAmount, 300m),
        ];

        var errors = PriceRangeValidator.ValidateDefinitions(table);

        errors.Where(error => error.Position == 1).Select(error => error.Code).ShouldBe(
        [
            PriceRangeErrorCode.MaxCostNotAboveMinCost,
            PriceRangeErrorCode.PercentOutOfRange,
        ]);
    }

    [Theory]
    [InlineData(100, 1000, 1000, PriceRangeErrorCode.RangesDoNotStartAtZero)]
    [InlineData(0, 1000, 1200, PriceRangeErrorCode.RangesHaveGap)]
    [InlineData(0, 1000, 900, PriceRangeErrorCode.RangesOverlap)]
    public void ValidateDefinitions_TableProblem_HasNoPosition(int firstMin, int firstMax, int secondMin, PriceRangeErrorCode expectedCode)
    {
        PriceRangeDefinition[] table =
        [
            new(firstMin, firstMax, PriceAction.AddAmount, 200m),
            new(secondMin, null, PriceAction.AddAmount, 300m),
        ];

        var errors = PriceRangeValidator.ValidateDefinitions(table);

        var error = errors.ShouldHaveSingleItem();
        error.Code.ShouldBe(expectedCode);
        error.Position.ShouldBeNull();
    }

    [Fact]
    public void Validate_SingleRange_HasNoPosition()
    {
        var errors = PriceRange.Validate(-1m, null, PriceAction.AddAmount, 0m);

        errors.ShouldHaveSingleItem().Position.ShouldBeNull();
    }

    [Fact]
    public void ValidateDefinitions_EmptyTable_ReturnsNoRanges()
    {
        var errors = PriceRangeValidator.ValidateDefinitions([]);

        errors.ShouldBe([new PriceRangeError(PriceRangeErrorCode.NoRanges, null)]);
    }

    [Fact]
    public void Create_InvalidValues_ThrowsWithTheSameValidation()
    {
        var exception = Should.Throw<ArgumentException>(() => PriceRange.Create(-1m, null, PriceAction.AddAmount, 0m));

        exception.Message.ShouldContain(nameof(PriceRangeErrorCode.NegativeMinCost));
    }
}

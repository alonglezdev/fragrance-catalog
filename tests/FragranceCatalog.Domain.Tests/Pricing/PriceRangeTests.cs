using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PriceRangeTests
{
    [Fact]
    public void Contains_CostEqualToMinCost_ReturnsTrue()
    {
        var range = PriceRange.Create(1000m, 2000m, PriceAction.AddAmount, 300m);

        range.Contains(1000m).ShouldBeTrue();
    }

    [Fact]
    public void Contains_CostEqualToMaxCost_ReturnsFalse()
    {
        var range = PriceRange.Create(1000m, 2000m, PriceAction.AddAmount, 300m);

        range.Contains(2000m).ShouldBeFalse();
    }

    [Fact]
    public void Contains_OpenEndedRangeWithHugeCost_ReturnsTrue()
    {
        var range = PriceRange.Create(1000m, null, PriceAction.AddAmount, 300m);

        range.Contains(1_000_000m).ShouldBeTrue();
    }

    [Fact]
    public void Create_FixedPriceAction_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => PriceRange.Create(0m, null, PriceAction.FixedPrice, 300m));
    }

    [Fact]
    public void Create_MaxCostNotAboveMinCost_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PriceRange.Create(1000m, 1000m, PriceAction.AddAmount, 300m));
    }

    [Fact]
    public void Create_NegativeMinCost_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PriceRange.Create(-1m, 1000m, PriceAction.AddAmount, 300m));
    }

    [Theory]
    [InlineData(PriceAction.AddAmount)]
    [InlineData(PriceAction.AddPercent)]
    public void Create_NegativeValue_ThrowsArgumentOutOfRangeException(PriceAction action)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PriceRange.Create(0m, 1000m, action, -1m));
    }

    [Fact]
    public void Create_PercentAtMarkupMaximum_IsAllowed()
    {
        var range = PriceRange.Create(0m, null, PriceAction.AddPercent, MarkupPercent.Maximum);

        range.Value.ShouldBe(500m);
    }

    [Fact]
    public void Create_PercentAboveMarkupMaximum_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PriceRange.Create(0m, null, PriceAction.AddPercent, 500.01m));
    }

    [Fact]
    public void Create_AmountAboveMarkupMaximum_IsAllowed()
    {
        var range = PriceRange.Create(0m, null, PriceAction.AddAmount, 800m);

        range.Value.ShouldBe(800m);
    }
}

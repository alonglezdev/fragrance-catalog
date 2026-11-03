using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PriceRoundingTests
{
    public static TheoryData<decimal, decimal> UpToNearestTenCases => new()
    {
        { 975m, 980m },
        { 1070m, 1070m },
        { 1197.90m, 1200m },
        { 1070.01m, 1080m },
    };

    public static TheoryData<decimal, decimal> EndsInNineCases => new()
    {
        { 1372m, 1379m },
        { 1389m, 1389m },
        { 1390m, 1399m },
        { 1389.10m, 1399m },
        { 1388.50m, 1389m },
    };

    [Fact]
    public void Apply_None_ReturnsPriceUnchanged()
    {
        var rounded = PriceRounding.Apply(1197.90m, RoundingMode.None);

        rounded.ShouldBe(1197.90m);
    }

    [Theory]
    [MemberData(nameof(UpToNearestTenCases))]
    public void Apply_UpToNearestTen_RoundsUpToNextMultipleOfTen(decimal price, decimal expected)
    {
        var rounded = PriceRounding.Apply(price, RoundingMode.UpToNearestTen);

        rounded.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(EndsInNineCases))]
    public void Apply_EndsInNine_RoundsUpToSmallestWholePriceEndingInNine(decimal price, decimal expected)
    {
        var rounded = PriceRounding.Apply(price, RoundingMode.EndsInNine);

        rounded.ShouldBe(expected);
    }

    [Fact]
    public void Apply_UnknownMode_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PriceRounding.Apply(100m, (RoundingMode)99));
    }
}

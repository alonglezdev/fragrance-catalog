using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PricingSettingsTests
{
    [Fact]
    public void CreateDefault_NewStore_UsesSpecDefaults()
    {
        var settings = PricingSettings.CreateDefault();

        settings.Id.ShouldBe(PricingSettings.SingletonId);
        settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Ranges);
        settings.BasePercent.ShouldBe(10m);
        settings.RoundingMode.ShouldBe(RoundingMode.None);
        settings.MinProfit.ShouldBe(100m);
        settings.CostChangeAlertPercent.ShouldBe(15);
    }

    [Fact]
    public void Update_ValidValues_ReplacesAllSettings()
    {
        var settings = PricingSettings.CreateDefault();

        settings.Update(BaseStrategy.Percent, 25m, RoundingMode.EndsInNine, 0m, 100);

        settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Percent);
        settings.BasePercent.ShouldBe(25m);
        settings.RoundingMode.ShouldBe(RoundingMode.EndsInNine);
        settings.MinProfit.ShouldBe(0m);
        settings.CostChangeAlertPercent.ShouldBe(100);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(500.01)]
    public void Update_BasePercentOutOfRange_ThrowsArgumentOutOfRangeException(double basePercent)
    {
        var settings = PricingSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update(BaseStrategy.Percent, (decimal)basePercent, RoundingMode.None, 100m, 15));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Update_CostChangeAlertPercentOutOfRange_ThrowsArgumentOutOfRangeException(int costChangeAlertPercent)
    {
        var settings = PricingSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update(BaseStrategy.Ranges, 10m, RoundingMode.None, 100m, costChangeAlertPercent));
    }

    [Fact]
    public void Update_NegativeMinProfit_ThrowsArgumentOutOfRangeException()
    {
        var settings = PricingSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update(BaseStrategy.Ranges, 10m, RoundingMode.None, -1m, 15));
    }

    [Fact]
    public void Update_InvalidValue_KeepsPreviousSettings()
    {
        var settings = PricingSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update(BaseStrategy.Percent, 999m, RoundingMode.EndsInNine, 100m, 15));

        settings.ActiveBaseStrategy.ShouldBe(BaseStrategy.Ranges);
        settings.RoundingMode.ShouldBe(RoundingMode.None);
    }
}

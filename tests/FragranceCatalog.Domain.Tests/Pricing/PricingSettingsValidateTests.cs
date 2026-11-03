using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Domain.Tests.Pricing;

public sealed class PricingSettingsValidateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    public void Validate_ValidValues_ReturnsNoErrors(int basePercent)
    {
        var errors = PricingSettings.Validate(BaseStrategy.Percent, basePercent, RoundingMode.EndsInNine, 0m, 1);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_UnknownBaseStrategy_ReturnsItsCode()
    {
        var errors = PricingSettings.Validate((BaseStrategy)99, 10m, RoundingMode.None, 100m, 15);

        errors.ShouldBe([new PricingSettingsError(PricingSettingsErrorCode.UnknownBaseStrategy, "99")]);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("500.01")]
    public void Validate_BasePercentOutOfRange_ReturnsItsCode(string basePercent)
    {
        var errors = PricingSettings.Validate(BaseStrategy.Percent, decimal.Parse(basePercent, System.Globalization.CultureInfo.InvariantCulture), RoundingMode.None, 100m, 15);

        errors.ShouldBe([new PricingSettingsError(PricingSettingsErrorCode.BasePercentOutOfRange, basePercent)]);
    }

    [Fact]
    public void Validate_UnknownRoundingMode_ReturnsItsCode()
    {
        var errors = PricingSettings.Validate(BaseStrategy.Ranges, 10m, (RoundingMode)99, 100m, 15);

        errors.ShouldBe([new PricingSettingsError(PricingSettingsErrorCode.UnknownRoundingMode, "99")]);
    }

    [Fact]
    public void Validate_NegativeMinProfit_ReturnsItsCode()
    {
        var errors = PricingSettings.Validate(BaseStrategy.Ranges, 10m, RoundingMode.None, -1m, 15);

        errors.ShouldBe([new PricingSettingsError(PricingSettingsErrorCode.NegativeMinProfit, "-1")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_CostChangeAlertPercentOutOfRange_ReturnsItsCode(int costChangeAlertPercent)
    {
        var errors = PricingSettings.Validate(BaseStrategy.Ranges, 10m, RoundingMode.None, 100m, costChangeAlertPercent);

        errors.ShouldBe([new PricingSettingsError(PricingSettingsErrorCode.CostChangeAlertPercentOutOfRange, costChangeAlertPercent.ToString())]);
    }

    [Fact]
    public void Validate_SeveralInvalidValues_ReturnsEveryError()
    {
        var errors = PricingSettings.Validate((BaseStrategy)99, 501m, (RoundingMode)99, -1m, 0);

        errors.Select(error => error.Code).ShouldBe(
        [
            PricingSettingsErrorCode.UnknownBaseStrategy,
            PricingSettingsErrorCode.BasePercentOutOfRange,
            PricingSettingsErrorCode.UnknownRoundingMode,
            PricingSettingsErrorCode.NegativeMinProfit,
            PricingSettingsErrorCode.CostChangeAlertPercentOutOfRange,
        ]);
    }

    [Fact]
    public void Update_InvalidValues_ThrowsWithTheSameValidation()
    {
        var settings = PricingSettings.CreateDefault();

        var exception = Should.Throw<ArgumentException>(() => settings.Update(BaseStrategy.Ranges, 501m, RoundingMode.None, 100m, 15));

        exception.Message.ShouldContain(nameof(PricingSettingsErrorCode.BasePercentOutOfRange));
    }
}

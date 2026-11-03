using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Domain.Tests.Sync;

public sealed class SyncSettingsValidateTests
{
    private const string TimeZone = "America/Mexico_City";

    private static readonly TimeOnly _wholeMinute = new(4, 0);

    [Theory]
    [InlineData(1, 250, 5)]
    [InlineData(100, 10000, 120)]
    public void Validate_ValuesAtTheLimits_ReturnsNoErrors(int failureThresholdPercent, int delayBetweenRequestsMs, int requestTimeoutSeconds)
    {
        var errors = SyncSettings.Validate(_wholeMinute, TimeZone, failureThresholdPercent, delayBetweenRequestsMs, requestTimeoutSeconds);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_TimeWithSeconds_ReturnsItsCode()
    {
        var errors = SyncSettings.Validate(new TimeOnly(4, 0, 30), TimeZone, 20, 1000, 30);

        errors.ShouldHaveSingleItem().Code.ShouldBe(SyncSettingsErrorCode.DailySyncTimeNotWholeMinute);
    }

    [Fact]
    public void Validate_UnknownTimeZone_ReturnsItsCodeAndValue()
    {
        var errors = SyncSettings.Validate(_wholeMinute, "Mars/Olympus", 20, 1000, 30);

        errors.ShouldBe([new SyncSettingsError(SyncSettingsErrorCode.UnknownTimeZone, "Mars/Olympus")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_FailureThresholdOutOfRange_ReturnsItsCode(int failureThresholdPercent)
    {
        var errors = SyncSettings.Validate(_wholeMinute, TimeZone, failureThresholdPercent, 1000, 30);

        errors.ShouldBe([new SyncSettingsError(SyncSettingsErrorCode.FailureThresholdPercentOutOfRange, failureThresholdPercent.ToString())]);
    }

    [Theory]
    [InlineData(249)]
    [InlineData(10001)]
    public void Validate_DelayOutOfRange_ReturnsItsCode(int delayBetweenRequestsMs)
    {
        var errors = SyncSettings.Validate(_wholeMinute, TimeZone, 20, delayBetweenRequestsMs, 30);

        errors.ShouldBe([new SyncSettingsError(SyncSettingsErrorCode.DelayBetweenRequestsOutOfRange, delayBetweenRequestsMs.ToString())]);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void Validate_RequestTimeoutOutOfRange_ReturnsItsCode(int requestTimeoutSeconds)
    {
        var errors = SyncSettings.Validate(_wholeMinute, TimeZone, 20, 1000, requestTimeoutSeconds);

        errors.ShouldBe([new SyncSettingsError(SyncSettingsErrorCode.RequestTimeoutOutOfRange, requestTimeoutSeconds.ToString())]);
    }

    [Fact]
    public void Validate_SeveralInvalidValues_ReturnsEveryError()
    {
        var errors = SyncSettings.Validate(new TimeOnly(4, 0, 30), "Mars/Olympus", 0, 0, 0);

        errors.Select(error => error.Code).ShouldBe(
        [
            SyncSettingsErrorCode.DailySyncTimeNotWholeMinute,
            SyncSettingsErrorCode.UnknownTimeZone,
            SyncSettingsErrorCode.FailureThresholdPercentOutOfRange,
            SyncSettingsErrorCode.DelayBetweenRequestsOutOfRange,
            SyncSettingsErrorCode.RequestTimeoutOutOfRange,
        ]);
    }
}

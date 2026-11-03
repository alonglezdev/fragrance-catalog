using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Domain.Tests.Sync;

public sealed class SyncSettingsTests
{
    [Fact]
    public void CreateDefault_NewStore_UsesSpecDefaults()
    {
        var settings = SyncSettings.CreateDefault();

        settings.Id.ShouldBe(SyncSettings.SingletonId);
        settings.IsDailySyncEnabled.ShouldBeTrue();
        settings.DailySyncTime.ShouldBe(new TimeOnly(4, 0));
        settings.TimeZoneId.ShouldBe("America/Mexico_City");
        settings.FailureThresholdPercent.ShouldBe(20);
        settings.DelayBetweenRequestsMs.ShouldBe(1000);
        settings.RequestTimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public void Update_ValidValues_ReplacesAllSettings()
    {
        var settings = SyncSettings.CreateDefault();

        settings.Update(false, new TimeOnly(23, 45), "America/Tijuana", 50, 250, 120);

        settings.IsDailySyncEnabled.ShouldBeFalse();
        settings.DailySyncTime.ShouldBe(new TimeOnly(23, 45));
        settings.TimeZoneId.ShouldBe("America/Tijuana");
        settings.FailureThresholdPercent.ShouldBe(50);
        settings.DelayBetweenRequestsMs.ShouldBe(250);
        settings.RequestTimeoutSeconds.ShouldBe(120);
    }

    [Fact]
    public void Update_TimeWithSeconds_ThrowsArgumentException()
    {
        var settings = SyncSettings.CreateDefault();

        Should.Throw<ArgumentException>(() => settings.Update(true, new TimeOnly(4, 0, 30), "America/Mexico_City", 20, 1000, 30));
    }

    [Theory]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("")]
    public void Update_UnknownTimeZone_ThrowsArgumentException(string timeZoneId)
    {
        var settings = SyncSettings.CreateDefault();

        Should.Throw<ArgumentException>(() => settings.Update(true, new TimeOnly(4, 0), timeZoneId, 20, 1000, 30));
    }

    [Theory]
    [InlineData(0, 1000, 30)]
    [InlineData(101, 1000, 30)]
    [InlineData(20, 249, 30)]
    [InlineData(20, 10001, 30)]
    [InlineData(20, 1000, 4)]
    [InlineData(20, 1000, 121)]
    public void Update_OperationalValueOutOfRange_ThrowsArgumentOutOfRangeException(int failureThresholdPercent, int delayBetweenRequestsMs, int requestTimeoutSeconds)
    {
        var settings = SyncSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update(true, new TimeOnly(4, 0), "America/Mexico_City", failureThresholdPercent, delayBetweenRequestsMs, requestTimeoutSeconds));
    }

    [Fact]
    public void IsKnownIanaTimeZone_IanaId_ReturnsTrue()
    {
        SyncSettings.IsKnownIanaTimeZone("America/Mexico_City").ShouldBeTrue();
    }
}

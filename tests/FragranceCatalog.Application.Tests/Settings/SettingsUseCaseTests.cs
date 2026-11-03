using FragranceCatalog.Application.Settings;
using FragranceCatalog.Domain.Store;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Tests.Settings;

public sealed class SettingsUseCaseTests
{
    private const string TimeZone = "America/Tijuana";

    private readonly FakeSettingsStore _store = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetSyncSettings_StoredSettings_ReturnsThem()
    {
        var settings = await new GetSyncSettings(_store).ExecuteAsync(Token);

        settings.FailureThresholdPercent.ShouldBe(SyncSettings.DefaultFailureThresholdPercent);
        settings.TimeZoneId.ShouldBe(SyncSettings.DefaultTimeZoneId);
    }

    [Fact]
    public async Task UpdateSyncSettings_ValidValues_SavesAndThenReschedules()
    {
        var useCase = new UpdateSyncSettings(_store, _store);

        var result = await useCase.ExecuteAsync(new SyncSettingsValues(false, new TimeOnly(23, 45), TimeZone, 50, 250, 120), Token);

        result.Value.IsDailySyncEnabled.ShouldBeFalse();
        _store.Sync.TimeZoneId.ShouldBe(TimeZone);
        _store.ScheduledSettings.ShouldBe([_store.Sync]);
        _store.SaveCountWhenScheduled.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateSyncSettings_SeveralInvalidValues_ReturnsAllCodesAndDoesNotReschedule()
    {
        var useCase = new UpdateSyncSettings(_store, _store);

        var result = await useCase.ExecuteAsync(new SyncSettingsValues(true, new TimeOnly(4, 0, 30), "Mars/Olympus", 101, 1000, 30), Token);

        result.Errors.Select(error => error.Code).ShouldBe(["DailySyncTimeNotWholeMinute", "UnknownTimeZone", "FailureThresholdPercentOutOfRange"]);
        _store.Sync.FailureThresholdPercent.ShouldBe(SyncSettings.DefaultFailureThresholdPercent);
        _store.SaveCount.ShouldBe(0);
        _store.ScheduledSettings.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStoreSettings_StoredSettings_ReturnsThem()
    {
        var settings = await new GetStoreSettings(_store).ExecuteAsync(Token);

        settings.StoreName.ShouldBe(StoreSettings.DefaultStoreName);
    }

    [Fact]
    public async Task UpdateStoreSettings_FormattedNumber_SavesItNormalized()
    {
        var useCase = new UpdateStoreSettings(_store);

        var result = await useCase.ExecuteAsync(new StoreSettingsValues("  Mi Tienda ", "+52 (33) 1234-5678", "Quiero {product}"), Token);

        result.Value.StoreName.ShouldBe("Mi Tienda");
        result.Value.WhatsAppNumber.ShouldBe("523312345678");
        _store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateStoreSettings_SeveralInvalidValues_ReturnsAllCodesAndTouchesNothing()
    {
        var useCase = new UpdateStoreSettings(_store);

        var result = await useCase.ExecuteAsync(new StoreSettingsValues("", "abc", "Hola {name}"), Token);

        result.Errors.Select(error => error.Code).ShouldBe(["StoreNameRequired", "InvalidWhatsAppNumber", "UnknownPlaceholder"]);
        result.Errors[2].Value.ShouldBe("{name}");
        _store.Store.StoreName.ShouldBe(StoreSettings.DefaultStoreName);
        _store.SaveCount.ShouldBe(0);
    }
}

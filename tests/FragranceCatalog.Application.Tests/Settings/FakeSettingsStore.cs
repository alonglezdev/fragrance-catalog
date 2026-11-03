using FragranceCatalog.Application.Settings;
using FragranceCatalog.Domain.Store;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Tests.Settings;

internal sealed class FakeSettingsStore : ISettingsStore, ISyncScheduler
{
    public SyncSettings Sync { get; } = SyncSettings.CreateDefault();

    public StoreSettings Store { get; } = StoreSettings.CreateDefault();

    public int SaveCount { get; private set; }

    public List<SyncSettings> ScheduledSettings { get; } = [];

    public int SaveCountWhenScheduled { get; private set; }

    public Task<SyncSettings> GetSyncSettingsAsync(CancellationToken cancellationToken) => Task.FromResult(Sync);

    public Task<StoreSettings> GetStoreSettingsAsync(CancellationToken cancellationToken) => Task.FromResult(Store);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task ApplyScheduleAsync(SyncSettings settings, CancellationToken cancellationToken)
    {
        ScheduledSettings.Add(settings);
        SaveCountWhenScheduled = SaveCount;
        return Task.CompletedTask;
    }
}

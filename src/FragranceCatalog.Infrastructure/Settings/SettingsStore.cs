using FragranceCatalog.Application.Settings;
using FragranceCatalog.Domain.Store;
using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Settings;

/// <summary>Reads and changes the settings rows with EF Core.</summary>
/// <param name="dbContext">Unit of work of the request.</param>
internal sealed class SettingsStore(FragranceCatalogDbContext dbContext) : ISettingsStore
{
    /// <inheritdoc />
    public Task<SyncSettings> GetSyncSettingsAsync(CancellationToken cancellationToken) =>
        dbContext.SyncSettings.SingleAsync(cancellationToken);

    /// <inheritdoc />
    public Task<StoreSettings> GetStoreSettingsAsync(CancellationToken cancellationToken) =>
        dbContext.StoreSettings.SingleAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}

using FragranceCatalog.Domain.Store;
using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Settings;

/// <summary>The settings rows the administrator reads and changes.</summary>
public interface ISettingsStore
{
    /// <summary>Loads the sync settings row.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<SyncSettings> GetSyncSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Loads the store settings row.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<StoreSettings> GetStoreSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Saves the changes made to the loaded rows.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

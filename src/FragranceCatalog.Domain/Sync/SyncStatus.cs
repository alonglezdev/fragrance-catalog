namespace FragranceCatalog.Domain.Sync;

/// <summary>Outcome of a catalog sync.</summary>
public enum SyncStatus
{
    /// <summary>Still reading the supplier catalog or applying changes.</summary>
    Running,

    /// <summary>Changes were applied to the catalog.</summary>
    Succeeded,

    /// <summary>Too many products failed, the sync was cancelled or interrupted, or an unexpected error occurred; the catalog was left untouched.</summary>
    Failed,
}

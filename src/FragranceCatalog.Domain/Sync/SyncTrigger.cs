namespace FragranceCatalog.Domain.Sync;

/// <summary>What started a catalog sync.</summary>
public enum SyncTrigger
{
    /// <summary>The daily job at the configured time.</summary>
    Scheduled,

    /// <summary>An administrator from the panel.</summary>
    Manual,
}

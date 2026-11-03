using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Sync;

/// <summary>
/// The catalog data the supplier sync reads and writes, shaped for that use case only.
/// Other features define their own small interfaces instead of sharing this one.
/// A run is one row from start to end: it is inserted as running and updated when it finishes; its errors are stored only then.
/// </summary>
public interface ICatalogSyncStore
{
    /// <summary>Finds runs still marked as running. When a new sync starts they can only be leftovers of a sync that was interrupted, for example by a restart.</summary>
    /// <param name="cancellationToken">Stops the sync.</param>
    Task<IReadOnlyList<SyncRun>> GetRunningRunsAsync(CancellationToken cancellationToken);

    /// <summary>Stores a run that has just started, so it is visible while the sync works.</summary>
    /// <param name="run">The running run.</param>
    /// <param name="cancellationToken">Stops the sync.</param>
    Task SaveStartedRunAsync(SyncRun run, CancellationToken cancellationToken);

    /// <summary>Reads the settings once when a sync starts, so a running sync keeps the values it started with.</summary>
    /// <param name="cancellationToken">Stops the sync.</param>
    Task<SyncSettings> GetSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Reads what the supplier data is compared against, after the slow supplier reading has finished.</summary>
    /// <param name="cancellationToken">Stops the sync.</param>
    Task<CatalogSnapshot> GetCatalogSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>Applies every change of the plan and updates the run in one transaction: either all of it is stored or nothing is, and the row stays as running.</summary>
    /// <param name="plan">Changes to apply.</param>
    /// <param name="run">The succeeded run, already stored as running.</param>
    /// <param name="cancellationToken">Stops the sync.</param>
    Task ApplyPlanAsync(CatalogSyncPlan plan, SyncRun run, CancellationToken cancellationToken);

    /// <summary>Updates a failed run on its own, discarding anything left pending by an earlier failed <see cref="ApplyPlanAsync"/>.</summary>
    /// <param name="run">The failed run, already stored as running.</param>
    /// <param name="cancellationToken">Stops the save; a sync being cancelled passes <see cref="CancellationToken.None"/> so its run is still recorded.</param>
    Task SaveFailedRunAsync(SyncRun run, CancellationToken cancellationToken);
}

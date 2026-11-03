using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Settings;

/// <summary>Keeps the daily sync job in line with the settings, so a change applies without restarting the API.</summary>
public interface ISyncScheduler
{
    /// <summary>Reschedules the daily job with the new time and zone, or removes it if the daily sync is off. A sync already running keeps its values.</summary>
    /// <param name="settings">The settings just saved.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task ApplyScheduleAsync(SyncSettings settings, CancellationToken cancellationToken);
}

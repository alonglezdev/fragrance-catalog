using FragranceCatalog.Domain.Sync;

namespace FragranceCatalog.Application.Sync;

/// <summary>How politely a sync talks to the supplier site; fixed when the sync starts so a running sync is not affected by new settings.</summary>
/// <param name="DelayBetweenRequests">Minimum time between the start of two consecutive requests.</param>
/// <param name="RequestTimeout">Maximum time to wait for one page, retries included, before counting it as failed.</param>
public sealed record SupplierRequestPacing(TimeSpan DelayBetweenRequests, TimeSpan RequestTimeout)
{
    /// <summary>Takes the pause and timeout configured by the administrator.</summary>
    /// <param name="settings">Sync settings read at the start of the sync.</param>
    public static SupplierRequestPacing From(SyncSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SupplierRequestPacing(
            TimeSpan.FromMilliseconds(settings.DelayBetweenRequestsMs),
            TimeSpan.FromSeconds(settings.RequestTimeoutSeconds));
    }
}

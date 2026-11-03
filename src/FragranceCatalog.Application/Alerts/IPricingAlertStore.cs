namespace FragranceCatalog.Application.Alerts;

/// <summary>The history data that pricing alerts need.</summary>
public interface IPricingAlertStore
{
    /// <summary>Reads the products whose price was recorded by the last successful sync and had an earlier price. Failed syncs change nothing, so they do not count.</summary>
    /// <param name="cancellationToken">Stops the read.</param>
    Task<IReadOnlyList<CostChange>> GetCostChangesOfLastSyncAsync(CancellationToken cancellationToken);
}

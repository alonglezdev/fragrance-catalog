using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Sync;

/// <summary>
/// One execution of the catalog sync, kept as history. It starts as <see cref="SyncStatus.Running"/> and ends exactly once,
/// either succeeded with change counts or failed; errors per page are recorded in both cases.
/// The only way back is <see cref="ReopenAfterFailedSave"/>, when the end of the run could not be stored.
/// </summary>
public sealed class SyncRun
{
    private readonly List<SyncRunError> _errors = [];

    private SyncRun(Guid id, DateTimeOffset startedAt, SyncTrigger trigger)
    {
        Id = id;
        StartedAt = startedAt;
        Trigger = trigger;
        Status = SyncStatus.Running;
    }

    /// <summary>Identifier of the run.</summary>
    public Guid Id { get; private set; }

    /// <summary>UTC instant the run started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>UTC instant the run ended, or null while it is running.</summary>
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>What started the run.</summary>
    public SyncTrigger Trigger { get; private set; }

    /// <summary>Current outcome.</summary>
    public SyncStatus Status { get; private set; }

    /// <summary>Products imported for the first time; zero unless the run succeeded.</summary>
    public int CreatedCount { get; private set; }

    /// <summary>Products whose supplier price changed; zero unless the run succeeded.</summary>
    public int PriceChangedCount { get; private set; }

    /// <summary>Products marked as discontinued; zero unless the run succeeded.</summary>
    public int DiscontinuedCount { get; private set; }

    /// <summary>Discontinued products that came back; zero unless the run succeeded.</summary>
    public int ReactivatedCount { get; private set; }

    /// <summary>Supplier pages that could not be read or parsed.</summary>
    public int ErrorCount { get; private set; }

    /// <summary>Failed pages with the reason for each.</summary>
    public IReadOnlyList<SyncRunError> Errors => _errors;

    /// <summary>Whether the run has already succeeded or failed.</summary>
    public bool IsFinished => Status != SyncStatus.Running;

    /// <summary>Opens a new run in the <see cref="SyncStatus.Running"/> state.</summary>
    /// <param name="trigger">What started the run.</param>
    /// <param name="startedAt">Start instant.</param>
    public static SyncRun Start(SyncTrigger trigger, DateTimeOffset startedAt)
    {
        Guard.ThrowIfUndefined(trigger);

        return new SyncRun(Guid.CreateVersion7(), startedAt.ToUniversalTime(), trigger);
    }

    /// <summary>Records a supplier page that failed so the run can continue with the rest.</summary>
    /// <param name="url">Supplier page that failed.</param>
    /// <param name="message">Why it failed.</param>
    public void RecordError(string url, string message)
    {
        EnsureRunning();
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _errors.Add(SyncRunError.Create(Id, url, message));
        ErrorCount = _errors.Count;
    }

    /// <summary>Closes the run after its changes were applied to the catalog.</summary>
    /// <param name="counts">How many products each kind of change touched.</param>
    /// <param name="finishedAt">End instant, not before the start.</param>
    public void Complete(SyncChangeCounts counts, DateTimeOffset finishedAt)
    {
        EnsureRunning();
        ArgumentNullException.ThrowIfNull(counts);
        ArgumentOutOfRangeException.ThrowIfNegative(counts.Created, nameof(counts));
        ArgumentOutOfRangeException.ThrowIfNegative(counts.PriceChanged, nameof(counts));
        ArgumentOutOfRangeException.ThrowIfNegative(counts.Discontinued, nameof(counts));
        ArgumentOutOfRangeException.ThrowIfNegative(counts.Reactivated, nameof(counts));

        CreatedCount = counts.Created;
        PriceChangedCount = counts.PriceChanged;
        DiscontinuedCount = counts.Discontinued;
        ReactivatedCount = counts.Reactivated;
        Finish(SyncStatus.Succeeded, finishedAt);
    }

    /// <summary>Closes the run without applying any change to the catalog.</summary>
    /// <param name="finishedAt">End instant, not before the start.</param>
    public void Fail(DateTimeOffset finishedAt)
    {
        EnsureRunning();

        Finish(SyncStatus.Failed, finishedAt);
    }

    /// <summary>
    /// Takes back a success that could not be stored together with its catalog changes, so the run can end as failed:
    /// those changes were rolled back, so the counts no longer describe anything that happened.
    /// </summary>
    public void ReopenAfterFailedSave()
    {
        if (Status != SyncStatus.Succeeded)
        {
            throw new InvalidOperationException($"Sync run {Id} is {Status}; only a succeeded run can be reopened.");
        }

        CreatedCount = 0;
        PriceChangedCount = 0;
        DiscontinuedCount = 0;
        ReactivatedCount = 0;
        Status = SyncStatus.Running;
        FinishedAt = null;
    }

    private void Finish(SyncStatus status, DateTimeOffset finishedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(finishedAt, StartedAt);

        Status = status;
        FinishedAt = finishedAt.ToUniversalTime();
    }

    private void EnsureRunning()
    {
        if (IsFinished)
        {
            throw new InvalidOperationException($"Sync run {Id} already finished as {Status}.");
        }
    }
}

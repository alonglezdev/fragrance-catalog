namespace FragranceCatalog.Domain.Sync;

/// <summary>A supplier page that could not be read or parsed during a sync.</summary>
public sealed class SyncRunError
{
    private SyncRunError(Guid id, Guid syncRunId, string url, string message)
    {
        Id = id;
        SyncRunId = syncRunId;
        Url = url;
        Message = message;
    }

    /// <summary>Identifier of the error entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Sync that hit the error.</summary>
    public Guid SyncRunId { get; private set; }

    /// <summary>Supplier page that failed.</summary>
    public string Url { get; private set; }

    /// <summary>Why it failed, for the developer reviewing the sync history.</summary>
    public string Message { get; private set; }

    internal static SyncRunError Create(Guid syncRunId, string url, string message) =>
        new(Guid.CreateVersion7(), syncRunId, url, message);
}

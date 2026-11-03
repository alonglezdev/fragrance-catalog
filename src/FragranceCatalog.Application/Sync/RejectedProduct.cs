namespace FragranceCatalog.Application.Sync;

/// <summary>A supplier product the domain does not accept. It counts as a failed page, so many of them cancel the sync like unreadable pages do.</summary>
/// <param name="SourceUrl">Supplier page the product was read from.</param>
/// <param name="Reason">Why the domain rejected it.</param>
public sealed record RejectedProduct(string SourceUrl, string Reason);

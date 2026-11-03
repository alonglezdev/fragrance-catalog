namespace FragranceCatalog.Domain.Sync;

/// <summary>How many products each kind of change touched in a successful sync.</summary>
/// <param name="Created">Products imported for the first time.</param>
/// <param name="PriceChanged">Existing products whose supplier price changed.</param>
/// <param name="Discontinued">Products no longer in the supplier catalog.</param>
/// <param name="Reactivated">Discontinued products that came back.</param>
public sealed record SyncChangeCounts(int Created, int PriceChanged, int Discontinued, int Reactivated);

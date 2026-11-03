namespace FragranceCatalog.Infrastructure.Persistence;

/// <summary>
/// Fixed identifiers of the rows created by the initial migration, so other components can find them
/// without searching by name and migrations stay stable.
/// </summary>
public static class SeedData
{
    /// <summary>Magna Perfumes, the only supplier at launch.</summary>
    public static readonly Guid MagnaSupplierId = new("0193a6f0-0000-7000-8000-000000000001");

    /// <summary>Initial range [0, 1000) +$200.</summary>
    public static readonly Guid FirstInitialPriceRangeId = new("0193a6f0-0000-7000-8000-000000000101");

    /// <summary>Initial range [1000, ∞) +$300.</summary>
    public static readonly Guid SecondInitialPriceRangeId = new("0193a6f0-0000-7000-8000-000000000102");
}

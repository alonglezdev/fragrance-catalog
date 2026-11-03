namespace FragranceCatalog.Domain.Pricing;

/// <summary>Cosmetic adjustment applied to the calculated price so it looks like a retail price.</summary>
public enum RoundingMode
{
    /// <summary>The calculated price is shown as is.</summary>
    None,

    /// <summary>Round up to the next multiple of 10 (975 becomes 980).</summary>
    UpToNearestTen,

    /// <summary>Round up to the next whole amount ending in 9 (1372 becomes 1379).</summary>
    EndsInNine,
}

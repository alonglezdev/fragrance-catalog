namespace FragranceCatalog.Domain.Pricing;

/// <summary>
/// One step of the base pricing table: products whose cost falls in [MinCost, MaxCost) get this markup.
/// The whole table is checked with <see cref="PriceRangeValidator"/>.
/// </summary>
public sealed class PriceRange
{
    private PriceRange(Guid id, decimal minCost, decimal? maxCost, PriceAction action, decimal value)
    {
        Id = id;
        MinCost = minCost;
        MaxCost = maxCost;
        Action = action;
        Value = value;
    }

    /// <summary>Identifier of the range.</summary>
    public Guid Id { get; private set; }

    /// <summary>Lowest cost covered, inclusive.</summary>
    public decimal MinCost { get; private set; }

    /// <summary>First cost no longer covered, exclusive; null means the range has no upper limit.</summary>
    public decimal? MaxCost { get; private set; }

    /// <summary>Markup kind; ranges only add an amount or a percentage.</summary>
    public PriceAction Action { get; private set; }

    /// <summary>Amount in MXN or percentage added to the cost.</summary>
    public decimal Value { get; private set; }

    /// <summary>The range covers every cost from its minimum upwards.</summary>
    public bool IsOpenEnded => MaxCost is null;

    /// <summary>
    /// Creates a range, rejecting negative bounds, an upper bound not above the lower one, fixed prices,
    /// negative amounts and percentages outside <see cref="MarkupPercent"/>.
    /// </summary>
    /// <param name="minCost">Lowest cost covered, inclusive.</param>
    /// <param name="maxCost">First cost not covered, exclusive, or null for no limit.</param>
    /// <param name="action"><see cref="PriceAction.AddAmount"/> or <see cref="PriceAction.AddPercent"/>.</param>
    /// <param name="value">Amount to add, zero or more, or percentage between <see cref="MarkupPercent.Minimum"/> and <see cref="MarkupPercent.Maximum"/>.</param>
    public static PriceRange Create(decimal minCost, decimal? maxCost, PriceAction action, decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minCost);
        if (maxCost is { } upperBound)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(upperBound, minCost, nameof(maxCost));
        }

        if (action == PriceAction.FixedPrice)
        {
            throw new ArgumentException("Price ranges can only add an amount or a percentage.", nameof(action));
        }

        PriceActionValue.ThrowIfInvalid(action, value);

        return new PriceRange(Guid.CreateVersion7(), minCost, maxCost, action, value);
    }

    /// <summary>Whether a supplier cost falls inside this range.</summary>
    /// <param name="cost">Supplier cost in MXN.</param>
    public bool Contains(decimal cost) => cost >= MinCost && (MaxCost is null || cost < MaxCost);
}

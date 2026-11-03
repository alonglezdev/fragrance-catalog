namespace FragranceCatalog.Domain.Pricing;

/// <summary>Turns a calculated price into a retail-looking price. Every mode only rounds up, so profit never shrinks.</summary>
public static class PriceRounding
{
    private const decimal RoundingStep = 10m;
    private const decimal TargetLastDigit = 9m;

    /// <summary>Rounds a price according to the configured mode.</summary>
    /// <param name="price">Calculated price, zero or more.</param>
    /// <param name="mode">Rounding configured by the administrator.</param>
    public static decimal Apply(decimal price, RoundingMode mode) => mode switch
    {
        RoundingMode.None => price,
        RoundingMode.UpToNearestTen => RoundUpToNearestTen(price),
        RoundingMode.EndsInNine => RoundUpToEndInNine(price),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown rounding mode."),
    };

    private static decimal RoundUpToNearestTen(decimal price) => Math.Ceiling(price / RoundingStep) * RoundingStep;

    private static decimal RoundUpToEndInNine(decimal price)
    {
        var wholePrice = Math.Ceiling(price);
        var lastDigit = wholePrice % RoundingStep;
        return wholePrice + (TargetLastDigit - lastDigit);
    }
}

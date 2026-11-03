namespace FragranceCatalog.Domain.Pricing;

/// <summary>
/// Allowed bounds for every markup percentage: the global base percentage, percentage price ranges and percentage pricing rules.
/// Sharing them keeps an exception from allowing a markup the base configuration would reject.
/// </summary>
public static class MarkupPercent
{
    /// <summary>Lowest markup percentage allowed.</summary>
    public const decimal Minimum = 0m;

    /// <summary>Highest markup percentage allowed.</summary>
    public const decimal Maximum = 500m;
}

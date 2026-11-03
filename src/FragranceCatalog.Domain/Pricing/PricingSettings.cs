using System.Globalization;
using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Pricing;

/// <summary>Store-wide pricing configuration edited from the admin panel. There is exactly one row.</summary>
public sealed class PricingSettings
{
    /// <summary>Identifier of the single settings row.</summary>
    public const int SingletonId = 1;

    /// <summary>Smallest cost change, in percent, that may trigger a cost alert.</summary>
    public const int MinCostChangeAlertPercent = 1;

    /// <summary>Largest cost change, in percent, that may trigger a cost alert.</summary>
    public const int MaxCostChangeAlertPercent = 100;

    /// <summary>Global markup percentage used until the administrator changes it.</summary>
    public const decimal DefaultBasePercent = 10m;

    /// <summary>Minimum profit in MXN below which a product is flagged, until the administrator changes it.</summary>
    public const decimal DefaultMinProfit = 100m;

    /// <summary>Cost change percentage that triggers an alert, until the administrator changes it.</summary>
    public const int DefaultCostChangeAlertPercent = 15;

    private PricingSettings(int id) => Id = id;

    /// <summary>Always <see cref="SingletonId"/>.</summary>
    public int Id { get; private set; }

    /// <summary>Strategy used when no brand or product rule applies. Switching strategy keeps the ranges.</summary>
    public BaseStrategy ActiveBaseStrategy { get; private set; }

    /// <summary>Markup percentage applied to every product when the base strategy is <see cref="BaseStrategy.Percent"/>.</summary>
    public decimal BasePercent { get; private set; }

    /// <summary>Rounding applied to every calculated price except fixed prices.</summary>
    public RoundingMode RoundingMode { get; private set; }

    /// <summary>Profit in MXN below which a product is flagged as low profit.</summary>
    public decimal MinProfit { get; private set; }

    /// <summary>Change in supplier cost, in percent and in either direction, that raises an alert after a sync.</summary>
    public int CostChangeAlertPercent { get; private set; }

    /// <summary>Settings for a new store: price ranges, 10% global markup, no rounding, $100 minimum profit and 15% cost alert.</summary>
    public static PricingSettings CreateDefault()
    {
        var settings = new PricingSettings(SingletonId);
        settings.Update(BaseStrategy.Ranges, DefaultBasePercent, RoundingMode.None, DefaultMinProfit, DefaultCostChangeAlertPercent);
        return settings;
    }

    /// <summary>Lists every problem of the proposed settings; an empty list means <see cref="Update"/> will accept them.</summary>
    /// <param name="activeBaseStrategy">Strategy used when no rule applies.</param>
    /// <param name="basePercent">Global markup percentage.</param>
    /// <param name="roundingMode">Rounding applied to calculated prices.</param>
    /// <param name="minProfit">Low profit threshold in MXN.</param>
    /// <param name="costChangeAlertPercent">Cost alert threshold.</param>
    public static IReadOnlyList<PricingSettingsError> Validate(BaseStrategy activeBaseStrategy, decimal basePercent, RoundingMode roundingMode, decimal minProfit, int costChangeAlertPercent)
    {
        var errors = new List<PricingSettingsError>();
        if (!Enum.IsDefined(activeBaseStrategy))
        {
            errors.Add(new PricingSettingsError(PricingSettingsErrorCode.UnknownBaseStrategy, activeBaseStrategy.ToString()));
        }

        if (basePercent is < MarkupPercent.Minimum or > MarkupPercent.Maximum)
        {
            errors.Add(new PricingSettingsError(PricingSettingsErrorCode.BasePercentOutOfRange, basePercent.ToString(CultureInfo.InvariantCulture)));
        }

        if (!Enum.IsDefined(roundingMode))
        {
            errors.Add(new PricingSettingsError(PricingSettingsErrorCode.UnknownRoundingMode, roundingMode.ToString()));
        }

        if (minProfit < 0m)
        {
            errors.Add(new PricingSettingsError(PricingSettingsErrorCode.NegativeMinProfit, minProfit.ToString(CultureInfo.InvariantCulture)));
        }

        if (costChangeAlertPercent is < MinCostChangeAlertPercent or > MaxCostChangeAlertPercent)
        {
            errors.Add(new PricingSettingsError(PricingSettingsErrorCode.CostChangeAlertPercentOutOfRange, costChangeAlertPercent.ToString(CultureInfo.InvariantCulture)));
        }

        return errors;
    }

    /// <summary>Replaces every pricing setting at once, rejecting values outside the allowed ranges.</summary>
    /// <param name="activeBaseStrategy">Strategy used when no rule applies.</param>
    /// <param name="basePercent">Global markup percentage, between <see cref="MarkupPercent.Minimum"/> and <see cref="MarkupPercent.Maximum"/>.</param>
    /// <param name="roundingMode">Rounding applied to calculated prices.</param>
    /// <param name="minProfit">Low profit threshold in MXN, zero or more.</param>
    /// <param name="costChangeAlertPercent">Cost alert threshold, between <see cref="MinCostChangeAlertPercent"/> and <see cref="MaxCostChangeAlertPercent"/>.</param>
    public void Update(BaseStrategy activeBaseStrategy, decimal basePercent, RoundingMode roundingMode, decimal minProfit, int costChangeAlertPercent)
    {
        Guard.ThrowIfInvalid(Validate(activeBaseStrategy, basePercent, roundingMode, minProfit, costChangeAlertPercent));

        ActiveBaseStrategy = activeBaseStrategy;
        BasePercent = basePercent;
        RoundingMode = roundingMode;
        MinProfit = minProfit;
        CostChangeAlertPercent = costChangeAlertPercent;
    }
}

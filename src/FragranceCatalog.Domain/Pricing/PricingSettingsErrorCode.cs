namespace FragranceCatalog.Domain.Pricing;

/// <summary>Reasons the pricing settings cannot be saved.</summary>
public enum PricingSettingsErrorCode
{
    /// <summary>The base strategy does not exist.</summary>
    UnknownBaseStrategy,

    /// <summary>The global percentage is outside the allowed markup bounds.</summary>
    BasePercentOutOfRange,

    /// <summary>The rounding mode does not exist.</summary>
    UnknownRoundingMode,

    /// <summary>The minimum profit is below zero.</summary>
    NegativeMinProfit,

    /// <summary>The cost alert percentage is outside its allowed range.</summary>
    CostChangeAlertPercentOutOfRange,
}

namespace FragranceCatalog.Domain.Pricing;

internal static class PriceActionValue
{
    public static PriceActionValueProblem? FindProblem(PriceAction action, decimal value) => action switch
    {
        PriceAction.AddAmount when value < 0m => PriceActionValueProblem.NegativeAmount,
        PriceAction.AddPercent when value is < MarkupPercent.Minimum or > MarkupPercent.Maximum => PriceActionValueProblem.PercentOutOfRange,
        PriceAction.FixedPrice when value <= 0m => PriceActionValueProblem.FixedPriceNotAboveZero,
        PriceAction.AddAmount or PriceAction.AddPercent or PriceAction.FixedPrice => null,
        _ => PriceActionValueProblem.UnknownAction,
    };
}

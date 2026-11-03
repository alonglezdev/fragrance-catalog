namespace FragranceCatalog.Domain.Pricing;

internal enum PriceActionValueProblem
{
    UnknownAction,
    NegativeAmount,
    PercentOutOfRange,
    FixedPriceNotAboveZero,
}

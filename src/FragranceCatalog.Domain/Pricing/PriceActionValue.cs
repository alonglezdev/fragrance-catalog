using System.Runtime.CompilerServices;
using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Pricing;

internal static class PriceActionValue
{
    public static void ThrowIfInvalid(PriceAction action, decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        Guard.ThrowIfUndefined(action);

        switch (action)
        {
            case PriceAction.AddAmount:
                ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
                break;
            case PriceAction.AddPercent:
                Guard.ThrowIfOutOfRange(value, MarkupPercent.Minimum, MarkupPercent.Maximum, paramName);
                break;
            case PriceAction.FixedPrice:
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
                break;
        }
    }
}

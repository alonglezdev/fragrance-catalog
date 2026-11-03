using System.Runtime.CompilerServices;

namespace FragranceCatalog.Domain.Common;

internal static class Guard
{
    public static void ThrowIfUndefined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Unknown {typeof(TEnum).Name} value.");
        }
    }

    public static void ThrowIfInvalid<TError>(IReadOnlyCollection<TError> errors, [CallerMemberName] string? operation = null)
    {
        if (errors.Count > 0)
        {
            throw new ArgumentException($"{operation} received invalid values: {string.Join("; ", errors)}");
        }
    }

    public static void ThrowIfEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An identifier is required.", paramName);
        }
    }

    public static void ThrowIfNotAbsoluteUrl(string value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"'{value}' is not an absolute URL.", paramName);
        }
    }
}

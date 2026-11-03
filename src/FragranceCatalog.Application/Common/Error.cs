using System.Globalization;

namespace FragranceCatalog.Application.Common;

/// <summary>An expected business error, ready to be shown in the admin panel.</summary>
/// <param name="Code">Stable name of the problem, such as "RangesHaveGap" or "ProductNotFound".</param>
/// <param name="Kind">Family of the error.</param>
/// <param name="Value">The value that caused it, when there is one.</param>
/// <param name="Position">Zero-based row of a submitted list the error belongs to, so the panel can mark it; null when it is not about one row.</param>
public sealed record Error(string Code, ErrorKind Kind, string? Value = null, int? Position = null)
{
    /// <summary>Creates a validation error from a domain error code.</summary>
    /// <typeparam name="TCode">Enum of the domain error codes.</typeparam>
    /// <param name="code">What is wrong.</param>
    /// <param name="value">The value that caused it.</param>
    public static Error Validation<TCode>(TCode code, string? value)
        where TCode : struct, Enum => new(code.ToString(), ErrorKind.Validation, value);

    /// <summary>Creates a validation error from a domain error code and an amount.</summary>
    /// <typeparam name="TCode">Enum of the domain error codes.</typeparam>
    /// <param name="code">What is wrong.</param>
    /// <param name="value">The amount that caused it, when there is one.</param>
    /// <param name="position">Zero-based row it belongs to, when it is about one row.</param>
    public static Error Validation<TCode>(TCode code, decimal? value, int? position)
        where TCode : struct, Enum => new(code.ToString(), ErrorKind.Validation, value?.ToString(CultureInfo.InvariantCulture), position);

    /// <summary>Creates an error for a record that does not exist.</summary>
    /// <param name="code">Stable name of the problem.</param>
    /// <param name="id">Identifier that was not found.</param>
    public static Error NotFound(string code, Guid id) => new(code, ErrorKind.NotFound, id.ToString());

    /// <summary>Creates an error for a request that clashes with the current state.</summary>
    /// <param name="code">Stable name of the problem.</param>
    /// <param name="value">The value involved.</param>
    public static Error Conflict(string code, string value) => new(code, ErrorKind.Conflict, value);
}

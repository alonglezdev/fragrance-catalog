namespace FragranceCatalog.Application.Common;

/// <summary>Outcome of a use case that can fail for expected business reasons; all the errors travel together so the panel shows them at once.</summary>
public class Result
{
    /// <summary>Creates an outcome from its errors; no errors means success.</summary>
    /// <param name="errors">Everything that went wrong.</param>
    protected Result(IReadOnlyList<Error> errors) => Errors = errors;

    /// <summary>Whether the use case did its work.</summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>Everything that went wrong; empty on success.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>A successful outcome without a value.</summary>
    public static Result Success() => new([]);

    /// <summary>A successful outcome with a value.</summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="value">What the use case produced.</param>
    public static Result<T> Success<T>(T value) => new(value, []);

    /// <summary>A failed outcome without a value.</summary>
    /// <param name="errors">Everything that went wrong, at least one error.</param>
    public static Result Failure(IEnumerable<Error> errors) => new(RequireErrors(errors));

    /// <summary>A failed outcome for a use case that returns a value.</summary>
    /// <typeparam name="T">Type of the value it would have produced.</typeparam>
    /// <param name="errors">Everything that went wrong, at least one error.</param>
    public static Result<T> Failure<T>(IEnumerable<Error> errors) => new(default, RequireErrors(errors));

    private static List<Error> RequireErrors(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var list = errors.ToList();
        return list.Count > 0 ? list : throw new ArgumentException("A failure needs at least one error.", nameof(errors));
    }
}

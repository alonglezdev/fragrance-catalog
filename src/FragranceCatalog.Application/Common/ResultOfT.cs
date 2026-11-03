namespace FragranceCatalog.Application.Common;

/// <summary>Outcome of a use case that produces a value when it succeeds.</summary>
/// <typeparam name="T">Type of the value.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, IReadOnlyList<Error> errors)
        : base(errors) => _value = value;

    /// <summary>What the use case produced; reading it after a failure is a programming error.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");
}

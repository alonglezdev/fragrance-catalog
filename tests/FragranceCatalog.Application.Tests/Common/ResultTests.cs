using FragranceCatalog.Application.Common;

namespace FragranceCatalog.Application.Tests.Common;

public sealed class ResultTests
{
    private static readonly Error _error = new("SomeCode", ErrorKind.Validation);

    [Fact]
    public void Success_WithValue_ExposesIt()
    {
        var result = Result.Success(5);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(5);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_TwoErrors_KeepsBoth()
    {
        var result = Result.Failure<int>([_error, _error with { Code = "OtherCode" }]);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.Select(error => error.Code).ShouldBe(["SomeCode", "OtherCode"]);
    }

    [Fact]
    public void Value_FailedResult_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<int>([_error]);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_NoErrors_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Result.Failure([]));
    }
}

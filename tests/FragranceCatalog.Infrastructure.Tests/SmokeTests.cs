namespace FragranceCatalog.Infrastructure.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void TestRunner_TrivialAssertion_Passes()
    {
        var expected = 2;

        var actual = 1 + 1;

        actual.ShouldBe(expected);
    }
}

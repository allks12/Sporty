using Sporty.Services.Common;

namespace Sporty.Tests.Services;

public class OperationResultTests {
    [Fact]
    public void Success_HasValueAndNoErrors() {
        var result = OperationResult<int>.Success(42);

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
        Assert.Equal(ErrorCode.None, result.ErrorCode);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_KeepsCodeAndErrors() {
        var result = OperationResult<int>.Failure(ErrorCode.NotFound, "No such group.");

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.NotFound, result.ErrorCode);
        Assert.Equal(["No such group."], result.Errors);
    }
}

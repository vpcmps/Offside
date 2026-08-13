using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class DomainExceptionTests
{
    [Fact]
    public void ToException_exposes_code_and_errors()
    {
        var error = Error.NotFound("order", 1);

        var ex = error.ToException();

        Assert.IsType<DomainException>(ex);
        Assert.Equal("not_found", ex.Message);
        Assert.Equal(error, Assert.Single(ex.Errors));
    }
}

using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class ErrorConstructionTests
{
    [Fact]
    public void Custom_rejects_empty_code()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Error.Custom("  ", ErrorKind.Conflict));

        Assert.Equal("code", ex.ParamName);
    }

    [Fact]
    public void Errors_with_same_code_kind_field_and_arguments_are_equal()
    {
        var left = Error.Custom("order.already_shipped", ErrorKind.Conflict);
        var right = Error.Custom("order.already_shipped", ErrorKind.Conflict);

        Assert.Equal(left, right);
    }
}

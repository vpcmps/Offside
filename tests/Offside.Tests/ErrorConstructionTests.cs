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

    [Fact]
    public void Custom_snapshots_arguments_as_read_only()
    {
        var original = new Dictionary<string, object?> { ["orderId"] = "123" };

        var error = Error.Custom("order.already_shipped", ErrorKind.Conflict, original);

        original["orderId"] = "mutated";
        original["extra"] = "new";

        Assert.Equal("123", error.Arguments["orderId"]);
        Assert.False(error.Arguments.ContainsKey("extra"));

        var mutable = Assert.IsAssignableFrom<IDictionary<string, object?>>(error.Arguments);
        Assert.Throws<NotSupportedException>(() => mutable["orderId"] = "via-cast");
    }

    [Fact]
    public void Custom_trims_code()
    {
        var error = Error.Custom("  order.x  ", ErrorKind.Conflict);

        Assert.Equal("order.x", error.Code);
    }

    [Fact]
    public void Errors_with_same_arguments_and_field_are_equal()
    {
        var arguments = new Dictionary<string, object?> { ["orderId"] = "123" };
        var left = Error.Custom("order.already_shipped", ErrorKind.Conflict, arguments, "shipping");
        var right = Error.Custom("order.already_shipped", ErrorKind.Conflict, arguments, "shipping");

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Errors_with_different_arguments_are_not_equal()
    {
        var left = Error.Custom(
            "order.already_shipped",
            ErrorKind.Conflict,
            new Dictionary<string, object?> { ["orderId"] = "123" });
        var right = Error.Custom(
            "order.already_shipped",
            ErrorKind.Conflict,
            new Dictionary<string, object?> { ["orderId"] = "456" });

        Assert.NotEqual(left, right);
        Assert.False(left == right);
        Assert.True(left != right);
    }

    [Fact]
    public void Errors_with_different_field_are_not_equal()
    {
        var left = Error.Custom("validation", ErrorKind.Validation, field: "email");
        var right = Error.Custom("validation", ErrorKind.Validation, field: "name");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Errors_with_different_kind_are_not_equal()
    {
        var left = Error.Custom("order.already_shipped", ErrorKind.Conflict);
        var right = Error.Custom("order.already_shipped", ErrorKind.Gone);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Errors_with_different_code_are_not_equal()
    {
        var left = Error.Custom("order.already_shipped", ErrorKind.Conflict);
        var right = Error.Custom("order.cancelled", ErrorKind.Conflict);

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Equal_errors_with_arguments_have_the_same_hash_code()
    {
        var arguments = new Dictionary<string, object?> { ["orderId"] = "123" };
        var left = Error.Custom("order.already_shipped", ErrorKind.Conflict, arguments, "shipping");
        var right = Error.Custom("order.already_shipped", ErrorKind.Conflict, arguments, "shipping");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equality_operators_are_null_safe()
    {
        var error = Error.Custom("order.x", ErrorKind.Conflict);

        Assert.False(error == null);
        Assert.False(null == error);
        Assert.True(error != null);
        Assert.True(null != error);
        Assert.True((Error?)null == (Error?)null);
        Assert.False((Error?)null != (Error?)null);
    }
}

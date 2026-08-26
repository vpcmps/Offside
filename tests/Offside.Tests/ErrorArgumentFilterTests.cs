using Xunit;

namespace Offside.Tests;

public sealed class ErrorArgumentFilterTests
{
    private static readonly Error Sample = Error.Custom(
        "access.rejected",
        ErrorKind.Unauthorized,
        new { rejectionReason = "missing-header", document = "12345678900", empty = (string?)null });

    [Fact]
    public void Default_emits_nothing() =>
        Assert.Empty(ErrorArgumentFilter.Select(Sample, includeAll: false, keys: Array.Empty<string>()));

    [Fact]
    public void An_allowlist_emits_only_named_non_null_keys()
    {
        var selected = ErrorArgumentFilter.Select(
            Sample,
            includeAll: false,
            keys: new[] { "rejectionReason", "document", "empty" })
            .ToArray();

        Assert.Equal(2, selected.Length);
        Assert.Contains(selected, pair => pair.Key == "rejectionReason" && Equals(pair.Value, "missing-header"));
        Assert.Contains(selected, pair => pair.Key == "document" && Equals(pair.Value, "12345678900"));
        Assert.DoesNotContain(selected, pair => pair.Key == "empty");
    }

    [Fact]
    public void Include_all_ignores_the_allowlist_and_skips_nulls()
    {
        var selected = ErrorArgumentFilter.Select(
            Sample,
            includeAll: true,
            keys: new[] { "rejectionReason" })
            .Select(pair => pair.Key)
            .ToArray();

        Assert.Equal(new[] { "rejectionReason", "document" }, selected);
    }

    [Fact]
    public void A_null_error_is_rejected() =>
        Assert.Throws<ArgumentNullException>(() =>
            ErrorArgumentFilter.Select(null!, includeAll: true, keys: null).ToArray());
}

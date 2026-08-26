using System.Globalization;
using System.Text;

namespace Offside.Testing;

/// <summary>
/// Renders errors for assertion messages. Every failure message in the package goes through
/// here, so improving the diagnostics is a single-file change.
/// </summary>
internal static class ErrorFormatter
{
    /// <summary>Renders a single error on one line.</summary>
    public static string Describe(Error error)
    {
        var text = new StringBuilder();
        text.Append('"').Append(error.Code).Append('"');
        text.Append(" (errorCode: ").Append(error.ErrorCode);
        text.Append(", kind: ").Append(error.Kind);

        if (error.Field != null)
            text.Append(", field: ").Append(error.Field);

        if (error.Arguments.Count > 0)
            text.Append(", arguments: ").Append(DescribeArguments(error.Arguments));

        text.Append(')');
        return text.ToString();
    }

    /// <summary>Renders a list of errors as an indented, indexed block.</summary>
    public static string Describe(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return "  (no errors)";

        var text = new StringBuilder();
        for (var index = 0; index < errors.Count; index++)
        {
            if (index > 0)
                text.Append(Environment.NewLine);

            text.Append("  [").Append(index.ToString(CultureInfo.InvariantCulture)).Append("] ");
            text.Append(Describe(errors[index]));
        }

        return text.ToString();
    }

    /// <summary>Renders the arguments dictionary as <c>name=value</c> pairs.</summary>
    public static string DescribeArguments(IReadOnlyDictionary<string, object?> arguments)
    {
        var text = new StringBuilder();
        text.Append('{');

        var first = true;
        foreach (var pair in arguments)
        {
            if (!first)
                text.Append(", ");
            first = false;

            text.Append(pair.Key).Append('=').Append(DescribeValue(pair.Value));
        }

        text.Append('}');
        return text.ToString();
    }

    /// <summary>Renders a single value, keeping <see langword="null"/> and strings unambiguous.</summary>
    public static string DescribeValue(object? value)
    {
        if (value is null)
            return "null";

        if (value is string text)
            return "\"" + text + "\"";

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>Describes what a failed result carries, for use after a "but" clause.</summary>
    public static string DescribeFailure(IReadOnlyList<Error> errors)
    {
        var count = errors.Count.ToString(CultureInfo.InvariantCulture);
        return "it failed with " + count + " error(s):" + Environment.NewLine + Describe(errors);
    }
}

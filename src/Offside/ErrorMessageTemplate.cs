using System.Globalization;

namespace Offside;

/// <summary>Interpolates error-message templates from error arguments.</summary>
public static class ErrorMessageTemplate
{
    /// <summary>
    /// Replaces tokens in the form <c>{name}</c> with matching argument values.
    /// </summary>
    /// <param name="template">The message template.</param>
    /// <param name="arguments">Values available to the template.</param>
    /// <returns>The interpolated template.</returns>
    public static string Interpolate(string template, IReadOnlyDictionary<string, object?> arguments)
    {
        if (template is null)
            throw new ArgumentNullException(nameof(template));
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));

        foreach (var pair in arguments)
        {
            if (pair.Value is null)
                continue;

            var token = "{" + pair.Key + "}";
            var value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            template = template.Replace(token, value);
        }

        return template;
    }
}

using System.Globalization;

namespace Offside;

/// <summary>
/// Turns an <see cref="Error"/> into human-readable text for a given culture.
/// </summary>
/// <remarks>
/// <see cref="JsonErrorMessageResolver"/> is the built-in implementation, registered as a
/// singleton by <c>AddOffside</c>. Implement this interface to source messages from somewhere
/// other than JSON catalogs, such as a database or resource files.
/// </remarks>
public interface IErrorMessageResolver
{
    /// <summary>Resolves the message for an error.</summary>
    /// <param name="error">The error to describe.</param>
    /// <param name="culture">The culture to resolve the message in.</param>
    /// <returns>
    /// The resolved and interpolated message. Implementations should degrade gracefully —
    /// the built-in resolver returns <see cref="Error.Code"/> when no template is found.
    /// </returns>
    string GetMessage(Error error, CultureInfo culture);
}

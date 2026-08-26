namespace Offside.Refit;

/// <summary>
/// Controls how a failed Refit call is translated into Offside <see cref="Error"/> values.
/// </summary>
/// <remarks>
/// The defaults suit a service that calls a dependency it does not own. Nothing here configures
/// Refit, <see cref="System.Net.Http.HttpClient"/>, or resilience — that stays in the host.
/// </remarks>
public sealed class OffsideRefitOptions
{
    /// <summary>
    /// Gets or sets the name of the dependency being called, exposed to message templates as
    /// <c>{api}</c>. Defaults to <c>external api</c>.
    /// </summary>
    public string ApiName { get; set; } = "external api";

    /// <summary>
    /// Gets or sets the prefix applied to catalog codes, so a 404 from the dependency becomes
    /// <c>external_api.not_found</c>. Set it to an empty string to fall back to the core codes
    /// (<c>not_found</c>, <c>timeout</c>, …), which ship in the default catalog.
    /// </summary>
    public string CodePrefix { get; set; } = "external_api";

    /// <summary>
    /// Gets or sets a value indicating whether an <c>application/problem+json</c> body is read
    /// from the failed response. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When the dependency is itself an Offside service, its <c>errors</c> array is restored
    /// error for error. Parsing never throws: a malformed or unexpected body degrades to the
    /// status-code mapping.
    /// </remarks>
    public bool ReadProblemDetails { get; set; } = true;

    internal static OffsideRefitOptions Default { get; } = new OffsideRefitOptions();

    internal string Code(string suffix) =>
        string.IsNullOrWhiteSpace(CodePrefix) ? suffix : CodePrefix.Trim() + "." + suffix;
}

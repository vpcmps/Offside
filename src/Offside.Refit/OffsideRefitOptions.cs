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
    /// <c>external_api.not_found</c> before <see cref="InboundStatus"/> is applied. Set it to an
    /// empty string to fall back to the core codes (<c>not_found</c>, <c>timeout</c>, …).
    /// </summary>
    public string CodePrefix { get; set; } = "external_api";

    /// <summary>
    /// Gets or sets a value indicating whether an <c>application/problem+json</c> body is read
    /// from the failed response. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When the dependency is itself an Offside service, its <c>errors</c> array is restored
    /// error for error. Parsing never throws: a malformed or unexpected body degrades to the
    /// status-code mapping. <see cref="InboundStatus"/> then runs on the restored errors.
    /// </remarks>
    public bool ReadProblemDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets how a dependency's client-error kinds are exposed to the caller.
    /// Defaults to <see cref="InboundStatusMapping.CollapseClientErrors"/>.
    /// </summary>
    public InboundStatusMapping InboundStatus { get; set; } = InboundStatusMapping.CollapseClientErrors;

    internal static OffsideRefitOptions Default { get; } = new OffsideRefitOptions();

    internal string Code(string suffix) =>
        string.IsNullOrWhiteSpace(CodePrefix) ? suffix : CodePrefix.Trim() + "." + suffix;
}

/// <summary>
/// What to do with a 4xx kind coming from a dependency — either the status mapping or a restored
/// Offside problem body.
/// </summary>
public enum InboundStatusMapping
{
    /// <summary>
    /// Fold every 4xx kind into <see cref="ErrorKind.ServiceUnavailable"/> so a 404 from a
    /// dependency is not your 404. The default. Timeout, service-unavailable, and unexpected
    /// stay as they are.
    /// </summary>
    CollapseClientErrors = 0,

    /// <summary>
    /// Keep the kind the dependency used. Opt in when two Offside services speak of the same
    /// resource, or when this host is a BFF that should surface the dependency's status.
    /// </summary>
    Mirror = 1
}

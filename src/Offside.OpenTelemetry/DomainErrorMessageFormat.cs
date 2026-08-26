namespace Offside.OpenTelemetry;

/// <summary>
/// Ready-made choices for <see cref="OffsideOpenTelemetryOptions.FormatMessage"/>, the text of the
/// log entry. Every format receives the error and its already-resolved message, so none of them
/// has to know about the catalog or the culture.
/// </summary>
/// <remarks>
/// This only shapes the log line. The code, the kind, and the field always travel as dimensions
/// whatever the format, so a format is never the reason a value stops being queryable.
/// </remarks>
public static class DomainErrorMessageFormat
{
    /// <summary>
    /// The resolved message on its own — the default. Best when the entries land somewhere the
    /// dimensions are queryable, which is the case for any OpenTelemetry backend.
    /// </summary>
    public static readonly Func<Error, string, string> MessageOnly =
        static (_, message) => message;

    /// <summary>
    /// The catalog code in brackets, then the message: <c>[order.already_shipped] Order already shipped.</c>
    /// Best when a human reads these lines raw — a console, a container log, a tail over SSH —
    /// where nothing renders the dimensions.
    /// </summary>
    public static readonly Func<Error, string, string> CodePrefixed =
        static (error, message) => "[" + error.Code + "] " + message;

    /// <summary>
    /// The screen identifier in brackets, then the message: <c>[ORDER_ALREADY_SHIPPED] Order already shipped.</c>
    /// Best when support reads logs against the identifier a user reports from the screen.
    /// </summary>
    public static readonly Func<Error, string, string> ErrorCodePrefixed =
        static (error, message) => "[" + error.ErrorCode + "] " + message;
}

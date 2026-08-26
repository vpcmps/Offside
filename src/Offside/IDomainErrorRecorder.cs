namespace Offside;

/// <summary>
/// Records an Offside <see cref="Error"/> as telemetry, so a domain failure that never became an
/// exception is still visible in the logs.
/// </summary>
/// <remarks>
/// Use <see cref="Result.RecordTo"/> / <see cref="Result{T}.RecordTo"/> to record a whole
/// result. HTTP hosts do not need to call those: <c>ToHttpResult</c> and <c>SendOffsideAsync</c>
/// record through this interface when it is registered. Implementations must not throw: recording
/// telemetry is never a reason to fail a request.
/// </remarks>
public interface IDomainErrorRecorder
{
    /// <summary>Records one error.</summary>
    /// <param name="error">The error to record.</param>
    /// <param name="properties">
    /// Extra dimensions merged into the telemetry, such as a tenant or an operation name.
    /// They are written verbatim, without the Offside prefix, and win over nothing —
    /// keys that collide with an Offside dimension are ignored.
    /// </param>
    void Record(Error error, IReadOnlyDictionary<string, string>? properties = null);
}

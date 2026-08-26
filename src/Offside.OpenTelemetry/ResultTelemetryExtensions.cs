namespace Offside.OpenTelemetry;

/// <summary>Records the errors of a failed <see cref="Result"/> as telemetry.</summary>
public static class ResultTelemetryExtensions
{
    /// <summary>Records one entry per error, in result order. A successful result records nothing.</summary>
    /// <param name="result">The result to inspect.</param>
    /// <param name="recorder">The recorder to write to.</param>
    /// <param name="properties">Extra dimensions merged into every entry.</param>
    /// <returns>The original result, so this can sit in a chain.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recorder"/> is <see langword="null"/>.</exception>
    public static Result RecordTo(
        this Result result,
        IDomainErrorRecorder recorder,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        if (recorder is null)
            throw new ArgumentNullException(nameof(recorder));

        foreach (var error in result.Errors)
            recorder.Record(error, properties);

        return result;
    }

    /// <summary>Records one entry per error, in result order. A successful result records nothing.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="recorder">The recorder to write to.</param>
    /// <param name="properties">Extra dimensions merged into every entry.</param>
    /// <returns>The original result, so this can sit in a chain.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recorder"/> is <see langword="null"/>.</exception>
    public static Result<T> RecordTo<T>(
        this Result<T> result,
        IDomainErrorRecorder recorder,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        if (recorder is null)
            throw new ArgumentNullException(nameof(recorder));

        foreach (var error in result.Errors)
            recorder.Record(error, properties);

        return result;
    }
}

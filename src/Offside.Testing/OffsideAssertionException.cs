namespace Offside.Testing;

/// <summary>
/// Thrown when an Offside assertion fails.
/// </summary>
/// <remarks>
/// This package deliberately has no test-framework dependency. xUnit, NUnit, MSTest and TUnit
/// all report an unhandled exception as a failed test, so a plain exception is enough to fail
/// a test in any of them. The message always carries the actual contents of the subject, which
/// is the whole point of the package.
/// </remarks>
public sealed class OffsideAssertionException : Exception
{
    /// <summary>Initializes a new instance with a failure message.</summary>
    /// <param name="message">The failure message, already formatted.</param>
    public OffsideAssertionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a failure message and an inner exception.</summary>
    /// <param name="message">The failure message, already formatted.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public OffsideAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

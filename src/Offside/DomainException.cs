namespace Offside;

/// <summary>
/// The escape hatch: carries domain <see cref="Error"/> values across a boundary that cannot
/// return a <see cref="Result"/>.
/// </summary>
/// <remarks>
/// Ordinary business rules should return a failed <see cref="Result"/> instead of throwing.
/// Use this only where the signature is out of your control, such as a constructor or an interface
/// you do not own. <see cref="Exception.Message"/> is the first error's <see cref="Error.Code"/>.
/// </remarks>
public sealed class DomainException : Exception
{
    /// <summary>Gets the errors carried by this exception, in reporting order.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>Initializes a new instance carrying the given errors.</summary>
    /// <param name="errors">The errors. Must contain at least one element.</param>
    public DomainException(IReadOnlyList<Error> errors)
        : base(errors[0].Code)
    {
        Errors = errors;
    }
}

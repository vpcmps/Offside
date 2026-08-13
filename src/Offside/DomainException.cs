namespace Offside;

public sealed class DomainException : Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public DomainException(IReadOnlyList<Error> errors)
        : base(errors[0].Code)
    {
        Errors = errors;
    }
}

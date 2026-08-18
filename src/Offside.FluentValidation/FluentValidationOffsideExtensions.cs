using FluentValidation;
using FluentValidation.Results;

namespace Offside.FluentValidation;

/// <summary>
/// Maps FluentValidation failures onto Offside <see cref="Error"/> values. Catalog codes come
/// from <see cref="ValidationFailure.ErrorCode"/> when the consumer set <c>WithErrorCode</c>;
/// FluentValidation's default <c>*Validator</c> names are not catalog keys.
/// </summary>
public static class FluentValidationOffsideExtensions
{
    /// <summary>Maps each failure to an Offside error, preserving order.</summary>
    /// <param name="failures">The FluentValidation failures.</param>
    /// <returns>The Offside errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failures"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Error> ToOffsideErrors(this IEnumerable<ValidationFailure> failures)
    {
        if (failures is null)
            throw new ArgumentNullException(nameof(failures));

        return failures.Select(ToError).ToArray();
    }

    /// <summary>Maps the result's failures. Empty when the result is valid.</summary>
    /// <param name="result">The FluentValidation result.</param>
    /// <returns>The Offside errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Error> ToOffsideErrors(this ValidationResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        return result.Errors.ToOffsideErrors();
    }

    /// <summary>Maps the exception's failures.</summary>
    /// <param name="exception">The thrown FluentValidation exception.</param>
    /// <returns>The Offside errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Error> ToOffsideErrors(this ValidationException exception)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));

        return exception.Errors.ToOffsideErrors();
    }

    /// <summary>
    /// A successful <see cref="Result"/> when the validation passed; otherwise a failure
    /// carrying every mapped error.
    /// </summary>
    /// <param name="result">The FluentValidation result.</param>
    /// <returns>The Offside result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static Result ToResult(this ValidationResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        return result.IsValid
            ? Result.Success()
            : Result.Failure(result.ToOffsideErrors());
    }

    private static Error ToError(ValidationFailure failure)
    {
        var catalogCode = CatalogCode(failure.ErrorCode);
        if (string.IsNullOrWhiteSpace(failure.PropertyName))
            return Error.Custom(
                catalogCode,
                ErrorKind.Validation,
                new { attemptedValue = failure.AttemptedValue });

        return Error.Validation(failure.PropertyName, catalogCode, failure.AttemptedValue);
    }

    private static string CatalogCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            return "validation";

        var trimmed = errorCode!.Trim();
        return trimmed.EndsWith("Validator", StringComparison.Ordinal)
            ? "validation"
            : trimmed;
    }
}

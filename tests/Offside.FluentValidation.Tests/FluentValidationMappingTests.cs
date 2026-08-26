using FluentValidation;
using FluentValidation.Results;
using Offside.FluentValidation;
using Offside.Testing;
using Xunit;

namespace Offside.FluentValidation.Tests;

public sealed class FluentValidationMappingTests
{
    [Fact]
    public void WithErrorCode_becomes_catalog_code()
    {
        var failure = new ValidationFailure("Email", "taken")
        {
            ErrorCode = "email.taken",
            AttemptedValue = "a@b.c"
        };

        var error = new[] { failure }.ToOffsideErrors().Single();

        Assert.Equal("email.taken", error.Code);
        Assert.Equal("VALIDATION", error.ErrorCode);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal("Email", error.Field);
        Assert.Equal("a@b.c", error.Arguments["attemptedValue"]);
    }

    [Fact]
    public void FluentValidation_default_validator_code_becomes_validation()
    {
        var failure = new ValidationFailure("Name", "required")
        {
            ErrorCode = "NotEmptyValidator"
        };

        var error = new[] { failure }.ToOffsideErrors().Single();

        Assert.Equal("validation", error.Code);
        Assert.Equal("VALIDATION", error.ErrorCode);
        Assert.Equal("Name", error.Field);
    }

    [Fact]
    public void Empty_error_code_becomes_validation()
    {
        var failure = new ValidationFailure("Name", "required")
        {
            ErrorCode = "  "
        };

        var error = new[] { failure }.ToOffsideErrors().Single();

        Assert.Equal("validation", error.Code);
    }

    [Fact]
    public void Missing_property_name_leaves_field_null()
    {
        var failure = new ValidationFailure("", "model invalid")
        {
            ErrorCode = "model.invalid"
        };

        var error = new[] { failure }.ToOffsideErrors().Single();

        Assert.Null(error.Field);
        Assert.Equal("model.invalid", error.Code);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal("VALIDATION", error.ErrorCode);
    }

    [Fact]
    public void Valid_result_maps_to_success()
    {
        var result = new ValidationResult();

        Assert.Empty(result.ToOffsideErrors());
        result.ToResult().ShouldBeSuccess();
    }

    [Fact]
    public void Invalid_result_maps_to_failure()
    {
        var result = new ValidationResult(
        [
            new ValidationFailure("Email", "taken") { ErrorCode = "email.taken" }
        ]);

        var mapped = result.ToResult();

        mapped.ShouldBeFailure();
        Assert.Equal("email.taken", mapped.Errors[0].Code);
    }

    [Fact]
    public void ValidationException_maps_failures()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Age", "too young") { ErrorCode = "age.minimum" }
        ]);

        var error = exception.ToOffsideErrors().Single();

        Assert.Equal("age.minimum", error.Code);
        Assert.Equal("Age", error.Field);
    }
}

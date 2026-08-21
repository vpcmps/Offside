---
name: offside-fluentvalidation
description: Maps FluentValidation failures to Offside Error and Result. Use when converting ValidationResult, ValidationFailure, or ValidationException in a .NET domain or application layer.
---

# Offside FluentValidation

Package `Offside.FluentValidation`. No HTTP. No FastEndpoints.

Use this skill after FluentValidation is selected. If project integrations are still undecided, use `offside-setup` first.

```csharp
using FluentValidation;
using Offside.FluentValidation;

RuleFor(x => x.Email).NotEmpty().WithErrorCode("email.required");

var mapped = validator.Validate(request).ToResult();
```

`.WithErrorCode("email.required")` becomes `Error.Code` (catalog key). FluentValidation's default `*Validator` names map to `validation`. `Error.ErrorCode` is always `VALIDATION`.

The FluentValidation message text is discarded — Offside resolves the catalog.

Model-level failures (`PropertyName` empty) set `Field` to null.

## API

- `failures.ToOffsideErrors()`
- `result.ToOffsideErrors()` / `result.ToResult()`
- `exception.ToOffsideErrors()`

## Do not

- Put FluentValidation messages in C# as if they were Offside catalog text.
- Reference this package from a domain that must not take a FluentValidation dependency — map at the application boundary.

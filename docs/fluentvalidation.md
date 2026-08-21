# FluentValidation

*[Português](pt-BR/fluentvalidation.md) · [Back to docs](README.md)*

`Offside.FluentValidation` turns FluentValidation failures into Offside `Error` values. It does not know about HTTP.

```bash
dotnet add package Offside.FluentValidation
```

Targets: `netstandard2.0`, `net8.0`, `net10.0`.

## Mapping

| FluentValidation | Offside |
|---|---|
| `.WithErrorCode("email.taken")` | `Error.Code` (catalog key) |
| Default `NotEmptyValidator` (and other `*Validator` names) | `code` = `validation` |
| `PropertyName` | `Field` (null when empty) |
| `AttemptedValue` | `Arguments["attemptedValue"]` |
| — | `Error.ErrorCode` = `VALIDATION` |
| `ErrorMessage` | discarded; the Offside catalog supplies the text |

```csharp
RuleFor(x => x.Email).NotEmpty().WithErrorCode("email.required");

var result = validator.Validate(request).ToResult();
```

Also: `failures.ToOffsideErrors()`, `validationResult.ToOffsideErrors()`, `exception.ToOffsideErrors()`.

Add the same key to `errors.json`:

```json
{ "email.required": "Email is required." }
```

FastEndpoints hosts should use [`Offside.FastEndpoint`](fastendpoints.md) instead of calling this from an endpoint by hand — that package already runs the mapper in the error pipeline.

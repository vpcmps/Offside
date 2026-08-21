# FluentValidation

*[English](../fluentvalidation.md) · [Voltar à docs](README.md)*

`Offside.FluentValidation` transforma falhas do FluentValidation em `Error` do Offside. Não conhece HTTP.

```bash
dotnet add package Offside.FluentValidation
```

Targets: `netstandard2.0`, `net8.0`, `net10.0`.

## Mapeamento

| FluentValidation | Offside |
|---|---|
| `.WithErrorCode("email.taken")` | `Error.Code` (chave do catálogo) |
| Default `NotEmptyValidator` (e outros `*Validator`) | `code` = `validation` |
| `PropertyName` | `Field` (null se vazio) |
| `AttemptedValue` | `Arguments["attemptedValue"]` |
| — | `Error.ErrorCode` = `VALIDATION` |
| `ErrorMessage` | descartada; o catálogo Offside fornece o texto |

```csharp
RuleFor(x => x.Email).NotEmpty().WithErrorCode("email.required");

var result = validator.Validate(request).ToResult();
```

Também: `failures.ToOffsideErrors()`, `validationResult.ToOffsideErrors()`, `exception.ToOffsideErrors()`.

Coloque a mesma chave em `errors.json`:

```json
{ "email.required": "E-mail é obrigatório." }
```

Hosts FastEndpoints devem usar [`Offside.FastEndpoint`](fastendpoints.md) em vez de chamar isto à mão no endpoint — esse pacote já executa o mapper no pipeline de erro.

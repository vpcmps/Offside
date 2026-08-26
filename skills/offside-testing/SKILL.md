---
name: offside-testing
description: Writes unit tests for code that returns Offside Result values — asserting Result, Result<T>, Error, ErrorKind and JSON message catalogs with Offside.Testing (ShouldBeSuccess, ShouldHaveError, ShouldHaveOnlyError, OffsideCatalog). Use when testing a handler, service, or validator that returns Result instead of throwing, or when checking that error codes exist in the catalog.
---

# Offside Testing

Package `Offside.Testing`. Only for projects that already use Offside. If the project does not return `Result`, this skill does not apply.

No test-framework dependency: failures throw `OffsideAssertionException`, which xUnit, NUnit, MSTest and TUnit all report as a failed test. The names are `ShouldHaveError`, not `Should()`, so it coexists with FluentAssertions or Shouldly in the same file.

```bash
dotnet add package Offside.Testing
```

## Assert the result

```csharp
using Offside.Testing;

handler.Handle(command).ShouldBeSuccess();

var order = handler.Handle(query).ShouldBeSuccess().Subject;
handler.Handle(query).ShouldBeSuccess().WithValue(o => o.Status == OrderStatus.Paid, "paid");

result.ShouldHaveError("order.duplicated").WithKind(ErrorKind.Conflict).ForField("number");
```

| Assertion | Means |
|---|---|
| `ShouldBeSuccess()` | Succeeded. On `Result<T>`, exposes the value via `Subject` / `WithValue`. |
| `ShouldBeFailure()` | Failed, without saying how. |
| `ShouldHaveError(code)` | Failed carrying this error, ignoring others. **Default choice.** |
| `ShouldHaveOnlyError(code)` | Failed carrying this error and nothing else. |
| `ShouldHaveErrorsInOrder(codes)` | Carries exactly these codes, in this order. |
| `ShouldHaveErrorCount(n)` | Carries this many errors. |

Refinements on a located error: `WithKind`, `WithErrorCode`, `ForField`, `WithArgument`, `WithMessage(resolver, text)`. `.And` returns the result for further assertions and is always optional.

## Assert the catalog

A code with no catalog entry is a runtime-only defect — the resolver falls back to the code itself, so users see `order.not_found` instead of a sentence.

```csharp
var catalog = OffsideCatalog.FromFile("errors/errors.json");

catalog.ShouldDefineAll("order.not_found", "order.duplicated");
catalog.ShouldResolve(Error.NotFound("order", 42));

OffsideCatalog.FromFile("errors/errors.pt-BR.json").ShouldDefineSameCodesAs(catalog);
```

`ShouldResolve` checks the code exists **and** that no `{token}` was left unfilled by `Error.Arguments`. `FromJson`, `FromStream` and `FromAssembly` cover catalogs not on disk.

## What to cover

For a flow returning `Result`, write at minimum:

1. One test per failure path. Nothing throws, so an unfired rule is silent otherwise.
2. One `ShouldDefineAll` covering every code the flow can return.
3. `ShouldResolve` for every code whose template has a `{token}`, using an error built the way production builds it — this catches an argument renamed on one side only.
4. `ShouldDefineSameCodesAs` per translated catalog.
5. `WithKind` wherever the kind decides the HTTP status, so a 409 cannot silently become a 422.

## Do not

- Do not use `ShouldHaveErrorsInOrder` as the default. Order comes from `Result.Combine` argument order or from FluentValidation rule declaration order, so reordering rules breaks it without any behaviour changing. Use `ShouldHaveError` unless order is the thing being asserted.
- Do not use `ShouldHaveOnlyError` everywhere. Reserve it for where an extra error leaking in is itself the defect.
- Do not prove a catalog entry exists with `WithMessage`. The built-in resolver returns `Error.Code` on a miss, so it passes either way — use `OffsideCatalog.ShouldDefine`.
- Do not assert `Assert.True(result.IsFailure)`. It reports nothing about which errors were carried, which is the whole reason this package exists.
- Do not add this package to a production project. It is a test-only dependency.

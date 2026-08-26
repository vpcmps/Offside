# Testing guide

*[Português](pt-BR/testing.md) · [Back to docs](README.md)*

`Offside.Testing` asserts over `Result`, `Result<T>`, `Error`, and message catalogs. The point is not shorter test code — it is what you read when a test fails. `Assert.True(result.IsFailure)` reports `Assert.True() Failure`; this package reports which errors the result actually carried.

## Install

```bash
dotnet add package Offside.Testing
```

It has no test-framework dependency. Failures are thrown as `OffsideAssertionException`, which xUnit, NUnit, MSTest and TUnit all report as a failed test.

The entry points are named `ShouldHaveError` and friends rather than `Should()`, so the package can be used in the same file as FluentAssertions or Shouldly without colliding with their entry point.

## Asserting a result

```csharp
using Offside.Testing;

createOrder.Handle(command).ShouldBeSuccess();

var order = getOrder.Handle(query).ShouldBeSuccess().Subject;
getOrder.Handle(query).ShouldBeSuccess().WithValue(o => o.Status == OrderStatus.Paid, "paid");
```

| Assertion | Means |
|---|---|
| `ShouldBeSuccess()` | The result succeeded. On `Result<T>`, exposes the value for refinement. |
| `ShouldBeFailure()` | The result failed, without saying how. |
| `ShouldHaveError(code)` | It failed carrying this error, ignoring any other. **The default choice.** |
| `ShouldHaveOnlyError(code)` | It failed carrying this error and nothing else. |
| `ShouldHaveErrorsInOrder(codes)` | It carries exactly these codes, in this order. |
| `ShouldHaveErrorCount(n)` | It carries this many errors, without saying which. |

`ShouldHaveError` is the one to reach for. It survives a rule being added elsewhere in the flow. `ShouldHaveOnlyError` is the stricter sibling: use it where an extra error leaking in is the defect you want to catch.

`ShouldHaveErrorsInOrder` pins down ordering, which comes from the source: argument order for `Result.Combine`, and rule declaration order for errors bridged from FluentValidation. Reordering rules in a validator will break it without any behaviour changing — only use it when the order is what you mean to assert.

## Refining the error

```csharp
result.ShouldHaveError("order.duplicated")
      .WithKind(ErrorKind.Conflict)
      .WithErrorCode("CONFLICT")
      .ForField("number")
      .WithArgument("number", "A-1");
```

The error is located first, so a failure says which part disagreed — "the error exists, but its kind is `Conflict`, expected `Validation`" — rather than "no error matched".

`WithMessage` resolves the message through a resolver you pass in:

```csharp
result.ShouldHaveError("not_found").WithMessage(resolver, "order 42 was not found");
result.ShouldHaveError("not_found").WithMessage(resolver, new CultureInfo("pt-BR"), "pedido 42 não encontrado");
```

Note that the built-in resolver returns `Error.Code` when the catalog has no entry for it, so a passing `WithMessage` does not prove the catalog defines the code. `OffsideCatalog` is what proves that.

## Chaining

`.And` hands the original result back:

```csharp
result.ShouldHaveError("user.email_invalid").ForField("email")
      .And.ShouldHaveError("user.age_invalid").ForField("age");
```

It is always optional — starting a new statement on the same result does the same thing, and is often easier to read:

```csharp
createResult.ShouldHaveError("order.duplicated").ForField("number");
updateResult.ShouldBeSuccess().WithValue(o => o.Status == OrderStatus.Paid);
```

## Asserting the catalog

A code with no catalog entry is a runtime-only defect: the resolver falls back to the code itself, so users see `order.not_found` where a sentence should be. `OffsideCatalog` reads the JSON directly and turns that into a build-time failure.

```csharp
var catalog = OffsideCatalog.FromFile("errors/errors.json");

catalog.ShouldDefine("order.not_found");
catalog.ShouldDefineAll("order.not_found", "order.duplicated", "order.already_shipped");
catalog.ShouldResolve(Error.NotFound("order", 42));
```

`ShouldResolve` is the strong one: it checks the code exists **and** that no `{token}` was left unfilled by `Error.Arguments`. A template of `"{resource} {id} was not found"` resolved against an error with no `id` argument fails here, naming the leftover token.

Keeping a translated catalog honest:

```csharp
var invariant = OffsideCatalog.FromFile("errors/errors.json");
var translated = OffsideCatalog.FromFile("errors/errors.pt-BR.json");

translated.ShouldDefineSameCodesAs(invariant);
```

`FromJson`, `FromStream` and `FromAssembly` cover catalogs that do not live on disk — embedded resources, or content pulled from Azure App Configuration and materialised in the test.

## What to cover

A useful minimum for a flow that returns `Result`:

- **Every failure path has a test.** A rule with no test is a rule that can silently stop firing — and with `Result`, nothing throws to tell you.
- **Every code a handler can return is defined in the catalog.** One `ShouldDefineAll` in a single test covers the whole flow.
- **Every code with a `{token}` template is asserted with `ShouldResolve`**, using an error built the way production builds it. This is the check that catches an argument renamed on one side only.
- **Translated catalogs are compared against the invariant one** with `ShouldDefineSameCodesAs`, in one test per culture.
- **Assert kind, not just code**, where the kind decides an HTTP status. `WithKind` is what keeps a 409 from quietly becoming a 422.
- Prefer `ShouldHaveError` throughout; reserve `ShouldHaveOnlyError` for the places where an extra error is itself the bug.

## Using it alongside FluentAssertions

Both can live in the same test. Offside assertions carry the domain vocabulary; the general-purpose library covers everything else:

```csharp
var order = handler.Handle(query).ShouldBeSuccess().Subject;

order.Lines.Should().HaveCount(3);
```

`Subject` exists for exactly this handoff — it exposes the located `Error` or the success value so another library can take over.

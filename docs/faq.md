# FAQ

*[Português](pt-BR/faq.md) · [Back to docs](README.md)*

## Why `Result` instead of exceptions?

Exceptions are for the unexpected. A missing order or a duplicate email is not unexpected — it is an outcome the caller must handle. Returning it means the signature is honest (`Result<Order> Get(string id)` admits failure; `Order Get(string id)` does not), several failures can be reported at once, and the transport mapping is data rather than a chain of `catch` blocks.

`ErrorKind.Unexpected` still exists for genuine faults, and gets [special handling](aspnet-guide.md#unexpected-errors-and-500s).

## Why is there no implicit conversion from `T` to `Result<T>`?

So a value never becomes a success by accident. With an implicit conversion, changing a method's return type to `Result<T>` compiles silently and every existing `return value;` keeps working — including the ones that should now be failures. Explicit construction turns that into a compile error you have to look at.

## Why can't I define my own `ErrorKind`?

Because a closed kind set makes the HTTP mapping total. Every error the domain can produce already has a defined status code — no registry to keep in sync, no default branch to forget, no `500` because someone added a kind and missed a switch.

Specificity lives in the code space instead, which is open:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
```

Clients branch on the code. The kind only decides the status and the severity rank.

## Why does the status come from the most severe error rather than the first?

So a genuine fault is never masked by a validation message that happened to be listed first. A result carrying both a `Validation` and an `Unexpected` error is a 500, not a 400.

Nothing is lost: all errors ship in the `errors` array regardless. Ties within a rank — `Unauthorized` and `Forbidden`, or `Validation` and `BadRequest` — go to the first error in the result.

## How do I return `201 Created`?

Branch before converting. `ToHttpResult` owns the failure path; you keep the success path:

```csharp
var result = handler.Handle(cmd);
return result.IsSuccess
    ? Results.Created($"/orders/{result.Value.Id}", result.Value)
    : result.ToHttpResult(http);
```

## `AddOffside` throws at startup. Why?

You did not register an invariant-culture catalog:

```csharp
options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors/errors.json"));
```

It is the final fallback for every lookup, so it is required. Failing at boot is deliberate — the alternative is discovering it on the first failing request in production.

## My messages come back as raw codes like `not_found`

The resolver could not find a template, so it returned the code. Check that:

- The code has an entry in the catalog — custom codes need one added by hand.
- The catalog file actually reaches the output directory (`<None Update="errors\*.json" CopyToOutputDirectory="PreserveNewest" />`).
- You passed the file **contents** to `AddJson`, not the path.

## A `{token}` shows up literally in the message

The template referenced an argument the error does not carry, or carries as null. `Error.NotFound("order")` against `"{resource} '{id}' was not found."` yields `order '{id}' was not found.` — the `id` argument is null and is skipped rather than blanked. Pass the argument, or drop the token from the template.

## When should I use `DomainException`?

Only where the signature is out of your control — a constructor, or an interface you did not write:

```csharp
throw Error.Validation("quantity", attemptedValue: quantity).ToException();
```

If half the failures in a codebase throw, the guarantee that a signature tells you what can go wrong is gone, and so is most of the reason to use this library.

## How do I add a language?

Copy the catalog, translate the values, register it. Partial translations are fine — anything missing falls back to the parent culture and then to the invariant catalog. See [Messages and cultures](messages.md#adding-a-language).

## Where does the culture come from?

From the request's `Accept-Language` header — first range, quality values stripped — unless you pass one explicitly. An absent, empty, `*`, or unrecognised value falls back to `CultureInfo.CurrentUICulture`. A malformed header never fails a request.

## Can I use Offside without ASP.NET Core?

Yes. `Offside` targets `netstandard2.0` and has no ASP.NET dependency. Workers, CLIs, and class libraries can return `Result` and resolve messages with `IErrorMessageResolver`; only the HTTP mapping lives in `Offside.AspNetCore`.

## Why is there no `ToActionResult(result, resolver, exposeExceptionDetails)` for the unit `Result`?

An oversight in the overload set, kept rather than changed while the library is pre-1.0. The generic `Result<T>` has it. For a unit `Result`, pass a culture explicitly or pass `null` through the options overload to fall back to `Accept-Language`.

## Is it safe to put user data in `Error.Arguments`?

Arguments feed message templates, and messages ship to clients. Identifiers and field names are fine; tokens, password hashes, and connection strings are not. Diagnostic material belongs in `Error.Unexpected(detail)`, which is sanitized out of the client response.

## Why is the repository folder called `DomainErrors`?

Historical — the project was renamed to Offside. The solution, the packages, and the namespaces are all `Offside`; only a local clone directory may still carry the old name.

## Is it production-ready?

It is pre-1.0. Minor releases may include breaking changes; see the [changelog](../CHANGELOG.md). The behaviour documented here is covered by tests across `net8.0` and `net10.0`.

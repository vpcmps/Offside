# Concepts

*[Português](pt-BR/concepts.md) · [Back to docs](README.md)*

Six ideas carry the whole library.

## Error

A domain failure described by data: a stable `Code` (the message-catalog key), an `ErrorCode` (the screen identifier), an `ErrorKind`, interpolation `Arguments`, and an optional `Field`. It is not an exception and carries no stack trace.

```csharp
var error = Error.NotFound("order", 42);
// Code      = "not_found"
// ErrorCode = "NOT_FOUND"
// Kind      = ErrorKind.NotFound
// Arguments = { resource: "order", id: 42 }
// Field     = null
```

`ErrorCode` is what clients branch on for screens. Several catalog `Code`s may share one `ErrorCode`. Omit it on a factory and `Error.DefaultErrorCode(Kind)` fills it (`NOT_FOUND`, `VALIDATION`, `TOO_MANY_REQUESTS`, …). Pass a specific value when a screen needs a finer identifier:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId }, errorCode: "ORDER_ALREADY_SHIPPED");
```

`Error` is immutable and compares by value, so it is safe to cache, compare in tests, and pass around freely:

```csharp
Error.NotFound("order", 42) == Error.NotFound("order", 42);   // true
```

Instances come only from the static factories — the constructor is internal. That is what guarantees every error in the system has a known shape and a code that a catalog can resolve.

## ErrorKind

The closed set of failure species. A kind decides two things: the **HTTP status code** and the **severity rank** used to pick a winner when a result carries several errors.

Business rules do not invent kinds. They reuse one and supply their own code:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
```

This is the central design trade. A closed kind set means the transport mapping is total — every error the domain can produce already has a defined status code, with no registry to maintain and no default branch to forget. An open code space means the domain can be as specific as it likes. See the [ASP.NET Core guide](aspnet-guide.md) for the full kind → status table.

## Result and Result&lt;T&gt;

The outcome of an operation: success (with a value, for `Result<T>`) or failure carrying one or more errors. This is how domain and application code report failure — returning, not throwing.

```csharp
Result<Order> found  = Result<Order>.Success(order);
Result<Order> missing = Result<Order>.Failure(Error.NotFound("order", id));
Result       done    = Result.Success();
```

Both are `readonly struct`s, so there is no allocation on the success path. Two consequences worth knowing:

- `default(Result)` is a **success**. A field you never assigned reads as "nothing went wrong".
- `Result.Failure()` with zero errors throws `ArgumentException`. A failure with nothing to report would be a failure nobody could act on.

Reading a value is explicit — `Value` throws on a failed result, so use `TryGetValue`, `Match`, or check `IsSuccess` first.

## Primary error

When a result carries several errors, one of them drives the response: the error of the **most severe kind**, with ties broken in favour of the **first error in the result**. It supplies the Problem Details `title`, `detail`, and `errorCode`, and its kind supplies the HTTP status.

The other errors are not discarded — every one appears in the `errors` array. A form that fails validation on three fields returns `400` and reports all three.

## Message catalog

A JSON file per culture mapping `Code` → message template. Metadata stays in C#; only text is translated.

```json
{ "not_found": "{resource} '{id}' was not found." }
```

Tokens are filled from `Error.Arguments`. Resolution walks from the requested culture to its parent to the invariant catalog, so `pt-BR` falls back to `pt` and then to the default. Details in [Messages and cultures](messages.md).

## Escape hatch

`Error.ToException()` produces a `DomainException` carrying the errors, for boundaries whose signature you do not control — a constructor, or an interface you did not write.

```csharp
if (quantity <= 0)
    throw Error.Validation("quantity", attemptedValue: quantity).ToException();
```

This is the exception, not the rule. Ordinary business failures return a `Result`. Reaching for `ToException` routinely gives up the property that makes the whole approach worth it: that a method signature tells you it can fail.

## Why not exceptions

Exceptions are control flow for the *unexpected*. A missing order, a duplicate email, an expired token — none of those are unexpected; they are outcomes the caller is supposed to handle. Modelling them as return values means:

- The signature is honest. `Result<Order> Get(string id)` says failure is possible; `Order Get(string id)` claims it is not.
- Several failures can be reported at once. An exception carries one.
- The transport mapping is data, not a chain of `catch` blocks.
- Nothing unwinds the stack for a business rule.

`ErrorKind.Unexpected` still exists for genuine faults — and it is the one kind whose detail is never shown to the client. See [500 handling](aspnet-guide.md#unexpected-errors-and-500s).

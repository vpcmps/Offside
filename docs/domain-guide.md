# Domain guide

*[Português](pt-BR/domain-guide.md) · [Back to docs](README.md)*

How to write domain and application code with `Result`. Everything on this page comes from the `Offside` package alone — no ASP.NET dependency.

## Returning a result

```csharp
using Offside;

public Result<Order> Get(string id)
{
    var order = _orders.Find(id);
    return order is null
        ? Result<Order>.Failure(Error.NotFound("order", id))
        : Result<Order>.Success(order);
}
```

For an operation with no return value, use the non-generic `Result`:

```csharp
public Result Cancel(string id)
{
    var order = _orders.Find(id);
    if (order is null)
        return Result.Failure(Error.NotFound("order", id));

    if (order.Shipped)
        return Result.Failure(Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId = id }));

    order.Cancel();
    return Result.Success();
}
```

## Error factories

Every factory produces a specific `ErrorKind` and a default catalog code.

| Factory | Kind | Code | Arguments |
|---|---|---|---|
| `Error.NotFound(resource, id?)` | `NotFound` | `not_found` | `resource`, `id` |
| `Error.Gone(resource, id?)` | `Gone` | `gone` | `resource`, `id` |
| `Error.Conflict(resource, reason?)` | `Conflict` | `conflict` | `resource`, `reason` |
| `Error.Validation(field, code?, attemptedValue?)` | `Validation` | `validation`, or `code` | `field`, `attemptedValue` |
| `Error.BadRequest(reason?)` | `BadRequest` | `bad_request` | `reason` |
| `Error.Unauthorized(reason?)` | `Unauthorized` | `unauthorized` | `reason` |
| `Error.Forbidden(reason?)` | `Forbidden` | `forbidden` | `reason` |
| `Error.PreconditionFailed(reason?)` | `PreconditionFailed` | `precondition_failed` | `reason` |
| `Error.Unprocessable(reason?)` | `Unprocessable` | `unprocessable` | `reason` |
| `Error.TooManyRequests(reason?)` | `TooManyRequests` | `too_many_requests` | `reason` |
| `Error.Unexpected(detail?)` | `Unexpected` | `unexpected` | `detail` |
| `Error.Custom(code, kind, arguments?, field?)` | *your choice* | *your choice* | *your choice* |

`Error.Validation` is the only factory that sets `Field`, and the only one that lets you override the code without going through `Custom`:

```csharp
Error.Validation("email");                                 // code "validation", field "email"
Error.Validation("email", "email.malformed", input);       // code "email.malformed", field "email"
```

## Custom errors for business rules

When a rule deserves its own message, keep the kind and invent the code:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
Error.Custom("payment.insufficient_funds", ErrorKind.Unprocessable, new { required, available });
Error.Custom("coupon.expired", ErrorKind.PreconditionFailed, new { coupon = code, expiredOn }, field: "coupon");
```

The code becomes the catalog key, so add a matching entry to every catalog:

```json
{ "order.already_shipped": "Order {orderId} has already shipped." }
```

An empty or whitespace code throws `ArgumentException`. Surrounding whitespace is trimmed.

A dotted, namespaced convention (`order.already_shipped`) is worth adopting: codes are a public contract that clients branch on, and a flat namespace collides sooner than you expect.

## Arguments

`Arguments` is a read-only snapshot taken at construction. Anonymous objects and dictionaries both work:

```csharp
Error.Custom("quota.exceeded", ErrorKind.TooManyRequests, new { limit = 100, window = "1h" });
Error.Custom("quota.exceeded", ErrorKind.TooManyRequests, new Dictionary<string, object?> { ["limit"] = 100 });
```

Two rules:

- **Arguments feed message templates.** A `{limit}` token in the catalog is filled from the `limit` entry.
- **Never put secrets in them.** They shape text that ships to the client. Tokens, password hashes, and internal connection strings do not belong there — put diagnostic material in `Error.Unexpected(detail)`, which is [sanitized on the way out](aspnet-guide.md#unexpected-errors-and-500s).

## Reading a result

`Value` throws `InvalidOperationException` on a failed result. Pick whichever of these fits:

```csharp
// Pattern match into a single value
var message = result.Match(
    order => $"Found {order.Id}",
    errors => $"Failed: {errors[0].Code}");

// Try-style
if (result.TryGetValue(out var order))
    Console.WriteLine(order.Id);

// Explicit check
if (result.IsSuccess)
    Use(result.Value);
```

## Composing

`Map` transforms a value. `Bind` chains an operation that can itself fail. Both short-circuit — the delegate never runs on a failed result, and the original errors pass through untouched:

```csharp
Result<OrderDto> dto = _orders.Get(id)
    .Bind(order => _pricing.Apply(order))   // Result<Order>
    .Map(order => OrderDto.From(order));    // plain value
```

`Result.Combine` merges independent results, concatenating errors in argument order. This is how you report every validation failure at once instead of one per round-trip:

```csharp
var combined = Result.Combine(
    ValidateEmail(request.Email),
    ValidateName(request.Name),
    ValidateAge(request.Age));

// All three failed → combined.Errors has three entries, in that order.
```

There is a `Combine<T>(params Result<T>[])` overload for value results; it merges the errors and discards the values, returning a unit `Result`.

## Deliberate omissions

Two things you may reach for and not find:

- **No implicit conversion from `T` to `Result<T>`.** Constructing a result is always explicit, so a value never becomes a success by accident — and a refactor that changes a return type produces a compile error rather than silent behaviour.
- **No `Apply`.** `Combine` covers the accumulate-all-errors case without a second, subtly different way to do it.

## The escape hatch

For boundaries that cannot return a `Result` — a constructor, an interface you do not own:

```csharp
public Order(string id, int quantity)
{
    if (quantity <= 0)
        throw Error.Validation("quantity", attemptedValue: quantity).ToException();
    ...
}
```

`DomainException.Errors` carries the errors through, so a boundary handler can still render them properly. Use it sparingly: a codebase where half the failures throw has lost the guarantee that the signature tells you what can go wrong.

## Testing

Errors compare by value, so assertions read plainly:

```csharp
var result = service.Get("missing");

Assert.True(result.IsFailure);
Assert.Equal(Error.NotFound("order", "missing"), result.Errors[0]);
```

Assert on `Code` and `Kind` rather than on resolved text — the text is catalog data and is expected to change without breaking anything.

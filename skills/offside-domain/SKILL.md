---
name: offside-domain
description: Implements domain and application failures with Offside Error, Result, and Combine. Use when writing business rules, validation, Custom errors, or converting exceptions to Result in a .NET domain layer.
---

# Offside domain errors

Business rules return `Result` / `Result<T>`. Do not throw for expected failures.

```csharp
using Offside;

public Result<Order> Get(string id)
{
    var order = _orders.Find(id);
    if (order is null)
        return Result<Order>.Failure(Error.NotFound("order", id));

    return Result<Order>.Success(order);
}
```

## Factories

| Call | Kind | Default code |
|---|---|---|
| `Error.NotFound(resource, id?)` | NotFound | `not_found` |
| `Error.Gone(resource, id?)` | Gone | `gone` |
| `Error.Conflict(resource, reason?)` | Conflict | `conflict` |
| `Error.Validation(field, code?, attempted?)` | Validation | `validation` or `code` |
| `Error.BadRequest(reason?)` | BadRequest | `bad_request` |
| `Error.Unauthorized(reason?)` | Unauthorized | `unauthorized` |
| `Error.Forbidden(reason?)` | Forbidden | `forbidden` |
| `Error.PreconditionFailed(reason?)` | PreconditionFailed | `precondition_failed` |
| `Error.Unprocessable(reason?)` | Unprocessable | `unprocessable` |
| `Error.TooManyRequests(reason?)` | TooManyRequests | `too_many_requests` |
| `Error.Unexpected(detail?)` | Unexpected | `unexpected` (`detail` is for logs, not HTTP) |
| `Error.Custom(code, kind, args?, field?, errorCode?)` | caller Kind | caller code |

`errorCode` is the screen identifier (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`). Omit it to use the Kind default. `code` remains the catalog key.

Business rule:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId }, errorCode: "ORDER_ALREADY_SHIPPED");
```

Add the same `code` key to `errors.json` / `errors.pt-BR.json`.

## Result

- `Failure()` with zero errors throws. `Value` on failure throws — use `TryGetValue` / `Match`.
- No implicit `T` → `Result<T>`.
- `Bind` / `Map` short-circuit. `Combine` merges failures (validation of several fields). There is no `Apply`.

```csharp
var combined = Result.Combine(emailResult, nameResult);
```

## Escape hatch

`error.ToException()` → `DomainException`. Not for ordinary rules.

## Layers

Domain and application: package `Offside` only. HTTP mapping belongs in the host (`offside-aspnet`).

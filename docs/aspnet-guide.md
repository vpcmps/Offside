# ASP.NET Core guide

*[Português](pt-BR/aspnet-guide.md) · [Back to docs](README.md)*

`Offside.AspNetCore` turns a `Result` into an HTTP response. It is the only layer that knows about status codes — the domain stays transport-agnostic.

## Registration

```csharp
builder.Services.AddOffside(options => { /* catalogs */ });
builder.Services.AddOffsideAspNetCore();
```

`AddOffsideAspNetCore` registers `OffsideAspNetCoreOptions`. When an `IHostEnvironment` is in the container, `ExposeExceptionDetails` defaults to `IsDevelopment()`.

## Minimal APIs

```csharp
app.MapGet("/orders/{id}", (string id, OrderService orders, HttpContext http) =>
    orders.Get(id).ToHttpResult(http));

app.MapPost("/orders", (CreateOrder cmd, OrderHandler handler, HttpContext http) =>
    handler.Handle(cmd).ToHttpResult(http));
```

The `HttpContext` overload is the one to reach for. It resolves the `IErrorMessageResolver` and the options from request services and derives the culture from the `Accept-Language` header.

## MVC controllers

```csharp
public sealed class OrdersController(OrderService orders, IErrorMessageResolver resolver) : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(string id) =>
        orders.Get(id).ToActionResult(resolver, CultureInfo.CurrentUICulture);
}
```

## Success mapping

| Result | Response |
|---|---|
| `Result.Success()` | `204 No Content` |
| `Result<T>.Success(value)` | `200 OK` with `value` as the body |

For a `201 Created` or any other success shape, branch before converting — `ToHttpResult` handles the failure path, and you keep full control of the success path:

```csharp
app.MapPost("/orders", (CreateOrder cmd, OrderHandler handler, HttpContext http) =>
{
    var result = handler.Handle(cmd);
    return result.IsSuccess
        ? Results.Created($"/orders/{result.Value.Id}", result.Value)
        : result.ToHttpResult(http);
});
```

## Failure mapping

Every failure produces the same body, `application/problem+json` with camelCase names:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Order 42 has already shipped.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    {
      "code": "order.already_shipped",
      "kind": "Conflict",
      "detail": "Order 42 has already shipped.",
      "field": null
    }
  ]
}
```

| Field | Meaning |
|---|---|
| `type` | `https://httpstatuses.io/{status}` |
| `title` | The primary error's `ErrorKind`, as a string |
| `status` | Derived from the most severe kind present |
| `detail` | The primary error's resolved message |
| `traceId` | `Activity.Current?.Id`, falling back to `HttpContext.TraceIdentifier` |
| `errors` | Every error in the result, in the order the domain reported them |
| `debug` | Present only on a 500 with `ExposeExceptionDetails` enabled; omitted otherwise |

Clients should branch on `errors[].code`, not on `detail` — the code is the contract, the text is catalog data.

## Status codes

| ErrorKind | Status |
|---|---|
| `Unexpected` | 500 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `TooManyRequests` | 429 |
| `Conflict` | 409 |
| `PreconditionFailed` | 412 |
| `Gone` | 410 |
| `Unprocessable` | 422 |
| `NotFound` | 404 |
| `Validation` | 400 |
| `BadRequest` | 400 |

## Choosing the primary error

When a result carries several errors, the response reflects the **most severe kind**, not the first error. Severity, most severe first:

| Rank | Kinds |
|---|---|
| 0 | `Unexpected` |
| 1 | `Unauthorized`, `Forbidden` |
| 2 | `TooManyRequests` |
| 3 | `Conflict` |
| 4 | `PreconditionFailed` |
| 5 | `Gone` |
| 6 | `Unprocessable` |
| 7 | `NotFound` |
| 8 | `Validation`, `BadRequest` |

**Ties go to the first error in the result.** `Unauthorized` and `Forbidden` share rank 1, so a result carrying both reports whichever the domain listed first.

```csharp
Result.Failure(
    Error.Validation("email"),          // 400
    Error.Conflict("order", "dup"),     // 409  ← most severe, wins
    Error.NotFound("order", 1));        // 404
// → status 409, title "Conflict", and all three errors in the errors array
```

Ordering by severity rather than by position means a genuine fault is never masked by a validation message that happened to be added first. Nothing is lost either way: the full list always ships.

## Unexpected errors and 500s

`ErrorKind.Unexpected` is handled differently, because its detail is diagnostic material rather than something a client should read.

When the winning kind is `Unexpected`:

1. Every unexpected error's `detail` is replaced with the generic `unexpected` message from the catalog — both the top-level `detail` and the entries in `errors`.
2. The real detail appears in `debug` **only** when `ExposeExceptionDetails` is enabled.
3. The failure is logged through `ILoggerFactory` under the category `Offside.AspNetCore`, together with the `traceId`.

```csharp
return Result.Failure(Error.Unexpected(ex.ToString()));
```

In production:

```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Unexpected",
  "status": 500,
  "detail": "An unexpected error occurred.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "code": "unexpected", "kind": "Unexpected", "detail": "An unexpected error occurred.", "field": null }
  ]
}
```

In development, the same response gains a `"debug": "System.InvalidOperationException: ..."` field. The client-facing `detail` is generic in both cases — `ExposeExceptionDetails` gates `debug` only.

The `traceId` is the bridge: it appears in the response and in the log line, so a user can quote it and you can find the real cause.

Set it explicitly if you do not want to depend on the environment. `OffsideAspNetCoreOptions` is a plain singleton, so register your own instance instead of calling `AddOffsideAspNetCore`:

```csharp
builder.Services.AddSingleton(new OffsideAspNetCoreOptions { ExposeExceptionDetails = false });
```

Or construct it directly at the call site:

```csharp
result.ToHttpResult(resolver, culture: null, new OffsideAspNetCoreOptions { ExposeExceptionDetails = false });
```

## Cultures

When no culture is passed, it comes from the request's `Accept-Language` header — the first range, with any quality value stripped. `Accept-Language: pt-BR,pt;q=0.9` resolves to `pt-BR`, which falls back to `pt` and then to the invariant catalog.

The header falls back to `CultureInfo.CurrentUICulture` when it is absent, empty, `*`, or not a recognised culture name. A malformed header never fails a request.

See [Messages and cultures](messages.md) for catalog resolution.

## Overload reference

| Method | Culture | Options |
|---|---|---|
| `ToHttpResult(resolver, exposeExceptionDetails?)` | `CurrentUICulture` | flag |
| `ToHttpResult(resolver, culture, exposeExceptionDetails?)` | explicit | flag |
| `ToHttpResult(resolver, culture?, options)` | explicit or `Accept-Language` | object |
| `ToHttpResult(httpContext)` | `Accept-Language` | from DI |
| `ToActionResult(resolver, culture, exposeExceptionDetails?)` | explicit | flag |
| `ToActionResult(resolver, culture?, options)` | explicit or `Accept-Language` | object |

Each row exists for both `Result` and `Result<T>`, with one exception: **there is no `ToActionResult(resolver, exposeExceptionDetails?)` for the non-generic `Result`.** The generic form has it; the unit form does not. Pass a culture explicitly, or pass `null` through the options overload to fall back to `Accept-Language`.

## Rules of thumb

- Never reference `Offside.AspNetCore` from a domain or application project. Status codes are a transport concern.
- Do not build a second error shape alongside this one. One shape across the API is most of the value.
- Keep secrets out of `Error.Arguments` — they end up in messages, and messages ship.
- Branch clients on `errors[].code`, never on `detail`.

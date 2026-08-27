# ASP.NET Core guide

*[Português](pt-BR/aspnet-guide.md) · [Back to docs](README.md)*

`Offside.AspNetCore` turns a `Result` into an HTTP response. It is the only layer that knows about status codes — the domain stays transport-agnostic.

## Registration

```csharp
builder.Services.AddOffside(options => { /* catalogs */ });
builder.Services.AddOffsideAspNetCore();
```

`AddOffsideAspNetCore` registers `OffsideAspNetCoreOptions`. When an `IHostEnvironment` is in the container, `ExposeExceptionDetails` defaults to `IsDevelopment()`. Pass a configure callback to set hooks afterwards — it wins over the environment default:

```csharp
builder.Services.AddOffsideAspNetCore(options =>
{
    options.LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail;
    options.TelemetryProperties = (problem, errors, http) =>
        new Dictionary<string, string> { ["Operation"] = http.Request.Path };
});
```

When `IDomainErrorRecorder` is registered (`AddOffsideOpenTelemetry` or `AddOffsideApplicationInsights`), the pipeline records according to `RecordMode` — one event per error by default, or only the error that drives the status with `PrimaryErrorOnly`. `OnProblem` is not telemetry.

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
  "errorCode": "ORDER_ALREADY_SHIPPED",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": [
    {
      "code": "order.already_shipped",
      "errorCode": "ORDER_ALREADY_SHIPPED",
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
| `errorCode` | The primary error's screen identifier |
| `traceId` | `Activity.Current.TraceId` (32 hex), falling back to `HttpContext.TraceIdentifier`. Override with `ResolveTraceId` |
| `errors` | Every error in the result, in the order the domain reported them |
| `errors[].code` | Catalog key (`order.already_shipped`) |
| `errors[].errorCode` | Screen identifier (`ORDER_ALREADY_SHIPPED`) |
| `debug` | Present only on a 500 with `ExposeExceptionDetails` enabled; omitted otherwise |

Extra fields added through `CustomizeProblem` are flattened into the document (and into `errors[]`) the same way ASP.NET `ProblemDetails.Extensions` works. Keys that collide with the contract (`type`, `title`, `status`, `detail`, `instance`, `traceId`, `errorCode`, `debug`, `errors`) are stripped. Use JSON-safe primitives.

Clients should branch on `errorCode` (top-level or `errors[].errorCode`), not on `detail`. `code` is the message-catalog key.

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
| `ServiceUnavailable` | 503 |
| `Timeout` | 504 |

The same mapping is `OffsideHttp.StatusCode(kind)`. `OffsideHttp.StatusCodes` is the distinct set (400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504) used as expected responses. `OffsideHttp.SelectPrimary` picks the error that drives the status when you write a custom response.

## Choosing the primary error

When a result carries several errors, the response reflects the **most severe kind**, not the first error. Severity, most severe first:

| Rank | Kinds |
|---|---|
| 0 | `Unexpected` |
| 1 | `Unauthorized`, `Forbidden` |
| 2 | `TooManyRequests` |
| 3 | `ServiceUnavailable`, `Timeout` |
| 4 | `Conflict` |
| 5 | `PreconditionFailed` |
| 6 | `Gone` |
| 7 | `Unprocessable` |
| 8 | `NotFound` |
| 9 | `Validation`, `BadRequest` |

**Ties go to the first error in the result.** `Unauthorized` and `Forbidden` share rank 1, so a result carrying both reports whichever the domain listed first. Auth and rate-limit win over 503/504 so a client is not told to retry a request that is unauthorized or throttled.

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
2. Every unexpected error's `errorCode` is forced to `UNEXPECTED`.
3. The real detail appears in `debug` **only** when `ExposeExceptionDetails` is enabled.
4. The failure is logged through `ILoggerFactory` under the category `Offside.AspNetCore`, together with the `traceId`, unless `LogUnexpected` is `false`. When a recorder is registered, this line defaults off so a 500 is not duplicated. Set `LogUnexpected = true` to keep both.

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
  "errorCode": "UNEXPECTED",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": [
    { "code": "unexpected", "errorCode": "UNEXPECTED", "kind": "Unexpected", "detail": "An unexpected error occurred.", "field": null }
  ]
}
```

In development, the same response gains a `"debug": "System.InvalidOperationException: ..."` field. The client-facing `detail` is generic in both cases — `ExposeExceptionDetails` gates `debug` only.

The `traceId` is the bridge: it appears in the response and in the log line, so a user can quote it and you can find the real cause. The default is the 32-hex W3C `TraceId` (the value Application Insights stores as `operation_Id`), not the full `Activity.Id` traceparent. Restore the old format with `ResolveTraceId`:

```csharp
builder.Services.AddOffsideAspNetCore(options =>
{
    options.ResolveTraceId = http =>
        Activity.Current?.Id ?? http.TraceIdentifier;
});
```

Set options explicitly if you do not want to depend on the environment. Prefer the configure callback on `AddOffsideAspNetCore`; registering your own singleton still works:

```csharp
builder.Services.AddSingleton(new OffsideAspNetCoreOptions { ExposeExceptionDetails = false });
```

Or construct it directly at the call site (this form has no DI hooks unless you put them on the object):

```csharp
result.ToHttpResult(resolver, culture: null, new OffsideAspNetCoreOptions { ExposeExceptionDetails = false });
```

## Recording failures

The host configures the recorder once. `ToHttpResult`, `ToActionResult`, and `SendOffsideAsync` do the rest — there is no `RecordTo` at the HTTP call site. Workers and MediatR handlers without `HttpContext` still call `RecordTo`.

Extra dimensions (an operation name, a tenant) come from `TelemetryProperties`. The pipeline always writes `HttpStatus`. Offside dimensions still win inside the recorder.

The default `RecordMode` is `PerError`: N invalid fields produce N traces, span events, and counter increments. Hosts that alert on request failure, not on each field, opt in:

```csharp
options.RecordMode = ProblemRecordMode.PrimaryErrorOnly;
```

That reuses `OffsideHttp.SelectPrimary`. `RecordTo` on a worker or MediatR handler stays one event per error; the mode applies only to the HTTP pipeline (`ToHttpResult`, `ToActionResult`, `SendOffsideAsync`).

`OnProblem` does not emit telemetry. It remains a host hook for anything else that should run after the document is built.

## Legacy aliases

Hosts that cannot change the client yet can flatten the old field names onto the document:

```csharp
builder.Services.AddOffsideAspNetCore(options =>
    options.LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail);
```

That adds `message` (= `detail`), per-item `reason` (= `detail`), per-item `name` (= `field` when the error has one, otherwise `LegacyGeneralErrorName`, default `"generalErrors"` — the FastEndpoints sentinel for errors not attributable to a field), and `technicalDetail` only when `debug` is present (Unexpected + `ExposeExceptionDetails`). It is omitted otherwise; it never copies the business `detail`. Set `LegacyGeneralErrorName` to null or empty to omit `name` on field-less errors. `Field` itself stays null — the sentinel is the alias only. `CustomizeProblem` stays for rarer extensions.

## Customizing the document and observing failures

`CustomizeProblem` runs after the document is built. Core properties stay init-only; add host-specific fields through `Extensions` (and `Item.Extensions`). Keep values JSON-safe. A throwing callback is logged under `Offside.AspNetCore` and the problem document is still written.

```csharp
builder.Services.AddOffsideAspNetCore(options =>
{
    options.CustomizeProblem = (problem, errors) =>
    {
        problem.Extensions["correlation"] = "…";
    };
    options.OnProblem = (problem, errors, http) =>
    {
        // host-specific work — not domain-error telemetry
    };
});
```

The FastEndpoints validation `ResponseBuilder` uses the same pipeline, so hooks, aliases, and the 32-hex `traceId` apply there too.

## Cultures

When no culture is passed, it comes from the request's `Accept-Language` header — the first range, with any quality value stripped. `Accept-Language: pt-BR,pt;q=0.9` resolves to `pt-BR`, which falls back to `pt` and then to the invariant catalog.

The header falls back to `CultureInfo.CurrentUICulture` when it is absent, empty, `*`, or not a recognised culture name. A malformed header never fails a request.

See [Messages and cultures](messages.md) for catalog resolution.

## Overload reference

| Method | Culture | Options |
|---|---|---|
| `ToHttpResult(resolver, exposeExceptionDetails?)` | `CurrentUICulture` | flag — **obsolete**, builds empty options |
| `ToHttpResult(resolver, culture, exposeExceptionDetails?)` | explicit | flag — **obsolete**, builds empty options |
| `ToHttpResult(resolver, culture?, options)` | explicit or `Accept-Language` | object |
| `ToHttpResult(httpContext)` | `Accept-Language` | from DI; throws if `AddOffsideAspNetCore` was not called |
| `ToActionResult(resolver, culture, exposeExceptionDetails?)` | explicit | flag — **obsolete**, builds empty options |
| `ToActionResult(resolver, culture?, options)` | explicit or `Accept-Language` | object |

Each row exists for both `Result` and `Result<T>`, with one exception: **there is no `ToActionResult(resolver, exposeExceptionDetails?)` for the non-generic `Result`.** The generic form has it; the unit form does not. Pass a culture explicitly, or pass `null` through the options overload to fall back to `Accept-Language`.

## Rules of thumb

- Never reference `Offside.AspNetCore` from a domain or application project. Status codes are a transport concern.
- Do not build a second error shape alongside this one. One shape across the API is most of the value.
- Keep secrets out of `Error.Arguments` — they end up in messages, and messages ship.
- Branch clients on `errorCode`, never on `detail`.
- Put operational dependency failures in `ErrorKind.ServiceUnavailable` / `Timeout`, not `Unexpected`. Do not put exception text in `{reason}` templates.

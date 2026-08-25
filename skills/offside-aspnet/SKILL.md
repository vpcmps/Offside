---
name: offside-aspnet
description: Maps Offside Result to RFC 7807 Problem Details in ASP.NET Core. Use when adding ToHttpResult, ToActionResult, AddOffsideAspNetCore, or HTTP error responses for domain failures.
---

# Offside ASP.NET

The host translates `Result` to Problem Details. It does not decide business rules.

Use this skill after standard ASP.NET Core exposure is selected. If the exposure or message source is undecided, use `offside-setup` first. FastEndpoints hosts use `offside-fastendpoint` for pipeline integration.

Requires `Offside` + `Offside.AspNetCore`, catalogs via `AddOffside`, then `AddOffsideAspNetCore()`.

## Map endpoints

```csharp
app.MapPost("/orders", (CreateOrder cmd, HttpContext http) =>
    _handler.Handle(cmd).ToHttpResult(http));
```

`ToHttpResult(HttpContext)` reads `IErrorMessageResolver` and `OffsideAspNetCoreOptions` from DI. Culture comes from `Accept-Language` (invalid/`*` → `CurrentUICulture`).

Controllers: `ToActionResult` / `ToActionResult<T>`.

Success: `Result` → 204, `Result<T>` → 200 + value.

## Response shape

Always `application/problem+json`:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "primary error message",
  "errorCode": "ORDER_ALREADY_SHIPPED",
  "traceId": "...",
  "errors": [{ "code": "order.already_shipped", "errorCode": "ORDER_ALREADY_SHIPPED", "kind": "Conflict", "detail": "...", "field": null }]
}
```

Status = most severe `ErrorKind`. Tie (Unauthorized/Forbidden, Validation/BadRequest) = first error in the list.

Clients branch on `errorCode` for screens. `code` is the message-catalog key.

Severity (high → low): Unexpected → Unauthorized/Forbidden → TooManyRequests → ServiceUnavailable/Timeout → Conflict → PreconditionFailed → Gone → Unprocessable → NotFound → Validation/BadRequest.

## 500

Winning Kind `Unexpected`: generic `detail` (JSON template `unexpected`, no secret args). `errorCode` is forced to `UNEXPECTED`. Optional `debug` only when `ExposeExceptionDetails` is true (default `IsDevelopment()`). Log the real detail via `ILogger` when `LogUnexpected` is true (default). Use `OnProblem` for host telemetry.

## Do not

- Reference `Offside.AspNetCore` from Domain.
- Return `ValidationProblemDetails` in parallel — one shape only.
- Put secrets in `Error.Arguments` that are interpolated into client messages.

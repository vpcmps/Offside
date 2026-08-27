---
name: offside-fastendpoint
description: Wires Offside Problem Details into FastEndpoints. Use when adding UseOffside, SendOffsideAsync, DontProduceOffside, or expected error metadata on FastEndpoints endpoints.
---

# Offside FastEndpoints

Everything FastEndpoints-specific lives in `Offside.FastEndpoint`. The host also needs `Offside` and `Offside.AspNetCore`, plus the selected message resolver.

Use this skill after FastEndpoints exposure is selected. If exposure or message source is undecided, use `offside-setup` first. The message source may be local JSON, Azure App Configuration, or a custom resolver.

```csharp
builder.Services.AddOffside(options => { /* local JSON catalogs, when selected */ });
builder.Services.AddOffsideAspNetCore();
builder.Services.AddFastEndpoints();

app.UseFastEndpoints(c => c.UseOffside());
```

`UseOffside`:

- Validation failures become `OffsideProblem` (`application/problem+json`).
- OpenAPI documents `OffsideProblem` as the error DTO.
- Every endpoint gets `Produces<OffsideProblem>` for Offside statuses (400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504).

Pass extra endpoint setup into `UseOffside` if you already have a configurator — FastEndpoints does not expose the previous callback:

```csharp
app.UseFastEndpoints(c => c.UseOffside(ep => ep.AllowAnonymous()));
```

Opt out per endpoint:

```csharp
public override void Configure()
{
    Get("/health");
    Definition.DontProduceOffside();
}
```

Send a domain `Result`:

```csharp
public override Task HandleAsync(CancellationToken ct) =>
    orders.Get(id).SendOffsideAsync(HttpContext, ct);
```

`SendOffsideAsync` reuses `ToHttpResult`. If `IDomainErrorRecorder` is registered, the pipeline records the failure — do not call `RecordTo` at the endpoint.

FluentValidation `.WithErrorCode` values are catalog keys (`Error.Code`). Screen routing uses `errorCode` (`VALIDATION`, `NOT_FOUND`, …). FastEndpoints `AddError`/`ThrowError` without a property set `PropertyName` to `"GeneralErrors"`; Offside maps that to `field: null`. With legacy aliases on, `errors[].name` is `"generalErrors"`.

## Do not

- Put FastEndpoints types in `Offside.AspNetCore` or the domain.
- Set `c.Endpoints.Configurator` after `UseOffside` — that replaces the Offside metadata.

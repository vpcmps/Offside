# Application Insights integration

*[Português](pt-BR/application-insights.md) · [Back to docs](README.md)*

A domain failure that becomes a Problem Details response leaves no trace in your logs — it was never an exception. `Offside.ApplicationInsights` records `Error` values as Application Insights traces, with a severity derived from the `ErrorKind` and stable dimensions you can filter on in Kusto.

## Install and register

```bash
dotnet add package Offside
dotnet add package Offside.ApplicationInsights
```

Configure Application Insights in the host first, then the integration:

```csharp
using Offside.ApplicationInsights;

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddOffsideApplicationInsights();
```

`AddOffsideApplicationInsights` registers `IDomainErrorRecorder` over the host's `TelemetryClient`. It never reads a connection string or calls `AddApplicationInsightsTelemetry` itself.

The trace message is the resolved catalog message, taken from the `IErrorMessageResolver` that `AddOffside` registered. Without one, the error's `Code` is written instead.

## Record a result

```csharp
public async Task<IResult> Cancel(string id)
{
    var result = _orders.Cancel(id).RecordTo(_recorder);
    return result.ToHttpResult();
}
```

`RecordTo` writes one trace per error, in result order, and returns the result unchanged so it can sit in a chain. A successful result records nothing. Extra dimensions are merged in:

```csharp
result.RecordTo(_recorder, new Dictionary<string, string> { ["tenant"] = tenantId });
```

Offside dimensions always win over supplied ones, so a tenant key can never rewrite `offside.kind`.

## What a trace looks like

| Dimension | Value |
|---|---|
| `offside.code` | The catalog key — `order.already_shipped` |
| `offside.errorCode` | The screen identifier — `ORDER_ALREADY_SHIPPED` |
| `offside.kind` | The failure species — `Conflict` |
| `offside.field` | The offending field, when the error has one |

Severity comes from the kind:

| Kind | Severity |
|---|---|
| `Unexpected` | `Critical` |
| `ServiceUnavailable`, `Timeout` | `Error` |
| `Unauthorized`, `Forbidden`, `TooManyRequests`, `Conflict`, `PreconditionFailed`, `Gone`, `Unprocessable` | `Warning` |
| `NotFound`, `Validation`, `BadRequest` | `Information` |

The point of the split: a validation failure is the system working, and should not page anyone; a 500 or a dependency outage should. Replace the whole map with `options.SeverityFor` when your operations team draws the line elsewhere.

A Kusto query over the result:

```kusto
traces
| where customDimensions["offside.kind"] == "Conflict"
| summarize count() by tostring(customDimensions["offside.errorCode"])
```

## Arguments and PII

`Error.Arguments` are **not** written by default. They carry whatever the domain put in them — identifiers, attempted values, a reason from a dependency — and telemetry outlives a request by months. Turn them on only when you know every argument is safe:

```csharp
builder.Services.AddOffsideApplicationInsights(options => options.IncludeArguments = true);
```

They then appear as `offside.arg.{name}`; null arguments are skipped.

## Options

| Option | Default | What it does |
|---|---|---|
| `PropertyPrefix` | `offside.` | Prefix of every Offside dimension |
| `IncludeArguments` | `false` | Writes `Error.Arguments` as dimensions |
| `Culture` | `InvariantCulture` | Culture the trace message is resolved in — deliberately not the request culture, so logs stay in one language |
| `SeverityFor` | The table above | Chooses the severity for a kind |

## With MediatR

If you already publish failures as domain notifications, `Offside.ApplicationInsights.MediatR` records each one — no call site changes at all:

```bash
dotnet add package Offside.ApplicationInsights.MediatR
```

```csharp
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();                    // the scoped collector
builder.Services.AddOffsideApplicationInsights();        // the recorder
builder.Services.AddOffsideApplicationInsightsMediatR(); // the bridge
```

`AddOffsideApplicationInsightsMediatR` is idempotent, and the bridge runs alongside the collector — neither replaces the other. See the [MediatR guide](mediatr-guide.md) for publishing.

## With Refit

`Offside.Refit` exposes `IExternalApiErrorObserver` for failures seen on the wire. A small adapter forwards them here; see [Observing failures on the wire](refit.md#observing-failures-on-the-wire).

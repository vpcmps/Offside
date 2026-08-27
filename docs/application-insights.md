# Application Insights integration

*[Português](pt-BR/application-insights.md) · [Back to docs](README.md)*

A domain failure that becomes a Problem Details response leaves no trace in your logs — it was never an exception. `Offside.ApplicationInsights` records `Error` values as Application Insights traces, with a severity derived from the `ErrorKind` and stable dimensions you can filter on in Kusto.

This package targets the classic `Microsoft.ApplicationInsights` SDK and needs the host's `TelemetryClient`. If your host is instrumented with `Azure.Monitor.OpenTelemetry.AspNetCore` instead, there is no `TelemetryClient` to resolve — use [`Offside.OpenTelemetry`](open-telemetry.md). The two are alternatives, not layers, and they agree on severity kind for kind.

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

`AddOffsideApplicationInsights` registers `IDomainErrorRecorder` (namespace `Offside`) over the host's `TelemetryClient`. It never reads a connection string or calls `AddApplicationInsightsTelemetry` itself.

The trace message is the resolved catalog message, taken from the `IErrorMessageResolver` that `AddOffside` registered. Without one, the error's `Code` is written instead.

Do not register this package and `Offside.OpenTelemetry` in the same host — they are alternatives, and they share the same `IDomainErrorRecorder` interface.

## Record a result

On an HTTP host, register the recorder and call `ToHttpResult` / `SendOffsideAsync`. The pipeline records according to `RecordMode` (one event per error by default). `RecordTo` at the endpoint is redundant. Hosts that alert on request failure, not on each field, set `RecordMode = ProblemRecordMode.PrimaryErrorOnly` on `AddOffsideAspNetCore`. `RecordTo` on a worker or MediatR handler is always one event per error.

```csharp
app.MapPost("/orders/{id}/cancel", (string id, HttpContext http) =>
    _orders.Cancel(id).ToHttpResult(http));
```

Workers, MediatR handlers, and any path without `HttpContext` still call `RecordTo`:

```csharp
var result = _orders.Cancel(id).RecordTo(_recorder);
```

`RecordTo` writes one trace per error, in result order, and returns the result unchanged so it can sit in a chain. A successful result records nothing. Extra dimensions are merged in:

```csharp
result.RecordTo(_recorder, new Dictionary<string, string> { ["tenant"] = tenantId });
```

HTTP extra dimensions come from `OffsideAspNetCoreOptions.TelemetryProperties` instead. Offside dimensions always win over supplied ones, so a tenant key can never rewrite `offside.kind`. See [Querying domain errors](queries.md) for Kusto.

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

The point of the split: a validation failure is the system working, and should not page anyone; a 500 or a dependency outage should. That map is `DomainErrorSeverityMap.Library`, the default. An operations view that still wants 404/400 in Warning uses the other preset:

```csharp
builder.Services.AddOffsideApplicationInsights(options =>
    options.SeverityFor = DomainErrorSeverityMap.Operations);
```

`Operations` raises NotFound / Validation / BadRequest to Warning and drops Unexpected from Critical to Error. Outages stay Error. Replace the whole map with `options.SeverityFor` when your team draws the line elsewhere.

A Kusto query over the result is in [Querying domain errors](queries.md).

## The trace text

By default the trace text is the resolved catalog message on its own. The code, the kind, and the field travel as dimensions, so nothing is lost — and in Kusto those dimensions are queryable without crowding every rendered line.

That trade-off flips when a human reads the lines raw — a console, a container log, a `kubectl logs` tail — where nothing renders the dimensions:

```csharp
builder.Services.AddOffsideApplicationInsights(options =>
    options.FormatMessage = DomainErrorMessageFormat.CodePrefixed);
```

```
[order.already_shipped] Order already shipped.
```

Three formats ship:

| Format | Line |
|---|---|
| `MessageOnly` (default) | `Order already shipped.` |
| `CodePrefixed` | `[order.already_shipped] Order already shipped.` |
| `ErrorCodePrefixed` | `[ORDER_ALREADY_SHIPPED] Order already shipped.` |

`ErrorCodePrefixed` earns its place when support reads logs against the identifier a user reports from the screen.

Any `Func<Error, string, string>` works — the error, and its already-resolved message:

```csharp
options.FormatMessage = (error, message) => $"{error.Kind}/{error.Code}: {message}";
```

The format shapes the trace text and nothing else: the dimensions are untouched by it, so a shorter line never costs you a filter. `Offside.OpenTelemetry` offers the same three formats under the same names, and a test fails if the two ever render differently.

## Arguments and PII

`Error.Arguments` are **not** written by default. They carry whatever the domain put in them — identifiers, attempted values, a reason from a dependency — and telemetry outlives a request by months. Turn them on only when you know every argument is safe:

```csharp
builder.Services.AddOffsideApplicationInsights(options => options.IncludeArguments = true);
```

They then appear as `offside.arg.{name}`; null arguments are skipped.

Prefer an allowlist when only a few keys are safe:

```csharp
builder.Services.AddOffsideApplicationInsights(options =>
    options.IncludeArgumentKeys = ["rejectionReason"]);
```

`IncludeArguments = true` ignores the list and writes every argument.

## Options

| Option | Default | What it does |
|---|---|---|
| `PropertyPrefix` | `offside.` | Prefix of every Offside dimension |
| `IncludeArguments` | `false` | Writes every `Error.Arguments` value as a dimension |
| `IncludeArgumentKeys` | empty | Writes only the named arguments; ignored when `IncludeArguments` is true |
| `Culture` | `InvariantCulture` | Culture the trace message is resolved in — deliberately not the request culture, so logs stay in one language |
| `SeverityFor` | `DomainErrorSeverityMap.Library` | Chooses the severity for a kind |
| `FormatMessage` | `MessageOnly` | Builds the trace text from the error and its resolved message |

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

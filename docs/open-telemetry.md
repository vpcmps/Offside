# OpenTelemetry integration

*[Português](pt-BR/open-telemetry.md) · [Back to docs](README.md)*

A domain failure that becomes a Problem Details response leaves no trace in your logs — it was never an exception. `Offside.OpenTelemetry` emits `Error` values through the OpenTelemetry signals your host already collects: a structured log entry, an event on the span in scope, and a counter.

Use this package when your host is instrumented with `Azure.Monitor.OpenTelemetry.AspNetCore`, the OpenTelemetry SDK with an OTLP exporter, or any other collector. Use [`Offside.ApplicationInsights`](application-insights.md) instead when your host still runs the classic `Microsoft.ApplicationInsights` SDK — the two are alternatives, not layers.

## Install and register

```bash
dotnet add package Offside
dotnet add package Offside.OpenTelemetry
```

Set up the pipeline in the host first, then the integration:

```csharp
using Offside.OpenTelemetry;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(OffsideTelemetry.MeterName))
    .UseAzureMonitor();

builder.Services.AddOffsideOpenTelemetry();
```

`AddOffsideOpenTelemetry` registers `IDomainErrorRecorder` over the host's `ILoggerFactory`. It configures neither OpenTelemetry nor an exporter, and never reads a connection string.

**`AddMeter(OffsideTelemetry.MeterName)` is not optional if you want the counter.** A meter no pipeline listens to is silently discarded — the most common reason for "I registered it and see nothing".

There is no activity source to register: the package never starts a span of its own. It attaches an event to whichever activity the host's instrumentation already has in scope, so ASP.NET Core instrumentation is enough.

The log message is the resolved catalog message, taken from the `IErrorMessageResolver` that `AddOffside` registered. Without one, the error's `Code` is written instead.

## Record a result

```csharp
public async Task<IResult> Cancel(string id)
{
    var result = _orders.Cancel(id).RecordTo(_recorder);
    return result.ToHttpResult();
}
```

`RecordTo` records one error at a time, in result order, and returns the result unchanged so it can sit in a chain. A successful result records nothing. Extra dimensions are merged in:

```csharp
result.RecordTo(_recorder, new Dictionary<string, string> { ["tenant"] = tenantId });
```

Offside dimensions always win over supplied ones, so a tenant key can never rewrite `offside.kind`.

## The three signals

| Signal | Where it lands | Carries |
|---|---|---|
| Log entry, category `Offside` | `traces` in Application Insights, or your log backend | Every dimension below |
| `offside.error` event on the current activity | The span of the request that failed | Every dimension below |
| `offside.errors` counter | `customMetrics`, or your metrics backend | `offside.kind` and `offside.code` only |

Dimensions:

| Dimension | Value |
|---|---|
| `offside.code` | The catalog key — `order.already_shipped` |
| `offside.errorCode` | The screen identifier — `ORDER_ALREADY_SHIPPED` |
| `offside.kind` | The failure species — `Conflict` |
| `offside.field` | The offending field, when the error has one |

**The counter deliberately carries less.** Field, arguments, and caller-supplied dimensions are unbounded; every distinct combination is a separate time series to store and query. Log entries and span events are per-occurrence and can afford the detail — a counter cannot.

## The log message

By default the log line is the resolved catalog message on its own. The code, the kind, and the field travel as dimensions, so nothing is lost — and on any OpenTelemetry backend those dimensions are queryable without crowding every rendered line.

That trade-off flips when a human reads the lines raw — a console, a container log, a `kubectl logs` tail — where nothing renders the dimensions:

```csharp
builder.Services.AddOffsideOpenTelemetry(options =>
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

The format shapes the log line and nothing else: the dimensions, the span event, and the counter are untouched by it, so a shorter line never costs you a filter.

## Severity

Severity comes from the kind, and maps onto `LogLevel`:

| Kind | Severity | `LogLevel` |
|---|---|---|
| `Unexpected` | `Critical` | `Critical` |
| `ServiceUnavailable`, `Timeout` | `Error` | `Error` |
| `Unauthorized`, `Forbidden`, `TooManyRequests`, `Conflict`, `PreconditionFailed`, `Gone`, `Unprocessable` | `Warning` | `Warning` |
| `NotFound`, `Validation`, `BadRequest` | `Information` | `Information` |

The point of the split: a validation failure is the system working, and should not page anyone; a 500 or a dependency outage should. Replace the whole map with `options.SeverityFor` when your operations team draws the line elsewhere.

This table is identical to the one in `Offside.ApplicationInsights`, and a test in the repository fails if the two ever drift. Moving a host from the classic SDK to OpenTelemetry does not change what its alerts fire on.

A Kusto query over the result:

```kusto
traces
| where customDimensions["offside.kind"] == "Conflict"
| summarize count() by tostring(customDimensions["offside.errorCode"])
```

## Span status

Recording an error leaves the span's status alone by default. A domain failure is often a perfectly successful request — a 404 answered correctly is not a broken operation, and marking it failed distorts your error rate.

Where a recorded error really does mean the span failed:

```csharp
builder.Services.AddOffsideOpenTelemetry(options =>
{
    options.SetActivityStatusOnError = true;
    options.MinimumSeverityForActivityFailure = DomainErrorSeverity.Error; // the default
});
```

Only errors at or above that severity mark the activity. With the default threshold, a `NotFound` still leaves the span successful while an `Unexpected` does not.

## Arguments and PII

`Error.Arguments` are **not** written by default. They carry whatever the domain put in them — identifiers, attempted values, a reason from a dependency — and telemetry outlives a request by months. Turn them on only when you know every argument is safe:

```csharp
builder.Services.AddOffsideOpenTelemetry(options => options.IncludeArguments = true);
```

They then appear as `offside.arg.{name}` on the log entry and the span event; null arguments are skipped. They never reach the counter, whatever this is set to.

## Options

| Option | Default | What it does |
|---|---|---|
| `PropertyPrefix` | `offside.` | Prefix of every Offside dimension |
| `IncludeArguments` | `false` | Writes `Error.Arguments` as dimensions |
| `Culture` | `InvariantCulture` | Culture the message is resolved in — deliberately not the request culture, so logs stay in one language |
| `SeverityFor` | The table above | Chooses the severity for a kind |
| `FormatMessage` | `MessageOnly` | Builds the log line from the error and its resolved message |
| `EmitLog` | `true` | Writes the log entry |
| `EmitActivityEvent` | `true` | Adds the event to the activity in scope |
| `EmitMetric` | `true` | Increments the counter |
| `SetActivityStatusOnError` | `false` | Marks the activity failed for severe errors |
| `MinimumSeverityForActivityFailure` | `Error` | The severity that counts as severe, when the above is on |

Each of the three `Emit*` switches is independent — turning one off leaves the other two untouched.

## With MediatR

If you already publish failures as domain notifications, `Offside.OpenTelemetry.MediatR` records each one — no call site changes at all:

```bash
dotnet add package Offside.OpenTelemetry.MediatR
```

```csharp
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();              // the scoped collector
builder.Services.AddOffsideOpenTelemetry();        // the recorder
builder.Services.AddOffsideOpenTelemetryMediatR(); // the bridge
```

`AddOffsideOpenTelemetryMediatR` is idempotent, and the bridge runs alongside the collector — neither replaces the other. See the [MediatR guide](mediatr-guide.md) for publishing.

## With Refit

`Offside.Refit` exposes `IExternalApiErrorObserver` for failures seen on the wire. A small adapter forwards them here; see [Observing failures on the wire](refit.md#observing-failures-on-the-wire).

# Refit integration

*[Português](pt-BR/refit.md) · [Back to docs](README.md)*

`Offside.Refit` covers the outbound edge: when a dependency answers with a failure, Refit throws an `ApiException`, and this package turns it into the same `Error` values your own domain produces. The core package stays free of Refit.

## Install and register

```bash
dotnet add package Offside
dotnet add package Offside.Refit
```

Configure your Refit clients as usual, then register the integration:

```csharp
using Offside.Refit;

builder.Services.AddRefitClient<IPaymentsApi>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://payments.example"));

builder.Services.AddOffsideRefit(options => options.ApiName = "payments");
```

`AddOffsideRefit` registers the mapping options and `IExternalApiCaller`. It does not create Refit clients, configure `HttpClient`, or add resilience policies — that stays in the host.

## Call a dependency

Inject `IExternalApiCaller` and let it own the `try`/`catch`:

```csharp
public async Task<Result<Order>> Get(string id, CancellationToken cancellationToken)
{
    return await _api.CallAsync(
        token => _payments.GetOrderAsync(id, token),
        cancellationToken: cancellationToken);
}
```

Four failures are converted; everything else propagates untouched, including a bug in your own callback:

| Failure | Result |
|---|---|
| `ApiException` (the dependency answered with an error) | The status code, or the problem body, decides — see below |
| `TimeoutException`, or a cancellation you did not request | `ErrorKind.Timeout` |
| `HttpRequestException` (the dependency was never reached) | `ErrorKind.ServiceUnavailable` |
| A cancellation you *did* request | Rethrown, so `OperationCanceledException` still means what it means |

## The status mapping

The mapping mirrors what the dependency said — the inverse of the kind-to-status mapping Offside applies on the way out.

| Status | Kind | | Status | Kind |
|---|---|---|---|---|
| 400 | `BadRequest` | | 429 | `TooManyRequests` |
| 401 | `Unauthorized` | | 500, other 5xx | `Unexpected` |
| 403 | `Forbidden` | | 502, 503 | `ServiceUnavailable` |
| 404 | `NotFound` | | 504 | `Timeout` |
| 409 | `Conflict` | | other 4xx | `BadRequest` |
| 410 | `Gone` | | | |
| 412 | `PreconditionFailed` | | | |
| 422 | `Unprocessable` | | | |

`OffsideRefit.Kind(statusCode)` exposes it directly.

**A mirrored 404 is not automatically your 404.** The default, `InboundStatusMapping.CollapseClientErrors`, folds every 4xx kind — including a restored Offside problem body — into `ServiceUnavailable` with catalog code `external_api.service_unavailable`. The original kind, code, and errorCode stay in arguments. `Timeout`, `ServiceUnavailable`, and `Unexpected` are left alone.

Two Offside services in the same product, or a BFF that should surface the dependency's status, opt in to the 0.4.0 behaviour:

```csharp
builder.Services.AddOffsideRefit(options =>
    options.InboundStatus = InboundStatusMapping.Mirror);
```

## Catalog codes

Each mapped error gets `external_api.` in front of its catalog code — `external_api.not_found` before collapse, `external_api.service_unavailable` after, `external_api.timeout` for a 504 — so a dependency failure is never confused with your own rule in the message catalog. Add the entries you use:

```json
{
  "external_api.not_found": "The {api} service could not find what we asked for.",
  "external_api.service_unavailable": "The {api} service is unavailable.",
  "external_api.timeout": "The {api} service took too long to answer."
}
```

Available tokens: `{api}`, `{status}`, `{requestUri}`, `{reason}`. Set `CodePrefix` to an empty string to fall back to the core codes (`not_found`, `timeout`, …), which already ship in the default catalog.

## Reading the dependency's problem body

With `ReadProblemDetails` on — the default — an `application/problem+json` body is read before the status is considered:

- **The dependency is an Offside service.** Its `errors` array is restored error for error, keeping each `code`, `errorCode`, `kind`, and `field`. `InboundStatus` then runs: with the default, those 4xx kinds still become `ServiceUnavailable`. With `Mirror`, two services speaking Offside lose nothing across the wire.
- **An ASP.NET validation body** (`"errors": { "email": ["…"] }`) becomes one `ErrorKind.Validation` error per field.
- **A plain problem document** contributes its `detail` and `errorCode`.

Parsing never throws. A malformed, truncated, or unexpected body degrades to the status mapping, so a misbehaving dependency cannot break your error path.

## Mapping without the caller

The extensions the caller uses are public, for code that already has the exception in hand:

```csharp
catch (ApiException exception)
{
    return exception.ToResult<Order>();   // also ToError(), ToOffsideErrors(), ToResult()
}
```

## Observing failures on the wire

`OffsideRefitDiagnosticsHandler` reports every failed response and transport failure to an `IExternalApiErrorObserver`, then lets the outcome continue unchanged. It observes; it never converts a response into a `Result`.

```csharp
builder.Services.AddOffsideRefitDiagnostics();
builder.Services.AddRefitClient<IPaymentsApi>()
    .AddHttpMessageHandler<OffsideRefitDiagnosticsHandler>();
```

Without your own registration the observer is a no-op. This is the seam to telemetry: with [`Offside.ApplicationInsights`](application-insights.md), a five-line adapter connects the two, and neither package depends on the other.

```csharp
internal sealed class TelemetryObserver(IDomainErrorRecorder recorder) : IExternalApiErrorObserver
{
    public void Observe(Error error) => recorder.Record(error);
}

builder.Services.AddSingleton<IExternalApiErrorObserver, TelemetryObserver>();
```

The handler reads the status code only — it leaves the response body untouched, so what it reports has no problem details in it. The full mapping happens in `IExternalApiCaller`.

## Options

| Option | Default | What it does |
|---|---|---|
| `ApiName` | `external api` | Exposed to templates as `{api}` |
| `CodePrefix` | `external_api` | Prefix for catalog codes; empty falls back to the core codes |
| `ReadProblemDetails` | `true` | Reads an `application/problem+json` body before falling back to the status |
| `InboundStatus` | `CollapseClientErrors` | Folds 4xx kinds into `ServiceUnavailable` after mapping; `Mirror` keeps the dependency's kind |

Options passed to a single `CallAsync` win over the registered defaults.

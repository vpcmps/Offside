# Querying domain errors

*[Português](pt-BR/queries.md) · [Back to docs](README.md)*

Domain failures recorded through Offside never become exceptions, so they never produce a message template you can filter on. There is no `{OriginalFormat}`. Filter on dimensions.

The same keys land on Application Insights traces (classic SDK) and on OpenTelemetry logs exported to Azure Monitor. Both write them under `customDimensions`.

| Dimension | What it is | Example |
|---|---|---|
| `offside.code` | Message-catalog key | `order.already_shipped` |
| `offside.errorCode` | Screen identifier | `ORDER_ALREADY_SHIPPED` |
| `offside.kind` | Failure species | `Conflict` |
| `offside.field` | Offending field, when present | `email` |

HTTP hosts also receive `HttpStatus` from the problem pipeline. Caller-supplied keys such as `Operation` come from `TelemetryProperties` or from `RecordTo`.

## Find a code

```kusto
traces
| where customDimensions["offside.code"] == "order.already_shipped"
| project timestamp, severityLevel, message, operation_Id, customDimensions
```

## Group by kind

```kusto
traces
| where isnotempty(customDimensions["offside.kind"])
| summarize count() by tostring(customDimensions["offside.kind"])
```

With the HTTP pipeline default `RecordMode.PerError`, `count()` is errors, not requests. A validation failure on five fields increments five times. `PrimaryErrorOnly` aligns the count with one HTTP failure.

## Alert on unexpected failures

Page on `Unexpected`, not on every recorded error. A correctly answered 404 is Information under the library map.

```kusto
traces
| where customDimensions["offside.kind"] == "Unexpected"
| summarize Failures=count() by bin(timestamp, 5m)
| where Failures > 0
```

An operations team that pages on business refusals as well should set `SeverityFor = DomainErrorSeverityMap.Operations` so 404/400 become Warning, then alert on `severityLevel >= 2`. Do not rebuild that split by parsing `message`.

## Arguments

Arguments are off by default. An allowlist writes only named keys as `offside.arg.{name}`:

```kusto
traces
| where customDimensions["offside.arg.rejectionReason"] == "missing-header"
```

They never appear on the `offside.errors` counter.

## The counter (OpenTelemetry)

```kusto
customMetrics
| where name == "offside.errors"
| summarize sum(value) by tostring(customDimensions["offside.kind"]), tostring(customDimensions["offside.code"])
```

If this query is empty while traces are present, the meter is not in the pipeline — call `AddMeter(OffsideTelemetry.MeterName)`. Offside logs that once on the first emission.

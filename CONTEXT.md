# Glossary

- **Error** — A domain failure: stable `Code` (catalog key), `ErrorCode` (screen identifier), `Kind`, interpolation `Arguments`, optional `Field`. Not an exception.
- **ErrorCode** — Short stable identifier clients use to choose a screen (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`). Several `Code` values may share one `ErrorCode`. Defaults from `Error.DefaultErrorCode(Kind)`.
- **ErrorKind** — Closed set of failure species. Selects HTTP status and severity rank. Business rules reuse a kind via `Custom`; they do not invent kinds.
- **Result / Result&lt;T&gt;** — Success with a value (or unit) or failure with one or more Errors. The normal way domain and application code report failure.
- **Primary error** — The first Error of the most severe Kind in a Result. Drives Problem Details `detail` and the HTTP status.
- **Message catalog** — JSON per culture mapping `Code` → template. Metadata stays in C#; only text is translated.
- **Escape hatch** — `ToException()` / unhandled exceptions. Not used for ordinary business rules.
- **Domain notification** — A MediatR `INotification` that carries exactly one complete `Error`. Published explicitly from a failed Result at the application boundary.
- **Domain notification collector** — A thread-safe scoped accumulator of notification errors. Reads return snapshots and never clear state; one dependency-injection scope should represent one logical operation.
- **External API failure** — A failure returned by a dependency the service calls. Mapped from the dependency's HTTP status onto an `ErrorKind` that mirrors it; whether it should surface unchanged to your own caller is the calling code's decision.
- **Domain error recorder** — A sink that writes an `Error` to telemetry. Severity comes from the `ErrorKind`; arguments are withheld by default, since telemetry outlives the request.

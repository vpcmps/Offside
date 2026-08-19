# Glossary

- **Error** — A domain failure: stable `Code`, `Kind`, interpolation `Arguments`, optional `Field`. Not an exception.
- **ErrorKind** — Closed set of failure species. Selects HTTP status and severity rank. Business rules reuse a kind via `Custom`; they do not invent kinds.
- **Result / Result&lt;T&gt;** — Success with a value (or unit) or failure with one or more Errors. The normal way domain and application code report failure.
- **Primary error** — The first Error of the most severe Kind in a Result. Drives Problem Details `detail` and the HTTP status.
- **Message catalog** — JSON per culture mapping `Code` → template. Metadata stays in C#; only text is translated.
- **Escape hatch** — `ToException()` / unhandled exceptions. Not used for ordinary business rules.
- **Domain notification** — A MediatR `INotification` that carries exactly one complete `Error`. Published explicitly from a failed Result at the application boundary.
- **Domain notification collector** — A thread-safe scoped accumulator of notification errors. Reads return snapshots and never clear state; one dependency-injection scope should represent one logical operation.

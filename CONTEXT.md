# Glossary

- **Error** — A domain failure: stable `Code` (catalog key), `ErrorCode` (screen identifier), `Kind`, interpolation `Arguments`, optional `Field`. Not an exception.
- **ErrorCode** — Short stable identifier clients use to choose a screen (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`). Several `Code` values may share one `ErrorCode`. Defaults from `Error.DefaultErrorCode(Kind)`.
- **ErrorKind** — Closed set of failure species. Selects HTTP status and severity rank. Business rules reuse a kind via `Custom`; they do not invent kinds.
- **Result / Result&lt;T&gt;** — Success with a value (or unit) or failure with one or more Errors. The normal way domain and application code report failure.
- **Primary error** — The first Error of the most severe Kind in a Result. Drives Problem Details `detail` and the HTTP status.
- **Message catalog** — JSON per culture mapping `Code` → template. Metadata stays in C#; only text is translated.
- **Escape hatch** — `ToException()` / unhandled exceptions. Not used for ordinary business rules.

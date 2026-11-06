# Migrating from domain exceptions

1. Register Offside and its ASP.NET Core options, then add `services.AddOffsideExceptionHandler()` and call `app.UseOffsideExceptionHandler()` before your endpoints.
2. Replace a custom not-found exception with `throw Error.NotFound("order", id).ToException()`. If your old handler still owns ordinary exceptions, configure `HandleUnexpectedExceptions = false`.
3. Convert the busiest use cases to return a failed `Result` and render it with `ToHttpResult(http)`. The error response has the same shape.
4. Remove the old exception types and handler once no remaining callers depend on them.

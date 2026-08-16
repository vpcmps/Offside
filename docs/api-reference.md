# API reference

*[Português](pt-BR/api-reference.md) · [Back to docs](README.md)*

Every public type, in one page. The XML documentation shipped with the packages is the authoritative version — this page is for scanning.

## Offside

Namespace `Offside`.

### ErrorKind

```csharp
public enum ErrorKind
{
    Unexpected, Unauthorized, Forbidden, TooManyRequests, Conflict,
    PreconditionFailed, Gone, Unprocessable, NotFound, Validation, BadRequest
}
```

The closed set of failure species. Selects the HTTP status and the severity rank. Declaration order is not severity order — see the [status and severity tables](aspnet-guide.md#status-codes).

### Error

```csharp
public sealed class Error : IEquatable<Error>
```

| Member | Description |
|---|---|
| `string Code { get; }` | Stable identifier and message-catalog key |
| `ErrorKind Kind { get; }` | Failure species |
| `IReadOnlyDictionary<string, object?> Arguments { get; }` | Read-only snapshot of the template values |
| `string? Field { get; }` | Offending field, when attributable |
| `static Error NotFound(string resource, object? id = null)` | Code `not_found` |
| `static Error Gone(string resource, object? id = null)` | Code `gone` |
| `static Error Conflict(string resource, string? reason = null)` | Code `conflict` |
| `static Error Validation(string field, string? code = null, object? attemptedValue = null)` | Code `validation` or `code`; sets `Field` |
| `static Error BadRequest(string? reason = null)` | Code `bad_request` |
| `static Error Unauthorized(string? reason = null)` | Code `unauthorized` |
| `static Error Forbidden(string? reason = null)` | Code `forbidden` |
| `static Error PreconditionFailed(string? reason = null)` | Code `precondition_failed` |
| `static Error Unprocessable(string? reason = null)` | Code `unprocessable` |
| `static Error TooManyRequests(string? reason = null)` | Code `too_many_requests` |
| `static Error Unexpected(string? detail = null)` | Code `unexpected`; `detail` is diagnostic only |
| `static Error Custom(string code, ErrorKind kind, object? arguments = null, string? field = null)` | Business-rule error. Throws `ArgumentException` on a blank code |
| `DomainException ToException()` | Escape hatch |
| `bool Equals(Error?)`, `operator ==`, `operator !=` | Value equality, arguments included |

The constructor is internal; construction goes through the factories.

### Result

```csharp
public readonly struct Result
```

| Member | Description |
|---|---|
| `bool IsSuccess { get; }` / `bool IsFailure { get; }` | Outcome |
| `IReadOnlyList<Error> Errors { get; }` | Errors on failure; empty on success |
| `TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)` | Branch into a value |
| `static Result Success()` | Success |
| `static Result Failure(params Error[] errors)` | Failure. Throws `ArgumentException` if empty |
| `static Result Failure(IEnumerable<Error> errors)` | Failure from a sequence, copied immediately |
| `static Result Combine(params Result[] results)` | Merge, concatenating errors in argument order |
| `static Result Combine<T>(params Result<T>[] results)` | Merge value results, discarding the values |

`default(Result)` is a success.

### Result&lt;T&gt;

```csharp
public readonly struct Result<T>
```

| Member | Description |
|---|---|
| `bool IsSuccess { get; }` / `bool IsFailure { get; }` | Outcome |
| `T Value { get; }` | The value. Throws `InvalidOperationException` on failure |
| `IReadOnlyList<Error> Errors { get; }` | Errors on failure; empty on success |
| `bool TryGetValue(out T value)` | Non-throwing read |
| `TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)` | Branch into a value |
| `Result<TOut> Map<TOut>(Func<T, TOut> map)` | Transform the value; short-circuits on failure |
| `Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)` | Chain a fallible operation; short-circuits on failure |
| `static Result<T> Success(T value)` | Success |
| `static Result<T> Failure(params Error[] errors)` | Failure. Throws `ArgumentException` if empty |
| `static Result<T> Failure(IEnumerable<Error> errors)` | Failure from a sequence, copied immediately |

No implicit conversion from `T`, and no `Apply` — see [deliberate omissions](domain-guide.md#deliberate-omissions).

### DomainException

```csharp
public sealed class DomainException : Exception
{
    public IReadOnlyList<Error> Errors { get; }
    public DomainException(IReadOnlyList<Error> errors);
}
```

`Message` is the first error's `Code`. Produced by `Error.ToException()`.

### IErrorMessageResolver

```csharp
public interface IErrorMessageResolver
{
    string GetMessage(Error error, CultureInfo culture);
}
```

Implement to source messages from somewhere other than JSON. By convention, return `error.Code` when no message is found.

### JsonErrorCatalog

```csharp
public sealed class JsonErrorCatalog
{
    public CultureInfo Culture { get; }
    public Stream Json { get; }
    public JsonErrorCatalog(CultureInfo culture, Stream json);
}
```

Throws `ArgumentNullException` on a null culture or stream.

### JsonErrorMessageResolver

```csharp
public sealed class JsonErrorMessageResolver : IErrorMessageResolver
{
    public JsonErrorMessageResolver(IEnumerable<JsonErrorCatalog> catalogs);
    public string GetMessage(Error error, CultureInfo culture);
}
```

Parses all catalogs in the constructor. Throws `InvalidOperationException` when no invariant catalog is supplied. Lookup order: exact culture → parent → invariant; then the code itself.

### OffsideOptions

```csharp
public sealed class OffsideOptions
{
    public OffsideOptions AddJson(CultureInfo culture, string json);
    public OffsideOptions AddJson(CultureInfo culture, Stream json);
}
```

Both overloads take catalog **content**, not a path. Fluent.

### OffsideServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffside(this IServiceCollection services, Action<OffsideOptions> configure);
```

Builds a `JsonErrorMessageResolver` eagerly and registers it as the singleton `IErrorMessageResolver`.

## Offside.AspNetCore

Namespace `Offside.AspNetCore`.

### OffsideAspNetCoreOptions

```csharp
public sealed class OffsideAspNetCoreOptions
{
    public bool ExposeExceptionDetails { get; set; }
    public static OffsideAspNetCoreOptions FromEnvironment(IHostEnvironment environment);
}
```

`ExposeExceptionDetails` gates the `debug` field only; the client-facing `detail` of a 500 is always the generic message.

### OffsideAspNetCoreServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffsideAspNetCore(this IServiceCollection services);
```

Registers `OffsideAspNetCoreOptions` as a singleton, defaulting `ExposeExceptionDetails` from `IHostEnvironment.IsDevelopment()` when one is present, otherwise `false`.

### OffsideProblem

```csharp
public sealed class OffsideProblem
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public int Status { get; init; }
    public required string Detail { get; init; }
    public required string TraceId { get; init; }
    public string? Debug { get; init; }              // omitted from JSON when null
    public required IReadOnlyList<Item> Errors { get; init; }

    public static OffsideProblem Create(
        IReadOnlyList<Error> errors,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        string traceId,
        bool exposeExceptionDetails = false);

    public sealed class Item
    {
        public required string Code { get; init; }
        public required string Kind { get; init; }
        public required string Detail { get; init; }
        public string? Field { get; init; }
    }
}
```

Serialized as `application/problem+json` with camelCase names. See [the response shape](aspnet-guide.md#failure-mapping).

### ResultHttpExtensions

```csharp
public static class ResultHttpExtensions
```

Minimal APIs — success is `204 No Content` for `Result`, `200 OK` with the value for `Result<T>`:

```csharp
IResult ToHttpResult(this Result result, IErrorMessageResolver resolver, bool exposeExceptionDetails = false);
IResult ToHttpResult(this Result result, IErrorMessageResolver resolver, CultureInfo culture, bool exposeExceptionDetails = false);
IResult ToHttpResult(this Result result, IErrorMessageResolver resolver, CultureInfo? culture, OffsideAspNetCoreOptions options);
IResult ToHttpResult(this Result result, HttpContext httpContext);

IResult ToHttpResult<T>(this Result<T> result, IErrorMessageResolver resolver, bool exposeExceptionDetails = false);
IResult ToHttpResult<T>(this Result<T> result, IErrorMessageResolver resolver, CultureInfo culture, bool exposeExceptionDetails = false);
IResult ToHttpResult<T>(this Result<T> result, IErrorMessageResolver resolver, CultureInfo? culture, OffsideAspNetCoreOptions options);
IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext);
```

MVC controllers — success is `NoContentResult` / `OkObjectResult`:

```csharp
IActionResult ToActionResult(this Result result, IErrorMessageResolver resolver, CultureInfo culture, bool exposeExceptionDetails = false);
IActionResult ToActionResult(this Result result, IErrorMessageResolver resolver, CultureInfo? culture, OffsideAspNetCoreOptions options);

IActionResult ToActionResult<T>(this Result<T> result, IErrorMessageResolver resolver, bool exposeExceptionDetails = false);
IActionResult ToActionResult<T>(this Result<T> result, IErrorMessageResolver resolver, CultureInfo culture, bool exposeExceptionDetails = false);
IActionResult ToActionResult<T>(this Result<T> result, IErrorMessageResolver resolver, CultureInfo? culture, OffsideAspNetCoreOptions options);
```

Note the asymmetry: there is **no** `ToActionResult(this Result, IErrorMessageResolver, bool)` for the unit `Result`. Pass a culture, or pass `null` through the options overload.

A `null` culture means "derive it from `Accept-Language`". All overloads throw `ArgumentNullException` on a null resolver, options, or `HttpContext`.

## Offside.Tool

Namespace `Offside.Tool`.

### SkillInstaller

```csharp
public sealed class SkillInstaller
{
    public const string CursorSkills = ".cursor/skills";
    public const string AgentsSkills = ".agents/skills";
    public const string ClaudeSkills = ".claude/skills";

    public SkillInstaller(string skillsSource);
    public static SkillInstaller FromToolLocation();
    public IReadOnlyList<string> Install(string projectRoot, bool force);
}
```

`Install` returns every path written, in write order. Throws `DirectoryNotFoundException` when the skills source or an expected skill folder is missing. See the [CLI page](cli.md).

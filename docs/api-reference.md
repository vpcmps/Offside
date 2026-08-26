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
    PreconditionFailed, Gone, Unprocessable, NotFound, Validation, BadRequest,
    ServiceUnavailable, Timeout
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
| `string ErrorCode { get; }` | Screen identifier (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`) |
| `ErrorKind Kind { get; }` | Failure species |
| `IReadOnlyDictionary<string, object?> Arguments { get; }` | Read-only snapshot of the template values |
| `string? Field { get; }` | Offending field, when attributable |
| `static string DefaultErrorCode(ErrorKind kind)` | Kind default, e.g. `TOO_MANY_REQUESTS` |
| `static Error NotFound(string resource, object? id = null, string? errorCode = null)` | Code `not_found` |
| `static Error Gone(string resource, object? id = null, string? errorCode = null)` | Code `gone` |
| `static Error Conflict(string resource, string? reason = null, string? errorCode = null)` | Code `conflict` |
| `static Error Validation(string field, string? code = null, object? attemptedValue = null, string? errorCode = null)` | Code `validation` or `code`; sets `Field` |
| `static Error BadRequest(string? reason = null, string? errorCode = null)` | Code `bad_request` |
| `static Error Unauthorized(string? reason = null, string? errorCode = null)` | Code `unauthorized` |
| `static Error Forbidden(string? reason = null, string? errorCode = null)` | Code `forbidden` |
| `static Error PreconditionFailed(string? reason = null, string? errorCode = null)` | Code `precondition_failed` |
| `static Error Unprocessable(string? reason = null, string? errorCode = null)` | Code `unprocessable` |
| `static Error TooManyRequests(string? reason = null, string? errorCode = null)` | Code `too_many_requests` |
| `static Error ServiceUnavailable(string? reason = null, string? errorCode = null)` | Code `service_unavailable`. Default catalog does not interpolate `{reason}` |
| `static Error Timeout(string? reason = null, string? errorCode = null)` | Code `timeout`. Default catalog does not interpolate `{reason}` |
| `static Error Unexpected(string? detail = null, string? errorCode = null)` | Code `unexpected`; `detail` is diagnostic only |
| `static Error Custom(string code, ErrorKind kind, object? arguments = null, string? field = null, string? errorCode = null)` | Business-rule error. Throws `ArgumentException` on a blank code |
| `DomainException ToException()` | Escape hatch |
| `bool Equals(Error?)`, `operator ==`, `operator !=` | Value equality, including `ErrorCode` and arguments |

Blank or whitespace `errorCode` uses `DefaultErrorCode(Kind)`; otherwise it is trimmed. The constructor is internal; construction goes through the factories.

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
| `Result RecordTo(IDomainErrorRecorder recorder, IReadOnlyDictionary<string, string>? properties = null)` | Records each error; success is a no-op. HTTP hosts that call `ToHttpResult` do not need this |

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
| `Result<T> RecordTo(IDomainErrorRecorder recorder, IReadOnlyDictionary<string, string>? properties = null)` | Same as `Result.RecordTo` |

No implicit conversion from `T`, and no `Apply` — see [deliberate omissions](domain-guide.md#deliberate-omissions).

### IDomainErrorRecorder

```csharp
public interface IDomainErrorRecorder
{
    void Record(Error error, IReadOnlyDictionary<string, string>? properties = null);
}
```

Implemented by `AddOffsideOpenTelemetry` and `AddOffsideApplicationInsights`. HTTP hosts do not call `RecordTo` — the problem pipeline records when this is registered. Implementations must not throw.

### DomainErrorSeverity

```csharp
public enum DomainErrorSeverity { Verbose, Information, Warning, Error, Critical }
```

Mirrors classic Application Insights severity names.

### DomainErrorSeverityMap

```csharp
public static class DomainErrorSeverityMap
{
    public static DomainErrorSeverity Library(ErrorKind kind);
    public static DomainErrorSeverity Operations(ErrorKind kind);
}
```

`Library` is the default on both telemetry packages (404/400 = Information, Unexpected = Critical). `Operations` raises refusals including NotFound/Validation/BadRequest to Warning and drops Unexpected to Error.

### DomainErrorMessageFormat

```csharp
public static class DomainErrorMessageFormat
{
    public static readonly Func<Error, string, string> MessageOnly;
    public static readonly Func<Error, string, string> CodePrefixed;
    public static readonly Func<Error, string, string> ErrorCodePrefixed;
}
```

Shapes the log or trace line only. Dimensions are unaffected.

### ErrorArgumentFilter

```csharp
public static class ErrorArgumentFilter
{
    public static IEnumerable<KeyValuePair<string, object?>> Select(
        Error error, bool includeAll, IReadOnlyCollection<string>? keys);
}
```

Used by both recorders. `includeAll: true` ignores `keys`. Null argument values are skipped.

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

### ErrorMessageTemplate

```csharp
public static class ErrorMessageTemplate
{
    public static string Interpolate(string template, IReadOnlyDictionary<string, object?> arguments);
}
```

Shared interpolation used by built-in resolvers. Null argument values and unmatched tokens remain literal.

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
    public OffsideOptions AddJsonFile(CultureInfo culture, string path);
    public OffsideOptions AddJsonFromAssembly(CultureInfo culture, Assembly assembly, string resourceName);
}
```

`AddJson` takes catalog **content**, not a path. `AddJsonFile` reads the file (relative paths resolve against `AppContext.BaseDirectory`) and throws `FileNotFoundException` naming the resolved path. `AddJsonFromAssembly` copies an embedded resource and throws `InvalidOperationException` naming a missing resource. Fluent.

### OffsideServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffside(this IServiceCollection services, Action<OffsideOptions> configure);
```

Builds a `JsonErrorMessageResolver` eagerly and registers it as the singleton `IErrorMessageResolver`.

## Offside.MediatR

Namespace `Offside.MediatR`. The package depends on MediatR in the range `[12.0.1,15.0.0)`; the Offside core package remains independent.

### DomainNotification

```csharp
public sealed class DomainNotification : INotification
{
    public DomainNotification(Error error);
    public Error Error { get; }
}
```

Carries exactly one non-null error. It is an error notification, not a domain event describing a state change.

### IDomainNotificationCollector

```csharp
public interface IDomainNotificationCollector
{
    bool HasNotifications { get; }
    IReadOnlyList<Error> Errors { get; }
    Result ToResult();
    Result<T> ToResult<T>(T value);
}
```

The collector is scoped and thread-safe. `Errors` is an independent snapshot; reads never clear state. Both result methods return success when empty and a failure containing every collected error otherwise.

### ResultMediatRExtensions

```csharp
public static Task<Result> PublishDomainNotificationsAsync(
    this Result result,
    IPublisher publisher,
    CancellationToken cancellationToken = default);

public static Task<Result<T>> PublishDomainNotificationsAsync<T>(
    this Result<T> result,
    IPublisher publisher,
    CancellationToken cancellationToken = default);
```

Success publishes nothing. Failure publishes one notification per error, sequentially and in result order, then returns the original result. Cancellation and handler exceptions stop remaining publications and propagate immediately.

### OffsideMediatRServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffsideMediatR(this IServiceCollection services);
```

Idempotently registers the scoped collector and its handler. It does not call `AddMediatR`, register `IPublisher`, or configure licensing. See the [MediatR guide](mediatr-guide.md).

## Offside.AzureAppConfiguration

Namespace `Offside.AzureAppConfiguration`.

```csharp
public sealed class AzureAppConfigurationOptions
{
    public string SectionName { get; set; } = "Errors";
}

public sealed class ConfigurationErrorMessageResolver : IErrorMessageResolver
{
    public ConfigurationErrorMessageResolver(IConfiguration configuration);
    public ConfigurationErrorMessageResolver(IConfiguration configuration, string sectionName);
}

public static IServiceCollection AddOffsideAzureAppConfiguration(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<AzureAppConfigurationOptions>? configure = null);
```

Reads `Errors:<culture>:<code>` dynamically, with exact-culture → parent → `default` fallback. The default catalog must exist. Azure connection, labels, and refresh are configured by the host; do not also call `AddOffside`.

## Offside.AspNetCore

Namespace `Offside.AspNetCore`.

### OffsideAspNetCoreOptions

```csharp
public sealed class OffsideAspNetCoreOptions
{
    public bool ExposeExceptionDetails { get; set; }
    public bool LogUnexpected { get; set; }            // true when no recorder; false when one is registered, unless set explicitly
    public Action<OffsideProblem, IReadOnlyList<Error>>? CustomizeProblem { get; set; }
    public Action<OffsideProblem, IReadOnlyList<Error>, HttpContext>? OnProblem { get; set; }
    public Func<OffsideProblem, IReadOnlyList<Error>, HttpContext, IReadOnlyDictionary<string, string>>? TelemetryProperties { get; set; }
    public LegacyProblemAliases LegacyAliases { get; set; }  // None
    public Func<HttpContext, string>? ResolveTraceId { get; set; }
    public static OffsideAspNetCoreOptions FromEnvironment(IHostEnvironment environment);
}

public enum LegacyProblemAliases
{
    None = 0,
    MessageReasonAndTechnicalDetail = 1
}
```

`ExposeExceptionDetails` gates the `debug` field only; the client-facing `detail` of a 500 is always the generic message. `LogUnexpected` controls the built-in `ILogger` line for `Unexpected` failures and defaults off when an `IDomainErrorRecorder` is registered. `TelemetryProperties` is merged into every pipeline recording (`HttpStatus` is always written). `LegacyAliases.MessageReasonAndTechnicalDetail` adds `message`, `errors[].name`, `errors[].reason`, and `technicalDetail`. `CustomizeProblem` may add flattened JSON members via `Extensions`; reserved keys are stripped. `OnProblem` is a host hook — it does not emit telemetry, and must not write the response body. `ResolveTraceId` replaces the default 32-hex `Activity.TraceId`. The `bool exposeExceptionDetails` overloads are obsolete and construct options without these callbacks; hooks require the `HttpContext` / DI path or an explicit options object. `ToHttpResult(HttpContext)` throws `InvalidOperationException` naming `AddOffsideAspNetCore` when the singleton is missing.

### OffsideAspNetCoreServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffsideAspNetCore(
    this IServiceCollection services,
    Action<OffsideAspNetCoreOptions>? configure = null);
```

Registers `OffsideAspNetCoreOptions` as a singleton, defaulting `ExposeExceptionDetails` from `IHostEnvironment.IsDevelopment()` when one is present, otherwise `false`. `configure` runs afterwards and wins.

### OffsideProblem

```csharp
public sealed class OffsideProblem
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public int Status { get; init; }
    public required string Detail { get; init; }
    public required string TraceId { get; init; }
    public required string ErrorCode { get; init; }  // primary screen identifier
    public string? Debug { get; init; }              // omitted from JSON when null
    public required IReadOnlyList<Item> Errors { get; init; }
    public IDictionary<string, object?> Extensions { get; init; }  // flattened via [JsonExtensionData]

    public static OffsideProblem Create(
        IReadOnlyList<Error> errors,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        string traceId,
        bool exposeExceptionDetails = false);

    public sealed class Item
    {
        public required string Code { get; init; }
        public required string ErrorCode { get; init; }
        public required string Kind { get; init; }
        public required string Detail { get; init; }
        public string? Field { get; init; }
        public IDictionary<string, object?> Extensions { get; init; }
    }
}
```

Serialized as `application/problem+json` with camelCase names. Extra fields on `Extensions` are flattened into the JSON. A sanitized 500 forces `errorCode` to `UNEXPECTED`. See [the response shape](aspnet-guide.md#failure-mapping).

### OffsideHttp

```csharp
public static class OffsideHttp
{
    public static IReadOnlyList<int> StatusCodes { get; }  // 400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504
    public static int StatusCode(ErrorKind kind);
    public static Error SelectPrimary(IReadOnlyList<Error> errors);
}
```

The kind → HTTP mapping used by Problem Details and by `Offside.FastEndpoint`. `SelectPrimary` returns the error of the most severe kind; empty lists throw `ArgumentException`.

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

The `bool exposeExceptionDetails` overloads are obsolete. A `null` culture means "derive it from `Accept-Language`". All overloads throw `ArgumentNullException` on a null resolver, options, or `HttpContext`. The `HttpContext` overloads also throw `InvalidOperationException` when `OffsideAspNetCoreOptions` is not registered.

## Offside.FluentValidation

Namespace `Offside.FluentValidation`. Targets `netstandard2.0`, `net8.0`, `net10.0`.

```csharp
public static class FluentValidationOffsideExtensions
{
    public static IReadOnlyList<Error> ToOffsideErrors(this IEnumerable<ValidationFailure> failures);
    public static IReadOnlyList<Error> ToOffsideErrors(this ValidationResult result);
    public static IReadOnlyList<Error> ToOffsideErrors(this ValidationException exception);
    public static Result ToResult(this ValidationResult result);
}
```

`.WithErrorCode("email.taken")` becomes `Error.Code`. FluentValidation's default `*Validator` names (and blank codes) become `validation`. `Error.ErrorCode` is `VALIDATION`. Empty `PropertyName` sets `Field` to null. See [FluentValidation](fluentvalidation.md).

## Offside.FastEndpoint

Namespace `Offside.FastEndpoint`. Targets `net8.0`, `net10.0`.

```csharp
public static class OffsideFastEndpointExtensions
{
    public static Config UseOffside(this Config config, Action<EndpointDefinition>? configure = null);
    public static void DontProduceOffside(this EndpointDefinition definition);
}

public static class OffsideResultSendExtensions
{
    public static Task SendOffsideAsync(this Result result, HttpContext httpContext, CancellationToken cancellationToken = default);
    public static Task SendOffsideAsync<T>(this Result<T> result, HttpContext httpContext, CancellationToken cancellationToken = default);
}
```

`UseOffside` sets the validation `ResponseBuilder` to `OffsideProblem`, `ProducesMetadataType` to `typeof(OffsideProblem)`, content type `application/problem+json`, and registers `Produces<OffsideProblem>` for every `OffsideHttp.StatusCodes` value. `SendOffsideAsync` reuses `ToHttpResult`. See [FastEndpoints](fastendpoints.md).

## Offside.Testing

Namespace `Offside.Testing`. Targets `netstandard2.0`, `net8.0`, `net10.0`. No test-framework dependency.

```csharp
public sealed class OffsideAssertionException : Exception;

public static class ResultAssertions
{
    public static Result ShouldBeSuccess(this Result result);
    public static Result ShouldBeFailure(this Result result);
    public static ErrorAssertion<Result> ShouldHaveError(this Result result, string code);
    public static ErrorAssertion<Result> ShouldHaveOnlyError(this Result result, string code);
    public static Result ShouldHaveErrorsInOrder(this Result result, params string[] codes);
    public static Result ShouldHaveErrorCount(this Result result, int count);
}

public static class ResultOfTAssertions
{
    public static SuccessAssertion<T> ShouldBeSuccess<T>(this Result<T> result);
    public static Result<T> ShouldBeFailure<T>(this Result<T> result);
    public static ErrorAssertion<Result<T>> ShouldHaveError<T>(this Result<T> result, string code);
    public static ErrorAssertion<Result<T>> ShouldHaveOnlyError<T>(this Result<T> result, string code);
    public static Result<T> ShouldHaveErrorsInOrder<T>(this Result<T> result, params string[] codes);
    public static Result<T> ShouldHaveErrorCount<T>(this Result<T> result, int count);
}

public sealed class ErrorAssertion<TResult>
{
    public Error Subject { get; }
    public TResult And { get; }

    public ErrorAssertion<TResult> WithKind(ErrorKind kind);
    public ErrorAssertion<TResult> WithErrorCode(string errorCode);
    public ErrorAssertion<TResult> ForField(string? field);
    public ErrorAssertion<TResult> WithArgument(string name, object? value);
    public ErrorAssertion<TResult> WithMessage(IErrorMessageResolver resolver, string message);
    public ErrorAssertion<TResult> WithMessage(IErrorMessageResolver resolver, CultureInfo culture, string message);
}

public sealed class SuccessAssertion<T>
{
    public T Subject { get; }
    public Result<T> And { get; }

    public SuccessAssertion<T> WithValue(T value);
    public SuccessAssertion<T> WithValue(Func<T, bool> predicate, string? description = null);
}

public sealed class OffsideCatalog
{
    public string Source { get; }
    public IReadOnlyCollection<string> Codes { get; }

    public static OffsideCatalog FromFile(string path);
    public static OffsideCatalog FromJson(string json, string? source = null);
    public static OffsideCatalog FromStream(Stream json, string? source = null);
    public static OffsideCatalog FromAssembly(Assembly assembly, string resourceName);

    public OffsideCatalog ShouldDefine(string code);
    public OffsideCatalog ShouldDefineAll(params string[] codes);
    public OffsideCatalog ShouldResolve(Error error);
    public OffsideCatalog ShouldResolveAll(params Error[] errors);
    public OffsideCatalog ShouldDefineSameCodesAs(OffsideCatalog other);
}
```

Every assertion throws `OffsideAssertionException` carrying the actual contents of the subject. `ShouldHaveError` is the default choice; `ShouldHaveOnlyError` also fails on an extra error, and `ShouldHaveErrorsInOrder` pins ordering that comes from `Result.Combine` argument order or FluentValidation rule declaration order. `OffsideCatalog` reads the JSON directly, so a missing code is distinguishable from a template equal to the code. See the [Testing guide](testing.md).
## Offside.Refit

Namespace `Offside.Refit`. Targets `netstandard2.0`, `net8.0`, `net10.0`.

```csharp
public static class OffsideRefit
{
    public static ErrorKind Kind(HttpStatusCode statusCode);
    public static string CodeSuffix(ErrorKind kind);
}

public sealed class OffsideRefitOptions
{
    public string ApiName { get; set; }              // "external api"
    public string CodePrefix { get; set; }           // "external_api"
    public bool ReadProblemDetails { get; set; }     // true
    public InboundStatusMapping InboundStatus { get; set; }  // CollapseClientErrors
}

public enum InboundStatusMapping { CollapseClientErrors, Mirror }

public static class RefitOffsideExtensions
{
    public static IReadOnlyList<Error> ToOffsideErrors(this ApiException exception, OffsideRefitOptions? options = null);
    public static Error ToError(this ApiException exception, OffsideRefitOptions? options = null);
    public static Result ToResult(this ApiException exception, OffsideRefitOptions? options = null);
    public static Result<T> ToResult<T>(this ApiException exception, OffsideRefitOptions? options = null);
    public static Error ToOffsideError(this HttpRequestException exception, OffsideRefitOptions? options = null);
}

public interface IExternalApiCaller
{
    Task<Result<T>> CallAsync<T>(Func<CancellationToken, Task<T>> call, OffsideRefitOptions? options = null, CancellationToken cancellationToken = default);
    Task<Result> CallAsync(Func<CancellationToken, Task> call, OffsideRefitOptions? options = null, CancellationToken cancellationToken = default);
}

public interface IExternalApiErrorObserver
{
    void Observe(Error error);
}

public sealed class OffsideRefitDiagnosticsHandler : DelegatingHandler
{
    public OffsideRefitDiagnosticsHandler(IExternalApiErrorObserver observer, OffsideRefitOptions options);
}

public static class OffsideRefitServiceCollectionExtensions
{
    public static IServiceCollection AddOffsideRefit(this IServiceCollection services, Action<OffsideRefitOptions>? configure = null);
    public static IServiceCollection AddOffsideRefitDiagnostics(this IServiceCollection services);
}
```

The status mapping is 404 → `NotFound`, 502/503 → `ServiceUnavailable`, 504 → `Timeout`, other 5xx → `Unexpected`, other 4xx → `BadRequest`. After that mapping (or after restoring a problem body), `InboundStatus` defaults to folding every 4xx kind into `ServiceUnavailable`. `Mirror` keeps the dependency's kind. `CallAsync` converts `ApiException`, timeouts, and transport failures only; a cancellation the caller requested is rethrown. Problem-body parsing never throws. See [Refit integration](refit.md).

## Offside.ApplicationInsights

Namespace `Offside.ApplicationInsights`. Targets `netstandard2.0`, `net8.0`, `net10.0`. `IDomainErrorRecorder`, `DomainErrorSeverity`, and `DomainErrorMessageFormat` live in `Offside`.

```csharp
public sealed class OffsideApplicationInsightsOptions
{
    public string PropertyPrefix { get; set; }              // "offside."
    public bool IncludeArguments { get; set; }              // false
    public IReadOnlyCollection<string> IncludeArgumentKeys { get; set; }  // empty
    public CultureInfo Culture { get; set; }                // InvariantCulture
    public Func<ErrorKind, DomainErrorSeverity> SeverityFor { get; set; }  // Library
    public Func<Error, string, string> FormatMessage { get; set; }  // MessageOnly
}

public static class OffsideApplicationInsightsServiceCollectionExtensions
{
    public static IServiceCollection AddOffsideApplicationInsights(this IServiceCollection services, Action<OffsideApplicationInsightsOptions>? configure = null);
}
```

Each error becomes one `TraceTelemetry` carrying `offside.code`, `offside.errorCode`, `offside.kind`, and `offside.field`. Offside dimensions win over supplied properties. `Error.Arguments` are written as `offside.arg.{name}` for keys in `IncludeArgumentKeys`, or for every key when `IncludeArguments` is on. `FormatMessage` shapes the trace text only. See [Application Insights](application-insights.md).

## Offside.ApplicationInsights.MediatR

Namespace `Offside.ApplicationInsights.MediatR`. Targets `netstandard2.0`, `net8.0`, `net10.0`.

```csharp
public sealed class DomainNotificationTelemetryHandler : INotificationHandler<DomainNotification>
{
    public DomainNotificationTelemetryHandler(IDomainErrorRecorder recorder);
    public Task Handle(DomainNotification notification, CancellationToken cancellationToken);
}

public static class OffsideApplicationInsightsMediatRServiceCollectionExtensions
{
    public static IServiceCollection AddOffsideApplicationInsightsMediatR(this IServiceCollection services);
}
```

Idempotent, and independent of the scoped collector registered by `AddOffsideMediatR`.

## Offside.OpenTelemetry

Namespace `Offside.OpenTelemetry`. Targets `netstandard2.0`, `net8.0`, `net10.0`. `IDomainErrorRecorder`, `DomainErrorSeverity`, and `DomainErrorMessageFormat` live in `Offside`.

```csharp
public static class OffsideTelemetry
{
    public const string MeterName;         // "Offside"
    public const string LoggerCategory;    // "Offside"
    public const string ErrorCounterName;  // "offside.errors"
    public const string ErrorEventName;    // "offside.error"
}

public enum ActivityFailurePolicy { None, ServerErrors, FromSeverity }

public sealed class OffsideOpenTelemetryOptions
{
    public string PropertyPrefix { get; set; }                                  // "offside."
    public bool IncludeArguments { get; set; }                                   // false
    public IReadOnlyCollection<string> IncludeArgumentKeys { get; set; }         // empty
    public CultureInfo Culture { get; set; }                                     // InvariantCulture
    public Func<ErrorKind, DomainErrorSeverity> SeverityFor { get; set; }        // Library
    public Func<Error, string, string> FormatMessage { get; set; }             // MessageOnly
    public bool EmitLog { get; set; }                                            // true
    public bool EmitActivityEvent { get; set; }                                  // true
    public bool EmitMetric { get; set; }                                         // true
    public ActivityFailurePolicy ActivityFailure { get; set; }                   // None
    public bool SetActivityStatusOnError { get; set; }                           // false
    public DomainErrorSeverity MinimumSeverityForActivityFailure { get; set; }   // Error
}

public static class OffsideOpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddOffsideOpenTelemetry(this IServiceCollection services, Action<OffsideOpenTelemetryOptions>? configure = null);
}
```

Each error becomes up to three signals: an `ILogger` entry under category `Offside` whose state is a list of key/value pairs, an `offside.error` event on `Activity.Current`, and an increment of the `offside.errors` counter. The first two carry `offside.code`, `offside.errorCode`, `offside.kind`, and `offside.field`; the counter carries only `offside.kind` and `offside.code`. `IncludeArgumentKeys` / `IncludeArguments` apply to the log and the span event, never the counter. `ActivityFailure.ServerErrors` marks the span for Unexpected / ServiceUnavailable / Timeout. The first emission with `EmitMetric` and no meter listener logs a warning asking for `AddMeter(OffsideTelemetry.MeterName)`.

The package references no OpenTelemetry or Azure assembly. Severity defaults match `Offside.ApplicationInsights`. See [OpenTelemetry](open-telemetry.md).

## Offside.OpenTelemetry.MediatR

Namespace `Offside.OpenTelemetry.MediatR`. Targets `netstandard2.0`, `net8.0`, `net10.0`.

```csharp
public sealed class DomainNotificationTelemetryHandler : INotificationHandler<DomainNotification>
{
    public DomainNotificationTelemetryHandler(IDomainErrorRecorder recorder);
    public Task Handle(DomainNotification notification, CancellationToken cancellationToken);
}

public static class OffsideOpenTelemetryMediatRServiceCollectionExtensions
{
    public static IServiceCollection AddOffsideOpenTelemetryMediatR(this IServiceCollection services);
}
```

Idempotent, and independent of the scoped collector registered by `AddOffsideMediatR`.

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

# Referência de API

*[English](../api-reference.md) · [Voltar às docs](README.md)*

Todos os tipos públicos, em uma página. A documentação XML enviada com os pacotes é a versão autoritativa — esta página serve para consulta rápida.

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

O conjunto fechado de espécies de falha. Seleciona o status HTTP e o rank de severidade. A ordem de declaração não é a ordem de severidade — veja as [tabelas de status e severidade](aspnet-guide.md#status-codes).

### Error

```csharp
public sealed class Error : IEquatable<Error>
```

| Membro | Descrição |
|---|---|
| `string Code { get; }` | Identificador estável e chave no catálogo de mensagens |
| `string ErrorCode { get; }` | Identificador de tela (`NOT_FOUND`, `ORDER_ALREADY_SHIPPED`) |
| `ErrorKind Kind { get; }` | Espécie da falha |
| `IReadOnlyDictionary<string, object?> Arguments { get; }` | Snapshot somente leitura dos valores do template |
| `string? Field { get; }` | Campo culpado, quando atribuível |
| `static string DefaultErrorCode(ErrorKind kind)` | Default do kind, p.ex. `TOO_MANY_REQUESTS` |
| `static Error NotFound(string resource, object? id = null, string? errorCode = null)` | Código `not_found` |
| `static Error Gone(string resource, object? id = null, string? errorCode = null)` | Código `gone` |
| `static Error Conflict(string resource, string? reason = null, string? errorCode = null)` | Código `conflict` |
| `static Error Validation(string field, string? code = null, object? attemptedValue = null, string? errorCode = null)` | Código `validation` ou `code`; preenche `Field` |
| `static Error BadRequest(string? reason = null, string? errorCode = null)` | Código `bad_request` |
| `static Error Unauthorized(string? reason = null, string? errorCode = null)` | Código `unauthorized` |
| `static Error Forbidden(string? reason = null, string? errorCode = null)` | Código `forbidden` |
| `static Error PreconditionFailed(string? reason = null, string? errorCode = null)` | Código `precondition_failed` |
| `static Error Unprocessable(string? reason = null, string? errorCode = null)` | Código `unprocessable` |
| `static Error TooManyRequests(string? reason = null, string? errorCode = null)` | Código `too_many_requests` |
| `static Error ServiceUnavailable(string? reason = null, string? errorCode = null)` | Código `service_unavailable`. O catálogo default não interpola `{reason}` |
| `static Error Timeout(string? reason = null, string? errorCode = null)` | Código `timeout`. O catálogo default não interpola `{reason}` |
| `static Error Unexpected(string? detail = null, string? errorCode = null)` | Código `unexpected`; `detail` é apenas diagnóstico |
| `static Error Custom(string code, ErrorKind kind, object? arguments = null, string? field = null, string? errorCode = null)` | Erro de regra de negócio. Lança `ArgumentException` com código em branco |
| `DomainException ToException()` | Escape hatch |
| `bool Equals(Error?)`, `operator ==`, `operator !=` | Igualdade por valor, incluindo `ErrorCode` e argumentos |

`errorCode` em branco ou só com espaços usa `DefaultErrorCode(Kind)`; senão é aparado. O construtor é interno; a construção passa pelas factories.

### Result

```csharp
public readonly struct Result
```

| Membro | Descrição |
|---|---|
| `bool IsSuccess { get; }` / `bool IsFailure { get; }` | Desfecho |
| `IReadOnlyList<Error> Errors { get; }` | Erros na falha; vazio no sucesso |
| `TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)` | Ramifica para um valor |
| `static Result Success()` | Sucesso |
| `static Result Failure(params Error[] errors)` | Falha. Lança `ArgumentException` se vazio |
| `static Result Failure(IEnumerable<Error> errors)` | Falha a partir de uma sequência, copiada na hora |
| `static Result Combine(params Result[] results)` | Funde, concatenando erros na ordem dos argumentos |
| `static Result Combine<T>(params Result<T>[] results)` | Funde resultados com valor, descartando os valores |
| `Result RecordTo(IDomainErrorRecorder recorder, IReadOnlyDictionary<string, string>? properties = null)` | Grava cada erro; sucesso não faz nada. Hosts HTTP que chamam `ToHttpResult` não precisam disto |

`default(Result)` é sucesso.

### Result&lt;T&gt;

```csharp
public readonly struct Result<T>
```

| Membro | Descrição |
|---|---|
| `bool IsSuccess { get; }` / `bool IsFailure { get; }` | Desfecho |
| `T Value { get; }` | O valor. Lança `InvalidOperationException` na falha |
| `IReadOnlyList<Error> Errors { get; }` | Erros na falha; vazio no sucesso |
| `bool TryGetValue(out T value)` | Leitura que não lança |
| `TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)` | Ramifica para um valor |
| `Result<TOut> Map<TOut>(Func<T, TOut> map)` | Transforma o valor; short-circuit na falha |
| `Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)` | Encadeia operação falível; short-circuit na falha |
| `static Result<T> Success(T value)` | Sucesso |
| `static Result<T> Failure(params Error[] errors)` | Falha. Lança `ArgumentException` se vazio |
| `static Result<T> Failure(IEnumerable<Error> errors)` | Falha a partir de uma sequência, copiada na hora |
| `Result<T> RecordTo(IDomainErrorRecorder recorder, IReadOnlyDictionary<string, string>? properties = null)` | Igual a `Result.RecordTo` |

Sem conversão implícita de `T`, e sem `Apply` — veja [ausências deliberadas](domain-guide.md#ausências-deliberadas).

### IDomainErrorRecorder

```csharp
public interface IDomainErrorRecorder
{
    void Record(Error error, IReadOnlyDictionary<string, string>? properties = null);
}
```

Implementada por `AddOffsideOpenTelemetry` e `AddOffsideApplicationInsights`. Hosts HTTP não chamam `RecordTo` — o pipeline de problem grava quando isto está registrado. Implementações não devem lançar.

### DomainErrorSeverity

```csharp
public enum DomainErrorSeverity { Verbose, Information, Warning, Error, Critical }
```

Espelha os nomes de severidade do SDK clássico de Application Insights.

### DomainErrorSeverityMap

```csharp
public static class DomainErrorSeverityMap
{
    public static DomainErrorSeverity Library(ErrorKind kind);
    public static DomainErrorSeverity Operations(ErrorKind kind);
}
```

`Library` é o padrão nos dois pacotes de telemetria (404/400 = Information, Unexpected = Critical). `Operations` sobe recusas incluindo NotFound/Validation/BadRequest para Warning e desce Unexpected para Error.

### DomainErrorMessageFormat

```csharp
public static class DomainErrorMessageFormat
{
    public static readonly Func<Error, string, string> MessageOnly;
    public static readonly Func<Error, string, string> CodePrefixed;
    public static readonly Func<Error, string, string> ErrorCodePrefixed;
}
```

Molda só a linha de log ou trace. As dimensões não são afetadas.

### ErrorArgumentFilter

```csharp
public static class ErrorArgumentFilter
{
    public static IEnumerable<KeyValuePair<string, object?>> Select(
        Error error, bool includeAll, IReadOnlyCollection<string>? keys);
}
```

Usado pelos dois recorders. `includeAll: true` ignora `keys`. Valores nulos de argumento são pulados.

### DomainException

```csharp
public sealed class DomainException : Exception
{
    public IReadOnlyList<Error> Errors { get; }
    public DomainException(IReadOnlyList<Error> errors);
}
```

`Message` é o `Code` do primeiro erro. Produzida por `Error.ToException()`.

### IErrorMessageResolver

```csharp
public interface IErrorMessageResolver
{
    string GetMessage(Error error, CultureInfo culture);
}
```

Implemente para buscar mensagens fora do JSON. Por convenção, devolva `error.Code` quando nenhuma mensagem for encontrada.

### ErrorMessageTemplate

```csharp
public static class ErrorMessageTemplate
{
    public static string Interpolate(string template, IReadOnlyDictionary<string, object?> arguments);
}
```

Interpolação compartilhada pelos resolvers nativos. Argumentos nulos e tokens sem correspondência permanecem literais.

### JsonErrorCatalog

```csharp
public sealed class JsonErrorCatalog
{
    public CultureInfo Culture { get; }
    public Stream Json { get; }
    public JsonErrorCatalog(CultureInfo culture, Stream json);
}
```

Lança `ArgumentNullException` com cultura ou stream nulos.

### JsonErrorMessageResolver

```csharp
public sealed class JsonErrorMessageResolver : IErrorMessageResolver
{
    public JsonErrorMessageResolver(IEnumerable<JsonErrorCatalog> catalogs);
    public string GetMessage(Error error, CultureInfo culture);
}
```

Parseia todos os catálogos no construtor. Lança `InvalidOperationException` quando nenhum catálogo invariante é fornecido. Ordem de busca: cultura exata → pai → invariante; depois o próprio código.

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

`AddJson` recebe o **conteúdo** do catálogo, não um caminho. `AddJsonFile` lê o arquivo (caminhos relativos resolvem contra `AppContext.BaseDirectory`) e lança `FileNotFoundException` nomeando o path resolvido. `AddJsonFromAssembly` copia um resource embutido e lança `InvalidOperationException` nomeando o resource ausente. Fluente.

### OffsideServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffside(this IServiceCollection services, Action<OffsideOptions> configure);
```

Constrói um `JsonErrorMessageResolver` de forma ansiosa e o registra como o singleton `IErrorMessageResolver`.

## Offside.MediatR

Namespace `Offside.MediatR`. O pacote depende do MediatR no intervalo `[12.0.1,15.0.0)`; o pacote Core do Offside permanece independente.

### DomainNotification

```csharp
public sealed class DomainNotification : INotification
{
    public DomainNotification(Error error);
    public Error Error { get; }
}
```

Carrega exatamente um erro não nulo. É uma notificação de erro, não um domain event que descreve mudança de estado.

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

O coletor é scoped e thread-safe. `Errors` é um snapshot independente; leituras nunca limpam o estado. Os dois métodos de resultado devolvem sucesso quando vazio e falha com todos os erros coletados nos demais casos.

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

Sucesso não publica nada. Falha publica uma notificação por erro, sequencialmente e na ordem do Result, e devolve o resultado original. Cancelamento e exceções de handlers interrompem as publicações restantes e são propagados imediatamente.

### OffsideMediatRServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffsideMediatR(this IServiceCollection services);
```

Registra de forma idempotente o coletor scoped e seu handler. Não chama `AddMediatR`, não registra `IPublisher` e não configura licenciamento. Veja o [guia do MediatR](mediatr-guide.md).

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

Lê dinamicamente `Errors:<cultura>:<código>`, com fallback cultura exata → pai → `default`. O catálogo padrão é obrigatório. Conexão com Azure, labels e refresh são configurados pelo host; não chame também `AddOffside`.

## Offside.AspNetCore

Namespace `Offside.AspNetCore`.

### OffsideAspNetCoreOptions

```csharp
public sealed class OffsideAspNetCoreOptions
{
    public bool ExposeExceptionDetails { get; set; }
    public bool LogUnexpected { get; set; }            // true sem recorder; false quando há um, a menos que definido explicitamente
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

`ExposeExceptionDetails` controla apenas o campo `debug`; o `detail` visível ao cliente em um 500 é sempre a mensagem genérica. `LogUnexpected` controla a linha built-in de `ILogger` para falhas `Unexpected` e assume desligada quando há um `IDomainErrorRecorder` registrado. `TelemetryProperties` é mesclado em toda gravação do pipeline (`HttpStatus` é sempre escrito). `LegacyAliases.MessageReasonAndTechnicalDetail` acrescenta `message`, `errors[].name`, `errors[].reason` e `technicalDetail`. `CustomizeProblem` pode acrescentar membros JSON achatados via `Extensions`; chaves reservadas são removidas. `OnProblem` é um gancho do host — não emite telemetria e não deve escrever no body da resposta. `ResolveTraceId` substitui o default de 32 hex de `Activity.TraceId`. As sobrecargas `bool exposeExceptionDetails` estão obsoletas e constroem options sem esses callbacks; os ganchos exigem o caminho `HttpContext` / DI ou um objeto de options explícito. `ToHttpResult(HttpContext)` lança `InvalidOperationException` nomeando `AddOffsideAspNetCore` quando o singleton está ausente.

### OffsideAspNetCoreServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffsideAspNetCore(
    this IServiceCollection services,
    Action<OffsideAspNetCoreOptions>? configure = null);
```

Registra `OffsideAspNetCoreOptions` como singleton, com `ExposeExceptionDetails` vindo de `IHostEnvironment.IsDevelopment()` quando há um presente, senão `false`. `configure` roda depois e prevalece.

### OffsideProblem

```csharp
public sealed class OffsideProblem
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public int Status { get; init; }
    public required string Detail { get; init; }
    public required string TraceId { get; init; }
    public required string ErrorCode { get; init; }  // identificador de tela do primário
    public string? Debug { get; init; }              // omitido do JSON quando nulo
    public required IReadOnlyList<Item> Errors { get; init; }
    public IDictionary<string, object?> Extensions { get; init; }  // achatado via [JsonExtensionData]

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

Serializado como `application/problem+json` com nomes em camelCase. Campos extras em `Extensions` são achatados no JSON. Um 500 sanitizado força `errorCode` para `UNEXPECTED`. Veja [o formato da resposta](aspnet-guide.md#mapeamento-de-falha).

### OffsideHttp

```csharp
public static class OffsideHttp
{
    public static IReadOnlyList<int> StatusCodes { get; }  // 400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504
    public static int StatusCode(ErrorKind kind);
    public static Error SelectPrimary(IReadOnlyList<Error> errors);
}
```

O mapeamento kind → HTTP usado pelo Problem Details e pelo `Offside.FastEndpoint`. `SelectPrimary` devolve o erro do kind mais severo; lista vazia lança `ArgumentException`.

### ResultHttpExtensions

```csharp
public static class ResultHttpExtensions
```

Minimal APIs — sucesso é `204 No Content` para `Result`, `200 OK` com o valor para `Result<T>`:

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

Controllers MVC — sucesso é `NoContentResult` / `OkObjectResult`:

```csharp
IActionResult ToActionResult(this Result result, IErrorMessageResolver resolver, CultureInfo culture, bool exposeExceptionDetails = false);
IActionResult ToActionResult(this Result result, IErrorMessageResolver resolver, CultureInfo? culture, OffsideAspNetCoreOptions options);

IActionResult ToActionResult<T>(this Result<T> result, IErrorMessageResolver resolver, bool exposeExceptionDetails = false);
IActionResult ToActionResult<T>(this Result<T> result, IErrorMessageResolver resolver, CultureInfo culture, bool exposeExceptionDetails = false);
IActionResult ToActionResult<T>(this Result<T> result, IErrorMessageResolver resolver, CultureInfo? culture, OffsideAspNetCoreOptions options);
```

Note a assimetria: **não existe** `ToActionResult(this Result, IErrorMessageResolver, bool)` para o `Result` unitário. Passe uma cultura, ou passe `null` pela sobrecarga com options.

As sobrecargas `bool exposeExceptionDetails` estão obsoletas. Uma cultura `null` significa "derive do `Accept-Language`". Todas as sobrecargas lançam `ArgumentNullException` com resolver, options ou `HttpContext` nulos. As sobrecargas com `HttpContext` também lançam `InvalidOperationException` quando `OffsideAspNetCoreOptions` não está registrado.

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

`.WithErrorCode("email.taken")` vira `Error.Code`. Os nomes default `*Validator` do FluentValidation (e códigos em branco) viram `validation`. `Error.ErrorCode` é `VALIDATION`. `PropertyName` vazio define `Field` como null. Veja [FluentValidation](fluentvalidation.md).

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

`UseOffside` define o `ResponseBuilder` de validação para `OffsideProblem`, `ProducesMetadataType` para `typeof(OffsideProblem)`, content type `application/problem+json`, e registra `Produces<OffsideProblem>` para cada valor de `OffsideHttp.StatusCodes`. `SendOffsideAsync` reusa `ToHttpResult`. Veja [FastEndpoints](fastendpoints.md).

## Offside.Refit

Namespace `Offside.Refit`. Alvos `netstandard2.0`, `net8.0`, `net10.0`.

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

O mapeamento de status é 404 → `NotFound`, 502/503 → `ServiceUnavailable`, 504 → `Timeout`, demais 5xx → `Unexpected`, demais 4xx → `BadRequest`. Depois desse mapeamento (ou depois de restaurar um problem body), `InboundStatus` por padrão dobra todo kind 4xx em `ServiceUnavailable`. `Mirror` preserva o kind da dependência. `CallAsync` converte apenas `ApiException`, timeouts e falhas de transporte; um cancelamento pedido pelo chamador é relançado. O parsing do problem body nunca lança. Veja [Integração com Refit](refit.md).

## Offside.ApplicationInsights

Namespace `Offside.ApplicationInsights`. Alvos `netstandard2.0`, `net8.0`, `net10.0`. `IDomainErrorRecorder`, `DomainErrorSeverity` e `DomainErrorMessageFormat` ficam em `Offside`.

```csharp
public sealed class OffsideApplicationInsightsOptions
{
    public string PropertyPrefix { get; set; }              // "offside."
    public bool IncludeArguments { get; set; }              // false
    public IReadOnlyCollection<string> IncludeArgumentKeys { get; set; }  // vazio
    public CultureInfo Culture { get; set; }                // InvariantCulture
    public Func<ErrorKind, DomainErrorSeverity> SeverityFor { get; set; }  // Library
    public Func<Error, string, string> FormatMessage { get; set; }  // MessageOnly
}

public static class OffsideApplicationInsightsServiceCollectionExtensions
{
    public static IServiceCollection AddOffsideApplicationInsights(this IServiceCollection services, Action<OffsideApplicationInsightsOptions>? configure = null);
}
```

Cada erro vira um `TraceTelemetry` com `offside.code`, `offside.errorCode`, `offside.kind` e `offside.field`. As dimensões do Offside vencem as propriedades fornecidas. `Error.Arguments` são escritos como `offside.arg.{nome}` para as chaves em `IncludeArgumentKeys`, ou para todas quando `IncludeArguments` está ligado. O `FormatMessage` molda apenas o texto do trace. Veja [Application Insights](application-insights.md).

## Offside.ApplicationInsights.MediatR

Namespace `Offside.ApplicationInsights.MediatR`. Alvos `netstandard2.0`, `net8.0`, `net10.0`.

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

Idempotente e independente do coletor scoped registrado por `AddOffsideMediatR`.

## Offside.OpenTelemetry

Namespace `Offside.OpenTelemetry`. Alvos `netstandard2.0`, `net8.0`, `net10.0`. `IDomainErrorRecorder`, `DomainErrorSeverity` e `DomainErrorMessageFormat` ficam em `Offside`.

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
    public IReadOnlyCollection<string> IncludeArgumentKeys { get; set; }         // vazio
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

Cada erro vira até três sinais: uma entrada de `ILogger` na categoria `Offside` cujo estado é uma lista de pares chave/valor, um evento `offside.error` na `Activity.Current` e um incremento do contador `offside.errors`. Os dois primeiros carregam `offside.code`, `offside.errorCode`, `offside.kind` e `offside.field`; o contador carrega só `offside.kind` e `offside.code`. `IncludeArgumentKeys` / `IncludeArguments` valem para o log e o evento do span, nunca para o contador. `ActivityFailure.ServerErrors` marca o span para Unexpected / ServiceUnavailable / Timeout. A primeira emissão com `EmitMetric` e sem listener de meter registra um aviso pedindo `AddMeter(OffsideTelemetry.MeterName)`.

O pacote não referencia nenhum assembly do OpenTelemetry ou do Azure. A severidade padrão coincide com a do `Offside.ApplicationInsights`. Veja [OpenTelemetry](open-telemetry.md).

## Offside.OpenTelemetry.MediatR

Namespace `Offside.OpenTelemetry.MediatR`. Alvos `netstandard2.0`, `net8.0`, `net10.0`.

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

Idempotente e independente do coletor scoped registrado por `AddOffsideMediatR`.

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

`Install` devolve todo caminho escrito, na ordem de escrita. Lança `DirectoryNotFoundException` quando a origem das skills ou uma pasta de skill esperada está faltando. Veja a [página do CLI](cli.md).

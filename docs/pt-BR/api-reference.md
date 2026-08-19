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
    PreconditionFailed, Gone, Unprocessable, NotFound, Validation, BadRequest
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
| `ErrorKind Kind { get; }` | Espécie da falha |
| `IReadOnlyDictionary<string, object?> Arguments { get; }` | Snapshot somente leitura dos valores do template |
| `string? Field { get; }` | Campo culpado, quando atribuível |
| `static Error NotFound(string resource, object? id = null)` | Código `not_found` |
| `static Error Gone(string resource, object? id = null)` | Código `gone` |
| `static Error Conflict(string resource, string? reason = null)` | Código `conflict` |
| `static Error Validation(string field, string? code = null, object? attemptedValue = null)` | Código `validation` ou `code`; preenche `Field` |
| `static Error BadRequest(string? reason = null)` | Código `bad_request` |
| `static Error Unauthorized(string? reason = null)` | Código `unauthorized` |
| `static Error Forbidden(string? reason = null)` | Código `forbidden` |
| `static Error PreconditionFailed(string? reason = null)` | Código `precondition_failed` |
| `static Error Unprocessable(string? reason = null)` | Código `unprocessable` |
| `static Error TooManyRequests(string? reason = null)` | Código `too_many_requests` |
| `static Error Unexpected(string? detail = null)` | Código `unexpected`; `detail` é apenas diagnóstico |
| `static Error Custom(string code, ErrorKind kind, object? arguments = null, string? field = null)` | Erro de regra de negócio. Lança `ArgumentException` com código em branco |
| `DomainException ToException()` | Escape hatch |
| `bool Equals(Error?)`, `operator ==`, `operator !=` | Igualdade por valor, argumentos incluídos |

O construtor é interno; a construção passa pelas factories.

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

Sem conversão implícita de `T`, e sem `Apply` — veja [ausências deliberadas](domain-guide.md#ausências-deliberadas).

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
}
```

As duas sobrecargas recebem o **conteúdo** do catálogo, não um caminho. Fluente.

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
    public static OffsideAspNetCoreOptions FromEnvironment(IHostEnvironment environment);
}
```

`ExposeExceptionDetails` controla apenas o campo `debug`; o `detail` visível ao cliente em um 500 é sempre a mensagem genérica.

### OffsideAspNetCoreServiceCollectionExtensions

```csharp
public static IServiceCollection AddOffsideAspNetCore(this IServiceCollection services);
```

Registra `OffsideAspNetCoreOptions` como singleton, com `ExposeExceptionDetails` vindo de `IHostEnvironment.IsDevelopment()` quando há um presente, senão `false`.

### OffsideProblem

```csharp
public sealed class OffsideProblem
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public int Status { get; init; }
    public required string Detail { get; init; }
    public required string TraceId { get; init; }
    public string? Debug { get; init; }              // omitido do JSON quando nulo
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

Serializado como `application/problem+json` com nomes em camelCase. Veja [o formato da resposta](aspnet-guide.md#mapeamento-de-falha).

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

Uma cultura `null` significa "derive do `Accept-Language`". Todas as sobrecargas lançam `ArgumentNullException` com resolver, options ou `HttpContext` nulos.

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

# Guia ASP.NET Core

*[English](../aspnet-guide.md) · [Voltar às docs](README.md)*

`Offside.AspNetCore` transforma um `Result` em uma resposta HTTP. É a única camada que conhece status codes — o domínio permanece agnóstico de transporte.

## Registro

```csharp
builder.Services.AddOffside(options => { /* catálogos */ });
builder.Services.AddOffsideAspNetCore();
```

`AddOffsideAspNetCore` registra `OffsideAspNetCoreOptions`. Quando há um `IHostEnvironment` no container, `ExposeExceptionDetails` assume `IsDevelopment()`. Passe um callback de configure para definir ganchos depois — ele prevalece sobre o default do ambiente:

```csharp
builder.Services.AddOffsideAspNetCore(options =>
{
    options.LogUnexpected = false;
    options.OnProblem = (problem, errors, http) => { /* sua telemetria */ };
});
```

## Minimal APIs

```csharp
app.MapGet("/orders/{id}", (string id, OrderService orders, HttpContext http) =>
    orders.Get(id).ToHttpResult(http));

app.MapPost("/orders", (CreateOrder cmd, OrderHandler handler, HttpContext http) =>
    handler.Handle(cmd).ToHttpResult(http));
```

A sobrecarga com `HttpContext` é a que se deve usar. Ela resolve o `IErrorMessageResolver` e as options a partir dos request services e deriva a cultura do header `Accept-Language`.

## Controllers MVC

```csharp
public sealed class OrdersController(OrderService orders, IErrorMessageResolver resolver) : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(string id) =>
        orders.Get(id).ToActionResult(resolver, CultureInfo.CurrentUICulture);
}
```

## Mapeamento de sucesso

| Resultado | Resposta |
|---|---|
| `Result.Success()` | `204 No Content` |
| `Result<T>.Success(value)` | `200 OK` com `value` no corpo |

Para um `201 Created` ou qualquer outro formato de sucesso, faça o branch antes de converter — `ToHttpResult` cuida do caminho de falha e você mantém controle total do caminho de sucesso:

```csharp
app.MapPost("/orders", (CreateOrder cmd, OrderHandler handler, HttpContext http) =>
{
    var result = handler.Handle(cmd);
    return result.IsSuccess
        ? Results.Created($"/orders/{result.Value.Id}", result.Value)
        : result.ToHttpResult(http);
});
```

## Mapeamento de falha

Toda falha produz o mesmo corpo, `application/problem+json` com nomes em camelCase:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "O pedido 42 já foi enviado.",
  "errorCode": "ORDER_ALREADY_SHIPPED",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": [
    {
      "code": "order.already_shipped",
      "errorCode": "ORDER_ALREADY_SHIPPED",
      "kind": "Conflict",
      "detail": "O pedido 42 já foi enviado.",
      "field": null
    }
  ]
}
```

| Campo | Significado |
|---|---|
| `type` | `https://httpstatuses.io/{status}` |
| `title` | O `ErrorKind` do erro primário, como string |
| `status` | Derivado do kind mais severo presente |
| `detail` | A mensagem resolvida do erro primário |
| `errorCode` | O identificador de tela do erro primário |
| `traceId` | `Activity.Current.TraceId` (32 hex), caindo para `HttpContext.TraceIdentifier`. Substitua com `ResolveTraceId` |
| `errors` | Todos os erros do resultado, na ordem em que o domínio os reportou |
| `errors[].code` | Chave do catálogo (`order.already_shipped`) |
| `errors[].errorCode` | Identificador de tela (`ORDER_ALREADY_SHIPPED`) |
| `debug` | Presente apenas em um 500 com `ExposeExceptionDetails` ligado; omitido nos demais casos |

Campos extras adicionados por `CustomizeProblem` são achatados no documento (e em `errors[]`) como o `ProblemDetails.Extensions` do ASP.NET. Chaves que colidem com o contrato (`type`, `title`, `status`, `detail`, `instance`, `traceId`, `errorCode`, `debug`, `errors`) são removidas. Use primitivos seguros para JSON.

Clientes devem fazer branch em `errorCode` (topo ou `errors[].errorCode`), não em `detail`. `code` é a chave do catálogo de mensagens.

## Status codes

| ErrorKind | Status |
|---|---|
| `Unexpected` | 500 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `TooManyRequests` | 429 |
| `Conflict` | 409 |
| `PreconditionFailed` | 412 |
| `Gone` | 410 |
| `Unprocessable` | 422 |
| `NotFound` | 404 |
| `Validation` | 400 |
| `BadRequest` | 400 |
| `ServiceUnavailable` | 503 |
| `Timeout` | 504 |

O mesmo mapeamento é `OffsideHttp.StatusCode(kind)`. `OffsideHttp.StatusCodes` é o conjunto distinto (400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504) usado como respostas esperadas. `OffsideHttp.SelectPrimary` escolhe o erro que dirige o status quando você escreve uma resposta própria.

## Escolhendo o erro primário

Quando um resultado carrega vários erros, a resposta reflete o **kind mais severo**, não o primeiro erro. Severidade, do mais severo para o menos:

| Rank | Kinds |
|---|---|
| 0 | `Unexpected` |
| 1 | `Unauthorized`, `Forbidden` |
| 2 | `TooManyRequests` |
| 3 | `ServiceUnavailable`, `Timeout` |
| 4 | `Conflict` |
| 5 | `PreconditionFailed` |
| 6 | `Gone` |
| 7 | `Unprocessable` |
| 8 | `NotFound` |
| 9 | `Validation`, `BadRequest` |

**Empates vão para o primeiro erro do resultado.** `Unauthorized` e `Forbidden` compartilham o rank 1, então um resultado que carrega os dois reporta aquele que o domínio listou primeiro. Auth e rate-limit vencem 503/504 para o cliente não ser instruído a retentar um pedido não autenticado ou limitado.

```csharp
Result.Failure(
    Error.Validation("email"),          // 400
    Error.Conflict("order", "dup"),     // 409  ← mais severo, vence
    Error.NotFound("order", 1));        // 404
// → status 409, title "Conflict", e os três erros no array errors
```

Ordenar por severidade em vez de por posição significa que uma falha genuína nunca é mascarada por uma mensagem de validação que por acaso foi adicionada primeiro. E nada se perde nos dois casos: a lista completa sempre é enviada.

## Erros inesperados e 500

`ErrorKind.Unexpected` é tratado de forma diferente, porque seu detalhe é material de diagnóstico e não algo que um cliente deva ler.

Quando o kind vencedor é `Unexpected`:

1. O `detail` de todo erro inesperado é substituído pela mensagem genérica `unexpected` do catálogo — tanto no `detail` de topo quanto nas entradas de `errors`.
2. O `errorCode` de todo erro inesperado é forçado para `UNEXPECTED`.
3. O detalhe real aparece em `debug` **apenas** quando `ExposeExceptionDetails` está ligado.
4. A falha é logada via `ILoggerFactory` na categoria `Offside.AspNetCore`, junto com o `traceId`, a menos que `LogUnexpected` seja `false`.

```csharp
return Result.Failure(Error.Unexpected(ex.ToString()));
```

Em produção:

```json
{
  "type": "https://httpstatuses.io/500",
  "title": "Unexpected",
  "status": 500,
  "detail": "Ocorreu um erro inesperado.",
  "errorCode": "UNEXPECTED",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errors": [
    { "code": "unexpected", "errorCode": "UNEXPECTED", "kind": "Unexpected", "detail": "Ocorreu um erro inesperado.", "field": null }
  ]
}
```

Em desenvolvimento, a mesma resposta ganha um campo `"debug": "System.InvalidOperationException: ..."`. O `detail` visível ao cliente é genérico nos dois casos — `ExposeExceptionDetails` controla apenas o `debug`.

O `traceId` é a ponte: aparece na resposta e na linha de log, então o usuário pode citá-lo e você encontra a causa real. O default é o `TraceId` W3C de 32 hex (o valor que o Application Insights guarda como `operation_Id`), não o traceparent completo em `Activity.Id`. Restaure o formato antigo com `ResolveTraceId`:

```csharp
builder.Services.AddOffsideAspNetCore(options =>
{
    options.ResolveTraceId = http =>
        Activity.Current?.Id ?? http.TraceIdentifier;
});
```

Defina as options explicitamente se não quiser depender do ambiente. Prefira o callback de configure em `AddOffsideAspNetCore`; registrar o próprio singleton ainda funciona:

```csharp
builder.Services.AddSingleton(new OffsideAspNetCoreOptions { ExposeExceptionDetails = false });
```

Ou construa direto no ponto de chamada (esta forma não tem ganchos de DI a menos que você os coloque no objeto):

```csharp
result.ToHttpResult(resolver, culture: null, new OffsideAspNetCoreOptions { ExposeExceptionDetails = false });
```

## Customizando o documento e observando falhas

`CustomizeProblem` roda depois que o documento é montado. As propriedades centrais continuam `init`; acrescente campos legados ou do host por `Extensions` (e `Item.Extensions`). Mantenha valores seguros para JSON. Um callback que lança é logado em `Offside.AspNetCore` e o documento ainda é escrito.

`OnProblem` roda em seguida, com o `HttpContext`. Use-o para um único evento de telemetria do host. Ponha `LogUnexpected` em `false` quando esse callback for dono do log, senão um 500 é logado duas vezes. Deixar `LogUnexpected` falso e `OnProblem` nulo significa um 500 silencioso. 503 e 504 não são logados pelo Offside; observe-os em `OnProblem` se precisar.

```csharp
builder.Services.AddOffsideAspNetCore(options =>
{
    options.LogUnexpected = false;
    options.CustomizeProblem = (problem, errors) =>
    {
        problem.Extensions["message"] = problem.Detail;
    };
    options.OnProblem = (problem, errors, http) =>
    {
        // um evento, template constante
    };
});
```

O `ResponseBuilder` de validação do FastEndpoints usa o mesmo pipeline, então os ganchos e o `traceId` de 32 hex valem lá também.

## Culturas

Quando nenhuma cultura é passada, ela vem do header `Accept-Language` da requisição — o primeiro range, sem o quality value. `Accept-Language: pt-BR,pt;q=0.9` resolve para `pt-BR`, que cai para `pt` e depois para o catálogo invariante.

O header cai para `CultureInfo.CurrentUICulture` quando está ausente, vazio, é `*`, ou não é um nome de cultura reconhecido. Um header malformado nunca derruba uma requisição.

Veja [Mensagens e culturas](messages.md) para a resolução de catálogo.

## Referência de sobrecargas

| Método | Cultura | Options |
|---|---|---|
| `ToHttpResult(resolver, exposeExceptionDetails?)` | `CurrentUICulture` | flag |
| `ToHttpResult(resolver, culture, exposeExceptionDetails?)` | explícita | flag |
| `ToHttpResult(resolver, culture?, options)` | explícita ou `Accept-Language` | objeto |
| `ToHttpResult(httpContext)` | `Accept-Language` | do DI |
| `ToActionResult(resolver, culture, exposeExceptionDetails?)` | explícita | flag |
| `ToActionResult(resolver, culture?, options)` | explícita ou `Accept-Language` | objeto |

Cada linha existe para `Result` e `Result<T>`, com uma exceção: **não existe `ToActionResult(resolver, exposeExceptionDetails?)` para o `Result` não genérico.** A forma genérica tem; a unitária não. Passe uma cultura explicitamente, ou passe `null` pela sobrecarga com options para cair no `Accept-Language`.

## Regras práticas

- Nunca referencie `Offside.AspNetCore` de um projeto de domínio ou aplicação. Status codes são preocupação de transporte.
- Não construa um segundo formato de erro ao lado deste. Um único formato em toda a API é a maior parte do valor.
- Mantenha segredos fora de `Error.Arguments` — eles acabam nas mensagens, e mensagens são enviadas.
- Faça clientes decidirem por `errorCode`, nunca por `detail`.
- Coloque falhas operacionais de dependência em `ErrorKind.ServiceUnavailable` / `Timeout`, não em `Unexpected`. Não coloque texto de exceção em templates `{reason}`.

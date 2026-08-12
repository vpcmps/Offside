# Offside — spec de design

Data: 2026-08-12  
Estado: aprovado em conversa; à espera de revisão do ficheiro

**Nome de mercado:** Offside.  
**Tagline:** the domain called offside.  
**Pacotes NuGet:** `Offside`, `Offside.AspNetCore`.  
**Namespace:** `Offside` / `Offside.AspNetCore`. Os tipos continuam `Error`, `ErrorKind`, `Result`.

Biblioteca .NET para erros de domínio: o domínio e a aplicação devolvem `Result` com uma lista de `Error`; a borda HTTP traduz isso em Problem Details (RFC 7807). Mensagens traduzíveis vivem em JSON por cultura; metadados (espécie, HTTP, `type`) vivem no C#.

## 1. Objectivos

- Um contrato único de erro reutilizável em API, worker, gRPC e CLI.
- Regras de negócio não lançam exceções; devolvem `Result` / `Result<T>`.
- Mensagens não estão hardcoded no C#; JSON por cultura com fallback.
- ASP.NET não decide regras: só mapeia `Result` → HTTP.

Fora de âmbito (v1): source generators, FluentValidation, gRPC interceptors, persistência de erros, UI.

## 2. Pacotes e targets

| Pacote | Targets | Responsabilidade |
|---|---|---|
| `Offside` | `netstandard2.0;net8.0;net10.0` | `Error`, `ErrorKind`, `Result`/`Result<T>`, factories, `Combine`, resolvedor JSON |
| `Offside.AspNetCore` | `net8.0;net10.0` | `ToHttpResult` / `IActionResult`, Problem Details, DI, 500 sanitizado |

Dependência: `Offside.AspNetCore` → `Offside`. O pacote Core **não** referencia ASP.NET.

Solução em `C:\Users\vpcam\dev\Offside` (pasta actual: `DomainErrors`; renomear na implementação).

## 3. Modelo de erro

### 3.1 `ErrorKind`

Espécies built-in e status HTTP default:

| Kind | Status | Code default | `type` RFC 7807 |
|---|---|---|---|
| `Unexpected` | 500 | `unexpected` | `https://httpstatuses.io/500` |
| `Unauthorized` | 401 | `unauthorized` | `https://httpstatuses.io/401` |
| `Forbidden` | 403 | `forbidden` | `https://httpstatuses.io/403` |
| `TooManyRequests` | 429 | `too_many_requests` | `https://httpstatuses.io/429` |
| `Conflict` | 409 | `conflict` | `https://httpstatuses.io/409` |
| `PreconditionFailed` | 412 | `precondition_failed` | `https://httpstatuses.io/412` |
| `Gone` | 410 | `gone` | `https://httpstatuses.io/410` |
| `Unprocessable` | 422 | `unprocessable` | `https://httpstatuses.io/422` |
| `NotFound` | 404 | `not_found` | `https://httpstatuses.io/404` |
| `Validation` | 400 | `validation` | `https://httpstatuses.io/400` |
| `BadRequest` | 400 | `bad_request` | `https://httpstatuses.io/400` |

`ErrorKind` **não** é extensível na v1. Erros de regra de negócio usam `Error.Custom(code, kind, args)` reutilizando um Kind existente para o HTTP. Não há `httpStatus` livre no `Error` v1.

### 3.2 `Error`

```text
Error
  Code: string              // chave no JSON; built-in = code default do Kind
  Kind: ErrorKind
  Arguments: IReadOnlyDictionary<string, object?>
  Field: string?            // preenchido por Error.Validation; opcional em Custom
```

Invariantes:

- `Code` não vazio.
- `Arguments` imutável; chaves usadas como placeholders `{chave}` nas mensagens.
- Igualdade por `Code`, `Kind`, `Field` e `Arguments`.
- `ToException()` devolve `DomainException` (mensagem = `Code`; `Errors` expostos na exceção). Só escape (bugs / invariantes), não caminho de domínio.

Não interpolar HTML. Não colocar segredos em `Arguments`. Números/datas formatados com cultura invariante na interpolação.

### 3.3 Factories

```csharp
Error.NotFound(string resource, object? id = null)
Error.Gone(string resource, object? id = null)
Error.Conflict(string resource, string? reason = null)
Error.Validation(string field, string? code = null, object? attemptedValue = null)
    // se `code` for passado, torna-se Error.Code (chave JSON); senão "validation"
Error.BadRequest(string? reason = null)
Error.Unauthorized(string? reason = null)
Error.Forbidden(string? reason = null)
Error.PreconditionFailed(string? reason = null)
Error.Unprocessable(string? reason = null)
Error.TooManyRequests(string? reason = null)
Error.Unexpected(string? detail = null) // `detail` é para log; não vai no HTTP de produção
Error.Custom(string code, ErrorKind kind, object? arguments = null, string? field = null)
```

`arguments` em `Custom` aceita `IDictionary<string, object?>` ou objeto anónimo convertido a dicionário.

Argumentos típicos das factories built-in: `resource`, `id`, `reason`, `field`, `attemptedValue`.

### 3.4 Regras de negócio (Custom)

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
```

A mensagem vive no JSON sob a chave `order.already_shipped`. O Kind continua a decidir o HTTP.

## 4. `Result` / `Result<T>`

`Result` e `Result<T>` são `readonly struct`.

- `IsSuccess` / `IsFailure`
- `Errors`: `IReadOnlyList<Error>` (vazia em sucesso)
- `Result.Success()` / `Result<T>.Success(value)`
- `Failure(params Error[] errors)` e `Failure(IEnumerable<Error> errors)`
- Conversão implícita `T` → `Result<T>`: **não** na v1 (esconde falhas)

Invariantes (estados ilegais):

- `Failure` sem erros → throw na construção.
- Ler `Value` em falha → throw. Usar `TryGetValue` / `Match`.

Combinators:

| Método | Comportamento |
|---|---|
| `Map` | Transforma o valor; short-circuit em falha |
| `Bind` | Encadeia `Result`; short-circuit em falha |
| `Match` | Bifurca sucesso / falha |
| `Combine` | Junta erros de vários `Result` (validação de vários campos). Nome único na v1; não há `Apply`. |

`Bind` não substitui `Combine`. Sem `Combine`, a lista de erros quase não seria usada.

## 5. Mensagens JSON (Core)

O resolvedor vive no **Core**. A borda HTTP só escolhe a `CultureInfo` do pedido. Workers/CLI passam a cultura que quiserem.

Interface:

```csharp
string GetMessage(Error error, CultureInfo culture);
```

Ficheiros: `errors.{culture}.json` (ex. `errors.pt-BR.json`, `errors.en.json`) e `errors.json` como default.

Formato:

```json
{
  "not_found": "{resource} '{id}' não foi encontrado.",
  "order.already_shipped": "A encomenda {orderId} já foi expedida."
}
```

Política de fallback:

1. Cultura pedida (ex. `pt-BR`) → pai (`pt`) → `errors.json`.
2. Chave em falta → devolver `Error.Code` (nunca throw em runtime por mensagem).
3. `errors.json` default em falta no arranque → falhar o startup.
4. Placeholder `{x}` sem argumento → deixar o token na string (não throw).

Descoberta: o Core **não** conhece `ContentRoot`. O host regista streams/ficheiros via DI (`IOptions` / `AddOffside(...)`). O Core só lê `Stream` + `CultureInfo`.

## 6. ASP.NET Core

`AddOffside()`:

- regista o resolvedor JSON e as fontes de ficheiros;
- usa cultura do pedido (`Accept-Language` / `CurrentUICulture`);
- opções de mapeamento e `ExposeExceptionDetails` (default = `IHostEnvironment.IsDevelopment()`).

Mapeamento de `Result`:

- Minimal APIs: `IResult ToHttpResult(this Result)` e `ToHttpResult<T>(this Result<T>)`.
- Controllers: overload `IActionResult`.
- Content-Type: `application/problem+json`.

### 6.1 Status com N erros

O Kind **mais grave** manda. Ordem (mais → menos grave):

1. `Unexpected`
2. `Unauthorized`, `Forbidden` (mesmo nível; desempate = primeiro na lista `Errors`)
3. `TooManyRequests`
4. `Conflict`
5. `PreconditionFailed`
6. `Gone`
7. `Unprocessable`
8. `NotFound`
9. `Validation`, `BadRequest` (mesmo nível; desempate = primeiro na lista `Errors`)

`title` / `type` / `status` vêm do Kind vencedor. `detail` é a mensagem resolvida do **erro primário** (primeiro erro desse Kind na lista). Todos os erros vão em `errors[]`.

Se o Kind vencedor for `Unexpected`, aplica-se o caminho 500 sanitizado (§6.3), mesmo que a lista tenha outros erros.

### 6.2 Shape canónico

Um único JSON, sempre:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "mensagem do erro primário",
  "traceId": "00-…",
  "errors": [
    {
      "code": "order.already_shipped",
      "kind": "Conflict",
      "detail": "A encomenda 123 já foi expedida.",
      "field": null
    }
  ]
}
```

Não há `ValidationProblemDetails` paralelo na v1.

### 6.3 500 e escape

Exceções não tratadas e `ErrorKind.Unexpected`:

- Cliente: `status` 500, `title`/`detail` genéricos (localizados se o JSON tiver `unexpected`), `traceId`, `errors[]` sem detalhe interno.
- Log: mensagem real, args, exception, stack, `traceId`.
- `Error.Unexpected(detail)` e `Exception.Message` **não** vão para `detail` em produção.
- Se `ExposeExceptionDetails`: extensão `debug` (não `detail`) com a mensagem; nunca o stack no body.

## 7. Estrutura do repositório

```text
Offside.sln
src/Offside/
src/Offside.AspNetCore/
tests/Offside.Tests/
tests/Offside.AspNetCore.Tests/
docs/superpowers/specs/
```

Exemplos de JSON de mensagens em `src/Offside/errors.json` (default EN) e samples/docs; os consumidores trazem os seus `errors.pt-BR.json`.

## 8. Testes

Core:

- Factories: code, kind, arguments.
- `Custom` com objeto anónimo.
- `Combine` junta erros; `Bind` curto-circuita. Não existe `Apply`.
- `Failure()` vazio e `Value` em falha throw.
- Fallback JSON: cultura → pai → default; chave em falta → `Code`.
- Interpolação de `{args}`; placeholder em falta permanece.

ASP.NET:

- Status pela severidade (incluindo empate Unauthorized/Forbidden).
- `application/problem+json` e `errors[]` completo.
- Cultura via `Accept-Language`.
- 500 sanitizado; `debug` só com a opção Development.
- `traceId` presente.

## 9. Decisões explícitas (não deixar o código decidir)

- Result é o caminho normal; exceções só escape.
- Metadados no C#; mensagens no JSON.
- Lista de erros no Result; HTTP escolhe Kind mais grave.
- Um shape de Problem Details (`errors[]`).
- 500 genérico + `traceId`; debug só em Development.
- `ErrorKind` fechado na v1; `Custom` reusa um Kind.
- Resolvedor no Core; host injecta streams.
- Sem conversão implícita `T` → `Result<T>` na v1.

## 10. Não-objectivos v1

- Pacote único.
- Extensão livre de status HTTP.
- Source generator de catálogo.
- Integração FluentValidation / gRPC.
- Implicit success conversion.

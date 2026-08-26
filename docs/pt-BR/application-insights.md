# Integração com Application Insights

*[English](../application-insights.md) · [Voltar às docs](README.md)*

Uma falha de domínio que vira Problem Details não deixa rastro nos seus logs — ela nunca foi uma exceção. `Offside.ApplicationInsights` registra valores `Error` como traces do Application Insights, com severidade derivada do `ErrorKind` e dimensões estáveis para filtrar no Kusto.

Este pacote fala com o SDK clássico `Microsoft.ApplicationInsights` e precisa do `TelemetryClient` do host. Se o seu host estiver instrumentado com `Azure.Monitor.OpenTelemetry.AspNetCore`, não existe `TelemetryClient` para resolver — use o [`Offside.OpenTelemetry`](open-telemetry.md). Os dois são alternativas, não camadas, e concordam na severidade kind a kind.

## Instalar e registrar

```bash
dotnet add package Offside
dotnet add package Offside.ApplicationInsights
```

Configure o Application Insights no host primeiro, depois a integração:

```csharp
using Offside.ApplicationInsights;

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddOffsideApplicationInsights();
```

`AddOffsideApplicationInsights` registra `IDomainErrorRecorder` sobre o `TelemetryClient` do host. Ele nunca lê connection string nem chama `AddApplicationInsightsTelemetry`.

A mensagem do trace é a mensagem resolvida do catálogo, vinda do `IErrorMessageResolver` que o `AddOffside` registrou. Sem ele, o `Code` do erro é escrito no lugar.

## Registrar um result

```csharp
public async Task<IResult> Cancel(string id)
{
    var result = _orders.Cancel(id).RecordTo(_recorder);
    return result.ToHttpResult();
}
```

`RecordTo` escreve um trace por erro, na ordem do result, e devolve o result inalterado para caber numa cadeia. Um result de sucesso não registra nada. Dimensões extras entram assim:

```csharp
result.RecordTo(_recorder, new Dictionary<string, string> { ["tenant"] = tenantId });
```

As dimensões do Offside sempre vencem as fornecidas — uma chave de tenant nunca reescreve `offside.kind`.

## Como fica um trace

| Dimensão | Valor |
|---|---|
| `offside.code` | A chave de catálogo — `order.already_shipped` |
| `offside.errorCode` | O identificador de tela — `ORDER_ALREADY_SHIPPED` |
| `offside.kind` | A espécie de falha — `Conflict` |
| `offside.field` | O campo ofensor, quando o erro tem um |

A severidade vem do kind:

| Kind | Severidade |
|---|---|
| `Unexpected` | `Critical` |
| `ServiceUnavailable`, `Timeout` | `Error` |
| `Unauthorized`, `Forbidden`, `TooManyRequests`, `Conflict`, `PreconditionFailed`, `Gone`, `Unprocessable` | `Warning` |
| `NotFound`, `Validation`, `BadRequest` | `Information` |

A razão do corte: uma falha de validação é o sistema funcionando e não deve acordar ninguém; um 500 ou uma queda de dependência deve. Troque o mapa inteiro com `options.SeverityFor` quando o seu time de operações traçar a linha em outro lugar.

Uma consulta Kusto sobre o resultado:

```kusto
traces
| where customDimensions["offside.kind"] == "Conflict"
| summarize count() by tostring(customDimensions["offside.errorCode"])
```

## O texto do trace

Por padrão o texto do trace é só a mensagem resolvida do catálogo. Código, kind e campo viajam como dimensões, então nada se perde — e no Kusto essas dimensões são consultáveis sem lotar cada linha renderizada.

O trade-off se inverte quando alguém lê as linhas cruas — um console, o log de um container, um `kubectl logs` — onde nada renderiza as dimensões:

```csharp
builder.Services.AddOffsideApplicationInsights(options =>
    options.FormatMessage = DomainErrorMessageFormat.CodePrefixed);
```

```
[order.already_shipped] Pedido já enviado.
```

Três formatos vêm prontos:

| Formato | Linha |
|---|---|
| `MessageOnly` (padrão) | `Pedido já enviado.` |
| `CodePrefixed` | `[order.already_shipped] Pedido já enviado.` |
| `ErrorCodePrefixed` | `[ORDER_ALREADY_SHIPPED] Pedido já enviado.` |

O `ErrorCodePrefixed` se justifica quando o suporte lê o log pelo identificador que o usuário informa a partir da tela.

Qualquer `Func<Error, string, string>` serve — o erro, e a mensagem já resolvida:

```csharp
options.FormatMessage = (error, message) => $"{error.Kind}/{error.Code}: {message}";
```

O formato molda o texto do trace e nada mais: as dimensões não são afetadas, então uma linha mais curta nunca custa um filtro. O `Offside.OpenTelemetry` oferece os mesmos três formatos com os mesmos nomes, e um teste falha se os dois passarem a renderizar diferente.

## Argumentos e PII

`Error.Arguments` **não** são escritos por padrão. Eles carregam o que o domínio colocou neles — identificadores, valores tentados, um motivo vindo de uma dependência — e telemetria sobrevive à requisição por meses. Ligue apenas quando souber que todo argumento é seguro:

```csharp
builder.Services.AddOffsideApplicationInsights(options => options.IncludeArguments = true);
```

Eles aparecem como `offside.arg.{nome}`; argumentos nulos são ignorados.

## Opções

| Opção | Padrão | O que faz |
|---|---|---|
| `PropertyPrefix` | `offside.` | Prefixo de toda dimensão do Offside |
| `IncludeArguments` | `false` | Escreve `Error.Arguments` como dimensões |
| `Culture` | `InvariantCulture` | Cultura em que a mensagem do trace é resolvida — deliberadamente não é a cultura da requisição, para o log ficar num idioma só |
| `SeverityFor` | A tabela acima | Escolhe a severidade de um kind |
| `FormatMessage` | `MessageOnly` | Monta o texto do trace a partir do erro e da mensagem resolvida |

## Com MediatR

Se você já publica falhas como domain notifications, o `Offside.ApplicationInsights.MediatR` registra cada uma — sem mudar nenhum ponto de chamada:

```bash
dotnet add package Offside.ApplicationInsights.MediatR
```

```csharp
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();                    // o coletor com escopo
builder.Services.AddOffsideApplicationInsights();        // o gravador
builder.Services.AddOffsideApplicationInsightsMediatR(); // a ponte
```

`AddOffsideApplicationInsightsMediatR` é idempotente, e a ponte roda ao lado do coletor — um não substitui o outro. Veja o [guia do MediatR](mediatr-guide.md) para a publicação.

## Com Refit

`Offside.Refit` expõe `IExternalApiErrorObserver` para falhas vistas no fio. Um pequeno adaptador as encaminha para cá; veja [Observar falhas no fio](refit.md#observar-falhas-no-fio).

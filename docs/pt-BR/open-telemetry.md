# Integração com OpenTelemetry

*[English](../open-telemetry.md) · [Voltar às docs](README.md)*

Uma falha de domínio que vira Problem Details não deixa rastro nos seus logs — ela nunca foi uma exceção. `Offside.OpenTelemetry` emite valores `Error` pelos sinais de OpenTelemetry que seu host já coleta: uma entrada de log estruturada, um evento no span em curso e um contador.

Use este pacote quando o host estiver instrumentado com `Azure.Monitor.OpenTelemetry.AspNetCore`, com o SDK do OpenTelemetry e um exporter OTLP, ou com qualquer outro coletor. Use o [`Offside.ApplicationInsights`](application-insights.md) quando o host ainda rodar o SDK clássico `Microsoft.ApplicationInsights` — os dois são alternativas, não camadas.

## Instalar e registrar

```bash
dotnet add package Offside
dotnet add package Offside.OpenTelemetry
```

Configure o pipeline no host primeiro, depois a integração:

```csharp
using Offside.OpenTelemetry;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter(OffsideTelemetry.MeterName))
    .UseAzureMonitor();

builder.Services.AddOffsideOpenTelemetry();
```

`AddOffsideOpenTelemetry` registra `IDomainErrorRecorder` sobre o `ILoggerFactory` do host. Ele não configura OpenTelemetry nem exporter, e nunca lê connection string.

**`AddMeter(OffsideTelemetry.MeterName)` não é opcional se você quer o contador.** Um meter que nenhum pipeline escuta é descartado em silêncio — é a causa mais comum de "registrei e não vejo nada".

Não há activity source a registrar: o pacote nunca abre span próprio. Ele anexa um evento à activity que a instrumentação do host já tem em curso, então a instrumentação do ASP.NET Core basta.

A mensagem do log é a mensagem resolvida do catálogo, vinda do `IErrorMessageResolver` que o `AddOffside` registrou. Sem um resolver, o `Code` do erro é escrito no lugar.

## Registrar um resultado

```csharp
public async Task<IResult> Cancel(string id)
{
    var result = _orders.Cancel(id).RecordTo(_recorder);
    return result.ToHttpResult();
}
```

`RecordTo` registra um erro por vez, na ordem do resultado, e devolve o resultado intacto para poder ficar no meio de uma cadeia. Resultado de sucesso não registra nada. Dimensões extras são mescladas:

```csharp
result.RecordTo(_recorder, new Dictionary<string, string> { ["tenant"] = tenantId });
```

As dimensões do Offside sempre vencem as fornecidas, então uma chave de tenant nunca reescreve `offside.kind`.

## Os três sinais

| Sinal | Onde cai | Carrega |
|---|---|---|
| Entrada de log, categoria `Offside` | `traces` no Application Insights, ou seu backend de log | Todas as dimensões abaixo |
| Evento `offside.error` na activity em curso | O span da requisição que falhou | Todas as dimensões abaixo |
| Contador `offside.errors` | `customMetrics`, ou seu backend de métricas | Só `offside.kind` e `offside.code` |

Dimensões:

| Dimensão | Valor |
|---|---|
| `offside.code` | A chave do catálogo — `order.already_shipped` |
| `offside.errorCode` | O identificador de tela — `ORDER_ALREADY_SHIPPED` |
| `offside.kind` | A espécie da falha — `Conflict` |
| `offside.field` | O campo ofensor, quando o erro tem um |

**O contador carrega menos de propósito.** Campo, argumentos e dimensões do chamador são ilimitados; cada combinação distinta é uma série temporal a mais para armazenar e consultar. Log e evento de span são por ocorrência e podem bancar o detalhe — um contador não.

## A mensagem do log

Por padrão a linha do log é só a mensagem resolvida do catálogo. Código, kind e campo viajam como dimensões, então nada se perde — e em qualquer backend de OpenTelemetry essas dimensões são consultáveis sem lotar cada linha renderizada.

O trade-off se inverte quando alguém lê as linhas cruas — um console, o log de um container, um `kubectl logs` — onde nada renderiza as dimensões:

```csharp
builder.Services.AddOffsideOpenTelemetry(options =>
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

O formato molda a linha do log e nada mais: as dimensões, o evento do span e o contador não são afetados, então uma linha mais curta nunca custa um filtro.

## Severidade

A severidade vem do kind, e mapeia para `LogLevel`:

| Kind | Severidade | `LogLevel` |
|---|---|---|
| `Unexpected` | `Critical` | `Critical` |
| `ServiceUnavailable`, `Timeout` | `Error` | `Error` |
| `Unauthorized`, `Forbidden`, `TooManyRequests`, `Conflict`, `PreconditionFailed`, `Gone`, `Unprocessable` | `Warning` | `Warning` |
| `NotFound`, `Validation`, `BadRequest` | `Information` | `Information` |

A razão da divisão: uma falha de validação é o sistema funcionando, e não deve acordar ninguém; um 500 ou uma queda de dependência deve. Substitua o mapa inteiro com `options.SeverityFor` quando seu time de operação traçar a linha em outro lugar.

Esta tabela é idêntica à do `Offside.ApplicationInsights`, e um teste do repositório falha se as duas divergirem. Migrar um host do SDK clássico para OpenTelemetry não muda o que dispara os alertas dele.

Uma consulta Kusto sobre o resultado:

```kusto
traces
| where customDimensions["offside.kind"] == "Conflict"
| summarize count() by tostring(customDimensions["offside.errorCode"])
```

## Status do span

Registrar um erro não mexe no status do span por padrão. Uma falha de domínio é, muitas vezes, uma requisição perfeitamente bem-sucedida — um 404 respondido corretamente não é uma operação quebrada, e marcá-la como falha distorce sua taxa de erro.

Onde um erro registrado realmente significa que o span falhou:

```csharp
builder.Services.AddOffsideOpenTelemetry(options =>
{
    options.SetActivityStatusOnError = true;
    options.MinimumSeverityForActivityFailure = DomainErrorSeverity.Error; // o padrão
});
```

Só erros a partir dessa severidade marcam a activity. Com o limiar padrão, um `NotFound` ainda deixa o span bem-sucedido e um `Unexpected` não.

## Argumentos e PII

`Error.Arguments` **não** são escritos por padrão. Eles carregam o que o domínio colocou neles — identificadores, valores tentados, uma razão vinda de dependência — e telemetria sobrevive à requisição por meses. Ligue só quando souber que todo argumento é seguro:

```csharp
builder.Services.AddOffsideOpenTelemetry(options => options.IncludeArguments = true);
```

Eles então aparecem como `offside.arg.{nome}` na entrada de log e no evento do span; argumentos nulos são pulados. Nunca chegam ao contador, esteja isso ligado ou não.

## Opções

| Opção | Padrão | O que faz |
|---|---|---|
| `PropertyPrefix` | `offside.` | Prefixo de toda dimensão do Offside |
| `IncludeArguments` | `false` | Escreve `Error.Arguments` como dimensões |
| `Culture` | `InvariantCulture` | Cultura em que a mensagem é resolvida — deliberadamente não a da requisição, para o log ficar num idioma só |
| `SeverityFor` | A tabela acima | Escolhe a severidade de um kind |
| `FormatMessage` | `MessageOnly` | Monta a linha do log a partir do erro e da mensagem resolvida |
| `EmitLog` | `true` | Escreve a entrada de log |
| `EmitActivityEvent` | `true` | Adiciona o evento à activity em curso |
| `EmitMetric` | `true` | Incrementa o contador |
| `SetActivityStatusOnError` | `false` | Marca a activity como falha para erros severos |
| `MinimumSeverityForActivityFailure` | `Error` | A severidade que conta como severa, quando a opção acima está ligada |

Cada um dos três interruptores `Emit*` é independente — desligar um não toca nos outros dois.

## Com MediatR

Se você já publica falhas como domain notifications, o `Offside.OpenTelemetry.MediatR` registra cada uma — sem mudar nenhum call site:

```bash
dotnet add package Offside.OpenTelemetry.MediatR
```

```csharp
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();              // o coletor com escopo
builder.Services.AddOffsideOpenTelemetry();        // o gravador
builder.Services.AddOffsideOpenTelemetryMediatR(); // a ponte
```

`AddOffsideOpenTelemetryMediatR` é idempotente, e a ponte roda junto com o coletor — nenhum substitui o outro. Veja o [guia do MediatR](mediatr-guide.md) para publicar.

## Com Refit

O `Offside.Refit` expõe `IExternalApiErrorObserver` para falhas vistas no fio. Um pequeno adaptador as encaminha para cá; veja [Observar falhas no fio](refit.md#observar-falhas-no-fio).

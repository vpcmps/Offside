# Consultar erros de domínio

*[English](../queries.md) · [Voltar à documentação](README.md)*

Falhas de domínio gravadas pelo Offside nunca viram exceção, então nunca produzem um message template em que se possa filtrar. Não há `{OriginalFormat}`. Filtre pelas dimensões.

As mesmas chaves chegam nos traces do Application Insights (SDK clássico) e nos logs OpenTelemetry exportados para o Azure Monitor. Nos dois casos elas ficam em `customDimensions`.

| Dimensão | O que é | Exemplo |
|---|---|---|
| `offside.code` | Chave do catálogo de mensagens | `order.already_shipped` |
| `offside.errorCode` | Identificador de tela | `ORDER_ALREADY_SHIPPED` |
| `offside.kind` | Espécie da falha | `Conflict` |
| `offside.field` | Campo ofensor, quando existe | `email` |

Hosts HTTP também recebem `HttpStatus` do pipeline de problem. Chaves do chamador, como `Operation`, vêm de `TelemetryProperties` ou de `RecordTo`.

## Encontrar um código

```kusto
traces
| where customDimensions["offside.code"] == "order.already_shipped"
| project timestamp, severityLevel, message, operation_Id, customDimensions
```

## Agrupar por kind

```kusto
traces
| where isnotempty(customDimensions["offside.kind"])
| summarize count() by tostring(customDimensions["offside.kind"])
```

Com o default `RecordMode.PerError` do pipeline HTTP, `count()` é erros, não requisições. Uma validação em cinco campos incrementa cinco vezes. `PrimaryErrorOnly` alinha a contagem a uma falha HTTP.

## Alertar em falhas inesperadas

Dispare em `Unexpected`, não em todo erro gravado. Um 404 respondido corretamente é Information no mapa da biblioteca.

```kusto
traces
| where customDimensions["offside.kind"] == "Unexpected"
| summarize Failures=count() by bin(timestamp, 5m)
| where Failures > 0
```

Um time de operações que também pagina recusas de negócio deve definir `SeverityFor = DomainErrorSeverityMap.Operations` para que 404/400 virem Warning, e então alertar em `severityLevel >= 2`. Não reconstrua essa divisão parseando `message`.

## Argumentos

Argumentos vêm desligados por padrão. Uma allowlist grava só as chaves nomeadas como `offside.arg.{nome}`:

```kusto
traces
| where customDimensions["offside.arg.rejectionReason"] == "missing-header"
```

Eles nunca aparecem no contador `offside.errors`.

## O contador (OpenTelemetry)

```kusto
customMetrics
| where name == "offside.errors"
| summarize sum(value) by tostring(customDimensions["offside.kind"]), tostring(customDimensions["offside.code"])
```

Se esta consulta vier vazia e os traces existirem, o meter não está no pipeline — chame `AddMeter(OffsideTelemetry.MeterName)`. O Offside registra isso uma vez na primeira emissão.

# Integração com Refit

*[English](../refit.md) · [Voltar às docs](README.md)*

`Offside.Refit` cobre a borda de saída: quando uma dependência responde com falha, o Refit lança uma `ApiException`, e este pacote a transforma nos mesmos valores `Error` que o seu domínio produz. O pacote Core continua sem Refit.

## Instalar e registrar

```bash
dotnet add package Offside
dotnet add package Offside.Refit
```

Configure seus clientes Refit normalmente e registre a integração:

```csharp
using Offside.Refit;

builder.Services.AddRefitClient<IPaymentsApi>()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://payments.example"));

builder.Services.AddOffsideRefit(options => options.ApiName = "payments");
```

`AddOffsideRefit` registra as opções de mapeamento e o `IExternalApiCaller`. Ele não cria clientes Refit, não configura `HttpClient` e não adiciona políticas de resiliência — isso continua no host.

## Chamar uma dependência

Injete `IExternalApiCaller` e deixe o `try`/`catch` com ele:

```csharp
public async Task<Result<Order>> Get(string id, CancellationToken cancellationToken)
{
    return await _api.CallAsync(
        token => _payments.GetOrderAsync(id, token),
        cancellationToken: cancellationToken);
}
```

Quatro falhas são convertidas; qualquer outra propaga intacta, inclusive um bug no seu próprio callback:

| Falha | Resultado |
|---|---|
| `ApiException` (a dependência respondeu com erro) | O status, ou o corpo do problem details, decide — veja abaixo |
| `TimeoutException`, ou um cancelamento que você não pediu | `ErrorKind.Timeout` |
| `HttpRequestException` (a dependência nem foi alcançada) | `ErrorKind.ServiceUnavailable` |
| Um cancelamento que você *pediu* | Relançado, para `OperationCanceledException` continuar significando o que significa |

## O mapeamento de status

O mapeamento espelha o que a dependência disse — o inverso do mapeamento kind → status que o Offside aplica na saída.

| Status | Kind | | Status | Kind |
|---|---|---|---|---|
| 400 | `BadRequest` | | 429 | `TooManyRequests` |
| 401 | `Unauthorized` | | 500 e demais 5xx | `Unexpected` |
| 403 | `Forbidden` | | 502, 503 | `ServiceUnavailable` |
| 404 | `NotFound` | | 504 | `Timeout` |
| 409 | `Conflict` | | outros 4xx | `BadRequest` |
| 410 | `Gone` | | | |
| 412 | `PreconditionFailed` | | | |
| 422 | `Unprocessable` | | | |

`OffsideRefit.Kind(statusCode)` expõe a tabela diretamente.

**Um 404 espelhado não é automaticamente o seu 404.** O padrão, `InboundStatusMapping.CollapseClientErrors`, dobra todo kind 4xx — inclusive um problem body Offside restaurado — em `ServiceUnavailable` com código de catálogo `external_api.service_unavailable`. O kind, o code e o errorCode originais ficam nos arguments. `Timeout`, `ServiceUnavailable` e `Unexpected` permanecem.

Dois serviços Offside no mesmo produto, ou um BFF que deve expor o status da dependência, optam pelo comportamento 0.4.0:

```csharp
builder.Services.AddOffsideRefit(options =>
    options.InboundStatus = InboundStatusMapping.Mirror);
```

## Códigos de catálogo

Cada erro mapeado recebe o prefixo `external_api.` no código de catálogo — `external_api.not_found` antes do collapse, `external_api.service_unavailable` depois, `external_api.timeout` para um 504 — para que uma falha de dependência nunca se confunda com uma regra sua. Adicione as entradas que usar:

```json
{
  "external_api.not_found": "O serviço {api} não encontrou o que pedimos.",
  "external_api.service_unavailable": "O serviço {api} está indisponível.",
  "external_api.timeout": "O serviço {api} demorou demais para responder."
}
```

Tokens disponíveis: `{api}`, `{status}`, `{requestUri}`, `{reason}`. Deixe `CodePrefix` vazio para cair nos códigos do Core (`not_found`, `timeout`, …), que já vêm no catálogo padrão.

## Lendo o problem details da dependência

Com `ReadProblemDetails` ligado — o padrão — um corpo `application/problem+json` é lido antes de o status ser considerado:

- **A dependência é um serviço Offside.** O array `errors` é restaurado erro a erro, preservando `code`, `errorCode`, `kind` e `field`. `InboundStatus` roda depois: com o padrão, esses kinds 4xx ainda viram `ServiceUnavailable`. Com `Mirror`, dois serviços que falam Offside não perdem nada na travessia.
- **Um corpo de validação do ASP.NET** (`"errors": { "email": ["…"] }`) vira um erro `ErrorKind.Validation` por campo.
- **Um problem document simples** contribui com `detail` e `errorCode`.

O parsing nunca lança. Um corpo malformado, truncado ou inesperado degrada para o mapeamento por status — uma dependência mal comportada não quebra o seu caminho de erro.

## Mapear sem o caller

As extensões que o caller usa são públicas, para código que já tem a exceção em mãos:

```csharp
catch (ApiException exception)
{
    return exception.ToResult<Order>();   // também ToError(), ToOffsideErrors(), ToResult()
}
```

## Observar falhas no fio

`OffsideRefitDiagnosticsHandler` reporta toda resposta de erro e toda falha de transporte a um `IExternalApiErrorObserver`, e deixa o desfecho seguir inalterado. Ele observa; nunca converte uma resposta em `Result`.

```csharp
builder.Services.AddOffsideRefitDiagnostics();
builder.Services.AddRefitClient<IPaymentsApi>()
    .AddHttpMessageHandler<OffsideRefitDiagnosticsHandler>();
```

Sem um registro seu, o observer é no-op. Este é o seam para telemetria: com o [`Offside.ApplicationInsights`](application-insights.md), um adaptador de cinco linhas liga os dois — e nenhum dos pacotes depende do outro.

```csharp
internal sealed class TelemetryObserver(IDomainErrorRecorder recorder) : IExternalApiErrorObserver
{
    public void Observe(Error error) => recorder.Record(error);
}

builder.Services.AddSingleton<IExternalApiErrorObserver, TelemetryObserver>();
```

O handler lê apenas o status — ele não toca no corpo da resposta, então o que ele reporta não tem problem details. O mapeamento completo acontece no `IExternalApiCaller`.

## Opções

| Opção | Padrão | O que faz |
|---|---|---|
| `ApiName` | `external api` | Exposto aos templates como `{api}` |
| `CodePrefix` | `external_api` | Prefixo dos códigos de catálogo; vazio cai nos códigos do Core |
| `ReadProblemDetails` | `true` | Lê um corpo `application/problem+json` antes de cair no status |
| `InboundStatus` | `CollapseClientErrors` | Dobra kinds 4xx em `ServiceUnavailable` depois do mapeamento; `Mirror` preserva o kind da dependência |

Opções passadas a um `CallAsync` específico vencem as registradas.

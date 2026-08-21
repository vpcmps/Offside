# Integração com MediatR

*[English](../mediatr-guide.md) · [Voltar às docs](README.md)*

`Offside.MediatR` conecta valores `Result` com falha a notificações MediatR sem adicionar MediatR ao pacote Core do Offside. Uma notificação sempre carrega um `Error`; ela não é um domain event que descreve mudança de estado.

## Instalar e registrar

```bash
dotnet add package Offside
dotnet add package Offside.MediatR
```

Configure o MediatR primeiro e depois a integração:

```csharp
using Offside.MediatR;

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();
```

`AddOffsideMediatR` é idempotente. Ele registra um coletor scoped e seu notification handler, mas não chama `AddMediatR`, não registra `IPublisher` e não configura licença do MediatR.

Não é necessário escanear o assembly `Offside.MediatR`. Se ele for escaneado, o coletor protege uma mesma instância de notificação contra registro duplicado do handler.

## Publicar um Result

Injete `IPublisher` e publique na borda da aplicação:

```csharp
public async Task<Result> Cancel(string id, CancellationToken cancellationToken)
{
    var result = _orders.Cancel(id);
    return await result.PublishDomainNotificationsAsync(_publisher, cancellationToken);
}
```

O overload genérico devolve o mesmo `Result<T>`:

```csharp
Result<Order> result = _orders.Get(id);
return await result.PublishDomainNotificationsAsync(_publisher, cancellationToken);
```

Sucesso não publica nada. Falha publica uma `DomainNotification` por erro, sequencialmente e na ordem do Result. Com `E` erros e `H` handlers, o limite de trabalho é `E × H` execuções de handler.

## Ler o coletor

Injete `IDomainNotificationCollector` no mesmo scope de injeção de dependência:

```csharp
if (collector.HasNotifications)
    return collector.ToResult();

return collector.ToResult(order);
```

`Errors` devolve um snapshot independente. `ToResult()` devolve sucesso quando vazio e falha com os erros coletados nos demais casos. `ToResult<T>(value)` devolve o valor informado apenas quando o coletor está vazio.

Leituras nunca removem notificações; não existe `Clear`. Crie um scope por operação lógica:

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var worker = scope.ServiceProvider.GetRequiredService<OrderWorker>();
await worker.Process(message, stoppingToken);
```

O ASP.NET Core já cria um scope por request. Workers devem criar um novo scope por mensagem ou job; reutilizar um scope longo também mantém erros antigos.

## Ordem, concorrência e falhas

- As extensões de Result garantem ordem porque aguardam cada publicação antes de iniciar a próxima.
- Publicações externas concorrentes são thread-safe, mas sua ordem relativa não é especificada.
- Exceções de handlers e cancelamento interrompem a publicação e são propagados.
- Handlers de erros anteriores — e handlers anteriores na estratégia atual do MediatR — podem já ter executado. Não existe rollback.
- Repetir a extensão cria novas notificações e pode repetir efeitos. Torne os handlers idempotentes quando houver retry.

Todo handler recebe o `Error` completo, incluindo `Arguments` e dados de diagnóstico de `Unexpected`. A sanitização HTTP não é executada nesse caminho. Nunca coloque segredos em um erro.

## Versões e licenciamento do MediatR

O pacote suporta MediatR `12.0.1` até `14.x` e é testado com `12.0.1`, `13.1.0` e `14.2.0`. O intervalo da dependência NuGet é `[12.0.1,15.0.0)`.

O MediatR 13 introduziu uma chave de licença e mudou seu modelo de licenciamento upstream. Hosts com 13 ou 14 devem configurar logging e avaliar a licença aplicável. Configure a chave no próprio MediatR; o Offside não aceita uma chave nem suprime warnings de licença. Veja a [release do MediatR 13](https://github.com/LuckyPennySoftware/MediatR/releases) e a [página oficial de licenciamento](https://mediatr.io/).

## Omissões deliberadas

- Sem pipeline behavior automático: a publicação fica visível no call site.
- Sem `Clear`: o scope controla o ciclo de vida do coletor.
- Sem abstração de retry ou transação: essas políticas pertencem à aplicação.

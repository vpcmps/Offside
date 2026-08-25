# Primeiros passos

*[English](../getting-started.md) · [Voltar às docs](README.md)*

Esta página leva um projeto do zero até uma resposta Problem Details em quatro passos.

## 1. Instalar

```bash
dotnet add package Offside
```

Adicione a integração com ASP.NET Core apenas no host web:

```bash
dotnet add package Offside.AspNetCore
```

`Offside` tem como alvo `netstandard2.0`, `net8.0` e `net10.0`. `Offside.AspNetCore` tem como alvo `net8.0` e `net10.0`.

Hosts FluentValidation ou FastEndpoints podem adicionar:

```bash
dotnet add package Offside.FluentValidation
dotnet add package Offside.FastEndpoint
```

Veja [FluentValidation](fluentvalidation.md) e [FastEndpoints](fastendpoints.md).

Opcionalmente, instale o CLI para gerar catálogos e skills de agente — veja a [página do CLI](cli.md):

```bash
dotnet tool install -g Offside.Tool
offside init
```

## 2. Adicionar um catálogo

O Offside nunca fixa o texto da mensagem no código. Crie `errors/errors.json` com um template por código de erro:

```json
{
  "not_found": "{resource} '{id}' não foi encontrado.",
  "gone": "{resource} '{id}' não existe mais.",
  "conflict": "Conflito em {resource}.",
  "validation": "{field} é inválido.",
  "bad_request": "Requisição inválida.",
  "unauthorized": "Não autenticado.",
  "forbidden": "Acesso negado.",
  "precondition_failed": "Pré-condição não atendida.",
  "unprocessable": "Não foi possível processar a requisição.",
  "too_many_requests": "Requisições em excesso.",
  "unexpected": "Ocorreu um erro inesperado.",
  "service_unavailable": "O serviço está temporariamente indisponível.",
  "timeout": "A requisição excedeu o tempo limite."
}
```

Garanta que o arquivo chegue ao diretório de saída:

```xml
<ItemGroup>
  <None Update="errors\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## 3. Registrar os serviços

```csharp
using System.Globalization;
using Offside;
using Offside.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOffside(options =>
{
    options.AddJsonFile(CultureInfo.InvariantCulture, "errors/errors.json");

    var ptBr = Path.Combine(AppContext.BaseDirectory, "errors/errors.pt-BR.json");
    if (File.Exists(ptBr))
        options.AddJsonFile(new CultureInfo("pt-BR"), ptBr);
});

builder.Services.AddOffsideAspNetCore();
```

Dois pontos que costumam pegar as pessoas:

- **`AddJsonFile` lê o arquivo.** Caminhos relativos resolvem contra `AppContext.BaseDirectory`. Um arquivo ausente falha na inicialização e nomeia o path. `AddJson` ainda recebe o *conteúdo* quando você já tem a string; `AddJsonFromAssembly` carrega um resource embutido.
- **O catálogo de cultura invariante é obrigatório.** Sem ele, `AddOffside` lança `InvalidOperationException` na inicialização — de propósito, para que um catálogo faltando seja uma falha de boot e não uma surpresa às 3 da manhã.

`AddOffsideAspNetCore` registra `OffsideAspNetCoreOptions`. Quando há um `IHostEnvironment` presente, `ExposeExceptionDetails` assume `IsDevelopment()`.

## 4. Devolver um resultado

O domínio devolve um `Result<T>` e não sabe nada sobre HTTP:

```csharp
using Offside;

public sealed class OrderService(IOrderRepository orders)
{
    public Result<Order> Get(string id)
    {
        var order = orders.Find(id);
        return order is null
            ? Result<Order>.Failure(Error.NotFound("order", id))
            : Result<Order>.Success(order);
    }
}
```

O endpoint converte em uma chamada:

```csharp
app.MapGet("/orders/{id}", (string id, OrderService orders, HttpContext http) =>
    orders.Get(id).ToHttpResult(http));
```

Encontrando, devolve `200 OK` com o pedido. Não encontrando:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "NotFound",
  "status": 404,
  "detail": "order '42' não foi encontrado.",
  "errorCode": "NOT_FOUND",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "code": "not_found", "errorCode": "NOT_FOUND", "kind": "NotFound", "detail": "order '42' não foi encontrado.", "field": null }
  ]
}
```

## Camadas

Projetos de domínio e aplicação referenciam **apenas `Offside`**. `Offside.AspNetCore` pertence ao host web, que é o único lugar que conhece status codes e Problem Details. É essa fronteira que permite ao mesmo código de domínio servir uma API HTTP, um worker e um CLI sem alteração.

```
Domínio / Aplicação  ──►  Offside
Host web             ──►  Offside + Offside.AspNetCore
```

## Próximos passos

- [Conceitos](concepts.md) — o vocabulário, em uma página curta
- [Guia de domínio](domain-guide.md) — todas as factories e combinadores
- [Guia ASP.NET Core](aspnet-guide.md) — escolha do status, 500, culturas
- [FluentValidation](fluentvalidation.md) — mapear falhas de validator para `Error`
- [FastEndpoints](fastendpoints.md) — `UseOffside` e `SendOffsideAsync`

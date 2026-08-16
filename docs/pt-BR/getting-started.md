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
  "unexpected": "Ocorreu um erro inesperado."
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
    options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors/errors.json"));

    var ptBr = Path.Combine(builder.Environment.ContentRootPath, "errors/errors.pt-BR.json");
    if (File.Exists(ptBr))
        options.AddJson(new CultureInfo("pt-BR"), File.ReadAllText(ptBr));
});

builder.Services.AddOffsideAspNetCore();
```

Dois pontos que costumam pegar as pessoas:

- **`AddJson` recebe o *conteúdo* do catálogo, não um caminho.** Leia o arquivo você mesmo, ou passe um `Stream` para um recurso embutido.
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
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "code": "not_found", "kind": "NotFound", "detail": "order '42' não foi encontrado.", "field": null }
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

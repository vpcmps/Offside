# Documentação do Offside

*[English](../README.md)*

Erros de domínio como `Result`, não como exceções. O domínio devolve um `Error`; o ASP.NET Core mapeia isso para Problem Details (RFC 7807). As mensagens ficam em catálogos JSON, não em C#.

## Comece por aqui

| Página | O que cobre |
|---|---|
| [Primeiros passos](getting-started.md) | Instalar, registrar e devolver a primeira resposta Problem Details |
| [Conceitos](concepts.md) | `Error`, `ErrorCode`, `ErrorKind`, `Result`, erro primário, catálogos, escape hatch |
| [Guia de domínio](domain-guide.md) | Escrever código de domínio com `Result<T>`: factories, `Custom`, `Bind`/`Map`/`Combine` |
| [Guia ASP.NET Core](aspnet-guide.md) | `ToHttpResult` / `ToActionResult`, escolha do status, formato da resposta, tratamento de 500 |
| [FluentValidation](fluentvalidation.md) | Mapear falhas do FluentValidation para `Error` / `Result` do Offside |
| [FastEndpoints](fastendpoints.md) | `UseOffside`, `SendOffsideAsync`, erros esperados no OpenAPI |
| [Mensagens e culturas](messages.md) | Formato do catálogo, fallback de cultura, interpolação de `{token}` |
| [CLI](cli.md) | `offside init` — skills de agente e templates de catálogo |
| [Referência de API](api-reference.md) | Todos os tipos e membros públicos, em uma página |
| [FAQ](faq.md) | Decisões de design e armadilhas comuns |

## O formato disso

```csharp
// Domínio — não sabe nada sobre HTTP
public Result<Order> Get(string id)
{
    var order = _orders.Find(id);
    return order is null
        ? Result<Order>.Failure(Error.NotFound("order", id))
        : Result<Order>.Success(order);
}
```

```csharp
// Endpoint — uma linha
app.MapGet("/orders/{id}", (string id, HttpContext http) => _orders.Get(id).ToHttpResult(http));
```

```json
// Resposta — 404, application/problem+json
{
  "type": "https://httpstatuses.io/404",
  "title": "NotFound",
  "status": 404,
  "detail": "order '42' was not found.",
  "errorCode": "NOT_FOUND",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "code": "not_found", "errorCode": "NOT_FOUND", "kind": "NotFound", "detail": "order '42' was not found.", "field": null }
  ]
}
```

## Pacotes

| Pacote | Target frameworks | Papel |
|---|---|---|
| `Offside` | `netstandard2.0`, `net8.0`, `net10.0` | `Error`, `ErrorKind`, `Result` / `Result<T>`, resolver JSON, `AddOffside` |
| `Offside.AspNetCore` | `net8.0`, `net10.0` | `ToHttpResult` / `ToActionResult`, Problem Details, `AddOffsideAspNetCore` |
| `Offside.FluentValidation` | `netstandard2.0`, `net8.0`, `net10.0` | Falhas FluentValidation → `Error` / `Result` |
| `Offside.FastEndpoint` | `net8.0`, `net10.0` | `UseOffside`, `SendOffsideAsync`, erros esperados no OpenAPI |
| `Offside.AzureAppConfiguration` | `netstandard2.0`, `net8.0`, `net10.0` | Resolver dinâmico para catálogos carregados pelo Azure App Configuration |
| `Offside.Tool` | `net8.0` | `offside init` — skills de agente e templates de catálogo |

O pacote core não tem dependência de ASP.NET, então projetos de domínio podem referenciá-lo livremente.

## Em outro lugar

- [Changelog](../../CHANGELOG.md) · [Contribuindo](../../CONTRIBUTING.md) · [Suporte](../../SUPPORT.md) · [Segurança](../../SECURITY.md)
- [Especificação de design](../superpowers/specs/2026-08-12-domain-errors-design.md) (português, interna)

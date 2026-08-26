![offside](../../assets/offside-lockup.png)

# Documentação do Offside

*catch it before the whistle · [English](../README.md)*

[![NuGet](https://img.shields.io/nuget/v/Offside?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Offside)
[![Downloads](https://img.shields.io/nuget/dt/Offside?label=downloads)](https://www.nuget.org/packages/Offside)
[![CI](https://img.shields.io/github/actions/workflow/status/vpcmps/Offside/ci.yml?branch=master&label=CI)](https://github.com/vpcmps/Offside/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/vpcmps/Offside/blob/master/LICENSE)
![Frameworks](https://img.shields.io/badge/net-standard2.0%20%7C%208.0%20%7C%2010.0-512BD4)

Erros de domínio como `Result`, não como exceções. O domínio devolve um `Error`; o ASP.NET Core mapeia isso para Problem Details (RFC 7807). As mensagens ficam em catálogos JSON, não em C#.

> Prefere um guia navegável com diagramas de arquitetura? Visite a **[Wiki em português](https://github.com/vpcmps/Offside/wiki)** ou a **[Wiki em inglês](https://github.com/vpcmps/Offside/wiki/Home-English)**.

## Comece por aqui

| Página | O que cobre |
|---|---|
| [Primeiros passos](getting-started.md) | Instalar, registrar e devolver a primeira resposta Problem Details |
| [Conceitos](concepts.md) | `Error`, `ErrorCode`, `ErrorKind`, `Result`, erro primário, catálogos, escape hatch |
| [Guia de domínio](domain-guide.md) | Escrever código de domínio com `Result<T>`: factories, `Custom`, `Bind`/`Map`/`Combine` |
| [Guia ASP.NET Core](aspnet-guide.md) | `ToHttpResult` / `ToActionResult`, escolha do status, formato da resposta, tratamento de 500 |
| [FluentValidation](fluentvalidation.md) | Mapear falhas do FluentValidation para `Error` / `Result` do Offside |
| [FastEndpoints](fastendpoints.md) | `UseOffside`, `SendOffsideAsync`, erros esperados no OpenAPI |
| [Integração com MediatR](mediatr-guide.md) | Publicar erros de resultados como notificações, coletá-los por scope e tratar retries com segurança |
| [Integração com Refit](refit.md) | Transformar a falha de uma API externa em `Error`, sem `try`/`catch` em cada chamada |
| [Application Insights](application-insights.md) | Registrar erros de domínio como traces, com severidade e dimensões estáveis |
| [Mensagens e culturas](messages.md) | Formato do catálogo, fallback de cultura, interpolação de `{token}` |
| [Guia de testes](testing.md) | Asserções sobre `Result`, `Error` e catálogos de mensagens em testes |
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

| Pacote | Versão | Target frameworks | Papel |
|---|---|---|---|
| `Offside` | [![NuGet](https://img.shields.io/nuget/v/Offside?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside) | `netstandard2.0`, `net8.0`, `net10.0` | `Error`, `ErrorKind`, `Result` / `Result<T>`, resolver JSON, `AddOffside` |
| `Offside.AspNetCore` | [![NuGet](https://img.shields.io/nuget/v/Offside.AspNetCore?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.AspNetCore) | `net8.0`, `net10.0` | `ToHttpResult` / `ToActionResult`, Problem Details, `AddOffsideAspNetCore` |
| `Offside.FluentValidation` | [![NuGet](https://img.shields.io/nuget/v/Offside.FluentValidation?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.FluentValidation) | `netstandard2.0`, `net8.0`, `net10.0` | Falhas FluentValidation → `Error` / `Result` |
| `Offside.FastEndpoint` | [![NuGet](https://img.shields.io/nuget/v/Offside.FastEndpoint?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.FastEndpoint) | `net8.0`, `net10.0` | `UseOffside`, `SendOffsideAsync`, erros esperados no OpenAPI |
| `Offside.AzureAppConfiguration` | [![NuGet](https://img.shields.io/nuget/v/Offside.AzureAppConfiguration?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.AzureAppConfiguration) | `netstandard2.0`, `net8.0`, `net10.0` | Resolver dinâmico para catálogos carregados pelo Azure App Configuration |
| `Offside.MediatR` | [![NuGet](https://img.shields.io/nuget/v/Offside.MediatR?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.MediatR) | `netstandard2.0`, `net8.0`, `net10.0` | Notificações MediatR para resultados com falha e coletor scoped |
| `Offside.Testing` | Publicação pendente | `netstandard2.0`, `net8.0`, `net10.0` | Asserções sobre `Result`, `Error` e catálogos, sem dependência de framework de teste |
| `Offside.Refit` | Publicação pendente | `netstandard2.0`, `net8.0`, `net10.0` | Falhas do Refit vindas de API externa mapeadas para `Error` / `Result` |
| `Offside.ApplicationInsights` | Publicação pendente | `netstandard2.0`, `net8.0`, `net10.0` | Erros de domínio registrados como traces do Application Insights |
| `Offside.ApplicationInsights.MediatR` | Publicação pendente | `netstandard2.0`, `net8.0`, `net10.0` | Ponte que registra domain notifications publicadas como telemetria |
| `Offside.Tool` | [![NuGet](https://img.shields.io/nuget/v/Offside.Tool?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.Tool) | `net8.0` | `offside init` — skills de agente e templates de catálogo |

O pacote core não tem dependência de ASP.NET nem de MediatR, então projetos de domínio podem referenciá-lo livremente.

## Em outro lugar

- [Changelog](../../CHANGELOG.md) · [Contribuindo](../../CONTRIBUTING.md) · [Suporte](../../SUPPORT.md) · [Segurança](../../SECURITY.md)
- [Especificação de design](../superpowers/specs/2026-08-12-domain-errors-design.md) (português, interna)

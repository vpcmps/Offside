![offside](../assets/offside-lockup.png)

# Offside documentation

*catch it before the whistle · [Português](pt-BR/README.md)*

[![NuGet](https://img.shields.io/nuget/v/Offside?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Offside)
[![Downloads](https://img.shields.io/nuget/dt/Offside?label=downloads)](https://www.nuget.org/packages/Offside)
[![CI](https://img.shields.io/github/actions/workflow/status/vpcmps/Offside/ci.yml?branch=master&label=CI)](https://github.com/vpcmps/Offside/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/vpcmps/Offside/blob/master/LICENSE)
![Frameworks](https://img.shields.io/badge/net-standard2.0%20%7C%208.0%20%7C%2010.0-512BD4)

Domain errors as `Result`, not exceptions. The domain returns an `Error`; ASP.NET Core maps it to Problem Details (RFC 7807). Messages live in JSON catalogs, not in C#.

## Start here

| Page | What it covers |
|---|---|
| [Getting started](getting-started.md) | Install, register, and return your first Problem Details response |
| [Concepts](concepts.md) | `Error`, `ErrorCode`, `ErrorKind`, `Result`, primary error, catalogs, the escape hatch |
| [Domain guide](domain-guide.md) | Writing domain code with `Result<T>`: factories, `Custom`, `Bind`/`Map`/`Combine` |
| [ASP.NET Core guide](aspnet-guide.md) | `ToHttpResult` / `ToActionResult`, status selection, the response shape, 500 handling |
| [FluentValidation](fluentvalidation.md) | Map FluentValidation failures to Offside `Error` / `Result` |
| [FastEndpoints](fastendpoints.md) | `UseOffside`, `SendOffsideAsync`, OpenAPI expected errors |
| [Messages and cultures](messages.md) | Catalog format, culture fallback, `{token}` interpolation |
| [CLI](cli.md) | `offside init` — agent skills and catalog templates |
| [API reference](api-reference.md) | Every public type and member, in one page |
| [FAQ](faq.md) | Design decisions and common pitfalls |

## The shape of it

```csharp
// Domain — knows nothing about HTTP
public Result<Order> Get(string id)
{
    var order = _orders.Find(id);
    return order is null
        ? Result<Order>.Failure(Error.NotFound("order", id))
        : Result<Order>.Success(order);
}
```

```csharp
// Endpoint — one line
app.MapGet("/orders/{id}", (string id, HttpContext http) => _orders.Get(id).ToHttpResult(http));
```

```json
// Response — 404, application/problem+json
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

## Packages

| Package | Version | Target frameworks | Role |
|---|---|---|---|
| `Offside` | [![NuGet](https://img.shields.io/nuget/v/Offside?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside) | `netstandard2.0`, `net8.0`, `net10.0` | `Error`, `ErrorKind`, `Result` / `Result<T>`, JSON resolver, `AddOffside` |
| `Offside.AspNetCore` | [![NuGet](https://img.shields.io/nuget/v/Offside.AspNetCore?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.AspNetCore) | `net8.0`, `net10.0` | `ToHttpResult` / `ToActionResult`, Problem Details, `AddOffsideAspNetCore` |
| `Offside.FluentValidation` | [![NuGet](https://img.shields.io/nuget/v/Offside.FluentValidation?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.FluentValidation) | `netstandard2.0`, `net8.0`, `net10.0` | FluentValidation failures → `Error` / `Result` |
| `Offside.FastEndpoint` | [![NuGet](https://img.shields.io/nuget/v/Offside.FastEndpoint?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.FastEndpoint) | `net8.0`, `net10.0` | `UseOffside`, `SendOffsideAsync`, OpenAPI expected errors |
| `Offside.AzureAppConfiguration` | [![NuGet](https://img.shields.io/nuget/v/Offside.AzureAppConfiguration?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.AzureAppConfiguration) | `netstandard2.0`, `net8.0`, `net10.0` | Dynamic resolver for catalogs loaded by Azure App Configuration |
| `Offside.Tool` | [![NuGet](https://img.shields.io/nuget/v/Offside.Tool?label=%20&logo=nuget)](https://www.nuget.org/packages/Offside.Tool) | `net8.0` | `offside init` — agent skills and catalog templates |

The core package has no ASP.NET dependency, so domain projects can reference it freely.

## Elsewhere

- [Changelog](../CHANGELOG.md) · [Contributing](../CONTRIBUTING.md) · [Support](../SUPPORT.md) · [Security](../SECURITY.md)
- [Design specification](superpowers/specs/2026-08-12-domain-errors-design.md) (Portuguese, internal)

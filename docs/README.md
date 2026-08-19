# Offside documentation

*[Português](pt-BR/README.md)*

Domain errors as `Result`, not exceptions. The domain returns an `Error`; ASP.NET Core maps it to Problem Details (RFC 7807). Messages live in JSON catalogs, not in C#.

## Start here

| Page | What it covers |
|---|---|
| [Getting started](getting-started.md) | Install, register, and return your first Problem Details response |
| [Concepts](concepts.md) | `Error`, `ErrorKind`, `Result`, primary error, catalogs, the escape hatch |
| [Domain guide](domain-guide.md) | Writing domain code with `Result<T>`: factories, `Custom`, `Bind`/`Map`/`Combine` |
| [ASP.NET Core guide](aspnet-guide.md) | `ToHttpResult` / `ToActionResult`, status selection, the response shape, 500 handling |
| [MediatR integration](mediatr-guide.md) | Publish result errors as notifications, collect them per scope, and handle retries safely |
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
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "code": "not_found", "kind": "NotFound", "detail": "order '42' was not found.", "field": null }
  ]
}
```

## Packages

| Package | Target frameworks | Role |
|---|---|---|
| `Offside` | `netstandard2.0`, `net8.0`, `net10.0` | `Error`, `ErrorKind`, `Result` / `Result<T>`, JSON resolver, `AddOffside` |
| `Offside.AspNetCore` | `net8.0`, `net10.0` | `ToHttpResult` / `ToActionResult`, Problem Details, `AddOffsideAspNetCore` |
| `Offside.AzureAppConfiguration` | `netstandard2.0`, `net8.0`, `net10.0` | Dynamic resolver for catalogs loaded by Azure App Configuration |
| `Offside.MediatR` | `netstandard2.0`, `net8.0`, `net10.0` | MediatR notifications for failed results and a scoped collector |
| `Offside.Tool` | `net8.0` | `offside init` — agent skills and catalog templates |

The core package has no ASP.NET or MediatR dependency, so domain projects can reference it freely.

## Elsewhere

- [Changelog](../CHANGELOG.md) · [Contributing](../CONTRIBUTING.md) · [Support](../SUPPORT.md) · [Security](../SECURITY.md)
- [Design specification](superpowers/specs/2026-08-12-domain-errors-design.md) (Portuguese, internal)

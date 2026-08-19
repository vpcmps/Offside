# Getting started

*[Português](pt-BR/getting-started.md) · [Back to docs](README.md)*

This page takes a project from nothing to a Problem Details response in four steps.

## 1. Install

```bash
dotnet add package Offside
```

Add the ASP.NET Core integration in the web host only:

```bash
dotnet add package Offside.AspNetCore
```

`Offside` targets `netstandard2.0`, `net8.0`, and `net10.0`. `Offside.AspNetCore` targets `net8.0` and `net10.0`.

FluentValidation or FastEndpoints hosts can add:

```bash
dotnet add package Offside.FluentValidation
dotnet add package Offside.FastEndpoint
```

See [FluentValidation](fluentvalidation.md) and [FastEndpoints](fastendpoints.md).

Optionally install the CLI to scaffold catalogs and agent skills — see the [CLI page](cli.md):

```bash
dotnet tool install -g Offside.Tool
offside init
```

## 2. Add a catalog

Offside never hard-codes message text. Create `errors/errors.json` with a template per error code:

```json
{
  "not_found": "{resource} '{id}' was not found.",
  "gone": "{resource} '{id}' is gone.",
  "conflict": "Conflict on {resource}.",
  "validation": "{field} is invalid.",
  "bad_request": "Bad request.",
  "unauthorized": "Unauthorized.",
  "forbidden": "Forbidden.",
  "precondition_failed": "Precondition failed.",
  "unprocessable": "Unable to process the request.",
  "too_many_requests": "Too many requests.",
  "unexpected": "An unexpected error occurred."
}
```

Make sure the file reaches the output directory:

```xml
<ItemGroup>
  <None Update="errors\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## 3. Register the services

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

Two things trip people up here:

- **`AddJson` takes the catalog *content*, not a path.** Read the file yourself, or pass a `Stream` for an embedded resource.
- **The invariant-culture catalog is required.** Without it `AddOffside` throws an `InvalidOperationException` at startup — deliberately, so a missing catalog is a boot failure rather than a surprise at 3 a.m.

`AddOffsideAspNetCore` registers `OffsideAspNetCoreOptions`. When an `IHostEnvironment` is present, `ExposeExceptionDetails` defaults to `IsDevelopment()`.

## 4. Return a result

The domain returns a `Result<T>` and knows nothing about HTTP:

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

The endpoint converts it in one call:

```csharp
app.MapGet("/orders/{id}", (string id, OrderService orders, HttpContext http) =>
    orders.Get(id).ToHttpResult(http));
```

A hit returns `200 OK` with the order. A miss returns:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
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

## Layering

Domain and application projects reference **`Offside` only**. `Offside.AspNetCore` belongs to the web host, which is the single place that knows about status codes and Problem Details. That boundary is what lets the same domain code back an HTTP API, a worker, and a CLI without change.

```
Domain / Application  ──►  Offside
Web host              ──►  Offside + Offside.AspNetCore
```

## Where next

- [Concepts](concepts.md) — the vocabulary, in one short page
- [Domain guide](domain-guide.md) — every factory and combinator
- [ASP.NET Core guide](aspnet-guide.md) — status selection, 500s, cultures
- [FluentValidation](fluentvalidation.md) — map validator failures to `Error`
- [FastEndpoints](fastendpoints.md) — `UseOffside` and `SendOffsideAsync`

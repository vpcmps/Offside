# Offside

*the domain called offside*

Domain errors as `Result`, not exceptions. The domain returns `Error`; ASP.NET Core maps that to Problem Details (RFC 7807). Messages live in JSON catalogs, not in C#.

## Install

```bash
dotnet add package Offside
dotnet add package Offside.AspNetCore
```

## Packages

| Package | Role |
|---|---|
| `Offside` | `Error`, `ErrorKind`, `Result` / `Result<T>`, JSON message resolver, `AddOffside` |
| `Offside.AspNetCore` | `ToHttpResult` / `ToActionResult`, Problem Details |

The Core package has no ASP.NET dependency.

## Example

```csharp
using Offside;
using Offside.AspNetCore;

Result GetOrder(string id)
{
    // domain / application — no HTTP here
    return Result.Failure(Error.NotFound("Order", id));
}

app.MapGet("/orders/{id}", (string id, IErrorMessageResolver resolver) =>
    GetOrder(id).ToHttpResult(resolver));
```

## JSON catalogs

`src/Offside/errors.json` is a reference catalog (English, built-in codes). Copy it into your host and register it — it is **not** embedded in the package.

```csharp
builder.Services.AddOffside(options =>
{
    options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors.json"));
    options.AddJson(new CultureInfo("pt-BR"), File.ReadAllText("errors.pt-BR.json"));
});
```

`AddOffside` requires a default catalog (`CultureInfo.InvariantCulture`). Missing keys fall back to the error `Code`. Extra cultures are optional (`errors.pt-BR.json`, `errors.en.json`, …).

## Spec

See [docs/superpowers/specs/2026-08-12-domain-errors-design.md](docs/superpowers/specs/2026-08-12-domain-errors-design.md).

# Offside

*the domain called offside*

Domain errors as `Result`, not exceptions. The domain returns `Error`; ASP.NET Core maps that to Problem Details (RFC 7807). Messages live in JSON catalogs, not in C#.

## Install

```bash
dotnet add package Offside
dotnet add package Offside.AspNetCore   # ASP.NET hosts only
```

Agent skills + catalog templates:

```bash
dotnet tool install -g Offside.Tool
offside init
```

`offside init` copies skills into `.cursor/skills`, `.agents/skills`, and `.claude/skills`, and writes `errors/errors.json` plus `errors/errors.pt-BR.json`. Use `--dir <path>` and `--force` as needed.

## Packages

| Package | Role |
|---|---|
| `Offside` | `Error`, `ErrorKind`, `Result` / `Result<T>`, JSON resolver, `AddOffside` |
| `Offside.AspNetCore` | `ToHttpResult` / `ToActionResult`, Problem Details, `AddOffsideAspNetCore` |
| `Offside.Tool` | `offside init` — skills and catalog templates |

The Core package has no ASP.NET dependency.

## Example

```csharp
using System.Globalization;
using Offside;
using Offside.AspNetCore;

builder.Services.AddOffside(options =>
{
    options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors/errors.json"));
});
builder.Services.AddOffsideAspNetCore();

Result GetOrder(string id) =>
    Result.Failure(Error.NotFound("Order", id));

app.MapGet("/orders/{id}", (string id, HttpContext http) =>
    GetOrder(id).ToHttpResult(http));
```

## Pack locally

```bash
dotnet pack -c Release -o artifacts
```

Produces `Offside`, `Offside.AspNetCore`, and `Offside.Tool` nupkgs (plus snupkgs).

## CI and publish

CI builds, tests (net8 + net10), and packs on `master` and pull requests.

To publish to nuget.org, add a [Trusted Publishing](https://www.nuget.org/account/trustedpublishing) policy:

- Repository Owner: `vpcmps`
- Repository: `Offside`
- Workflow File: `release.yml`
- Environment: leave blank

Then push a version tag (the tag is the package version):

```bash
git tag v0.1.0
git push origin v0.1.0
```

## Spec

See [docs/superpowers/specs/2026-08-12-domain-errors-design.md](docs/superpowers/specs/2026-08-12-domain-errors-design.md).

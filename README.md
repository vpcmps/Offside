# Offside

*the domain called offside*

Domain errors as `Result`, not exceptions. The domain returns `Error`; ASP.NET Core maps that to Problem Details (RFC 7807). Messages live in JSON catalogs, not in C#.

**[Documentation](https://github.com/vpcmps/Offside/blob/master/docs/README.md)** · [Português](https://github.com/vpcmps/Offside/blob/master/docs/pt-BR/README.md)

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

## Compatibility and status

`Offside` supports `netstandard2.0`, `net8.0`, and `net10.0`. `Offside.AspNetCore` supports `net8.0` and `net10.0`; `Offside.Tool` runs on `net8.0`.

The project is pre-1.0. Minor releases may include breaking changes. Releases follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html), and notable changes are recorded in [CHANGELOG.md](CHANGELOG.md).

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

A failure becomes `application/problem+json`, with the status taken from the most severe error present:

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "NotFound",
  "status": 404,
  "detail": "Order '42' was not found.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "code": "not_found", "kind": "NotFound", "detail": "Order '42' was not found.", "field": null }
  ]
}
```

| ErrorKind | Status | | ErrorKind | Status |
|---|---|---|---|---|
| `Unexpected` | 500 | | `Gone` | 410 |
| `Unauthorized` | 401 | | `Unprocessable` | 422 |
| `Forbidden` | 403 | | `NotFound` | 404 |
| `TooManyRequests` | 429 | | `Validation` | 400 |
| `Conflict` | 409 | | `BadRequest` | 400 |
| `PreconditionFailed` | 412 | | | |

Full guides: [getting started](https://github.com/vpcmps/Offside/blob/master/docs/getting-started.md) · [concepts](https://github.com/vpcmps/Offside/blob/master/docs/concepts.md) · [domain](https://github.com/vpcmps/Offside/blob/master/docs/domain-guide.md) · [ASP.NET Core](https://github.com/vpcmps/Offside/blob/master/docs/aspnet-guide.md) · [messages](https://github.com/vpcmps/Offside/blob/master/docs/messages.md) · [API reference](https://github.com/vpcmps/Offside/blob/master/docs/api-reference.md)

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

The internal [design specification](https://github.com/vpcmps/Offside/blob/master/docs/superpowers/specs/2026-08-12-domain-errors-design.md) records the original decisions (in Portuguese). For usage, prefer the [documentation](https://github.com/vpcmps/Offside/blob/master/docs/README.md).

## Community and security

Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. For help, see [SUPPORT.md](SUPPORT.md). Report vulnerabilities privately according to [SECURITY.md](SECURITY.md).

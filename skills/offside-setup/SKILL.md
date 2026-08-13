---
name: offside-setup
description: Adds Offside domain-error packages, JSON catalogs, and DI to a .NET project. Use when the user asks to install Offside, add domain errors, Result pattern, Problem Details, or run offside init.
---

# Offside setup

Wire Offside into the **current** .NET project. Do not scaffold a new repo unless asked.

## Checklist

- [ ] Detect host (ASP.NET vs worker/CLI)
- [ ] Add NuGet packages
- [ ] Copy JSON catalogs (`errors.json` required)
- [ ] Register DI
- [ ] Map HTTP if ASP.NET

## Packages

```bash
dotnet add package Offside
```

If the project is ASP.NET Core (Web SDK, `Microsoft.AspNetCore.App`, Minimal APIs, or controllers):

```bash
dotnet add package Offside.AspNetCore
```

Prefer the latest stable from nuget.org. For a local build, `dotnet add package Offside --source <artifacts-dir>`.

## Catalogs

Copy templates next to the host project (or `errors/`):

- `errors.json` — invariant/default (required)
- `errors.pt-BR.json` — optional extra culture

Source of templates: `offside init` output, or `src/Offside/errors.json` in the Offside repo.

Register **file contents**, not paths. `AddOffside` throws if the invariant catalog is missing.

```csharp
using System.Globalization;
using Offside;
using Offside.AspNetCore;

builder.Services.AddOffside(options =>
{
    options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors.json"));
    var ptBr = Path.Combine(builder.Environment.ContentRootPath, "errors.pt-BR.json");
    if (File.Exists(ptBr))
        options.AddJson(new CultureInfo("pt-BR"), File.ReadAllText(ptBr));
});

builder.Services.AddOffsideAspNetCore(); // ASP.NET only
```

Copy `errors.json` to output:

```xml
<ItemGroup>
  <None Update="errors.json;errors.pt-BR.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## HTTP

Minimal APIs:

```csharp
app.MapGet("/orders/{id}", (string id, HttpContext http) =>
    GetOrder(id).ToHttpResult(http));
```

Controllers: `return result.ToActionResult(resolver, culture);` or resolve `IErrorMessageResolver` from DI.

Domain/application projects reference **only** `Offside`. They never reference `Offside.AspNetCore`.

## After setup

Point the user at `offside-domain` (Error/Result) and `offside-aspnet` (Problem Details).

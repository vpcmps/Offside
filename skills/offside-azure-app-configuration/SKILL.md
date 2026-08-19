---
name: offside-azure-app-configuration
description: Configures Azure App Configuration as Offside's dynamic message source, including section layout, dependency injection, and refresh for ASP.NET Core or worker hosts.
---

# Offside Azure App Configuration

Use Azure App Configuration as the selected message source. This choice is independent of standard ASP.NET Core versus FastEndpoints exposure.

## Packages and configuration

Add `Offside.AzureAppConfiguration` and the Azure App Configuration provider used by the host. ASP.NET Core refresh also needs `Microsoft.Azure.AppConfiguration.AspNetCore`.

The Offside package reads the host's `IConfiguration`; credentials, endpoint, labels, selectors, and refresh intervals remain host responsibilities. Prefer the repository's existing authentication strategy.

Store messages below `Errors` by default:

```text
Errors:default:not_found = missing {resource}
Errors:pt-BR:not_found   = nao encontrado {resource}
```

`default` is required and is the final fallback. A different root requires `options.SectionName`.

## Registration

Register Azure as the only Offside message resolver:

```csharp
using Azure.Identity;

builder.Configuration.AddAzureAppConfiguration(options => options
    .Connect(new Uri(builder.Configuration["AppConfig:Endpoint"]!),
        new DefaultAzureCredential())
    .Select("Errors:*")
    .ConfigureRefresh(refresh => refresh.RegisterAll()));

builder.Services.AddAzureAppConfiguration();
builder.Services.AddOffsideAzureAppConfiguration(builder.Configuration);
```

Do not also call `AddOffside`; that would register the local JSON resolver. Add `AddOffsideAspNetCore()` only when the selected exposure uses HTTP. FastEndpoints additionally follows `offside-fastendpoint`.

For ASP.NET Core refresh, call `app.UseAzureAppConfiguration()` in the appropriate middleware order. Workers have no request middleware and must trigger an `IConfigurationRefresher` according to their execution model.

## Verify

Confirm `Errors:default` exists, an unknown localized key falls back to `default`, refresh changes the next resolution, and DI contains the intended `IErrorMessageResolver` only.

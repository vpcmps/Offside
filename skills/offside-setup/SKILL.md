---
name: offside-setup
description: Configures a .NET project to use Offside with a user-selected message source, host adapter, validation, and optional MediatR notifications. Use when installing Offside or changing its project-level integrations.
---

# Offside setup

Configure the **current** .NET project. Do not scaffold a new repository unless asked.

## Confirm the capability selection

Inspect project files and existing registrations first. Preselect only capabilities already present, clearly marking them as detected. Do not install or remove a capability based on detection alone.

Before changing files, ask the user to confirm one option from each required group and any optional integrations. Use a structured checkbox or multi-select UI when available. Otherwise show this numbered Markdown checklist and wait for a reply:

```text
Message source — select exactly one:
[ ] 1. Local JSON catalogs
[ ] 2. Azure App Configuration
[ ] 3. Custom IErrorMessageResolver

Exposure — select exactly one:
[ ] 4. Domain/application only (no HTTP)
[ ] 5. Standard ASP.NET Core
[ ] 6. ASP.NET Core with FastEndpoints

Validation — optional:
[ ] 7. FluentValidation

Notifications — optional:
[ ] 8. MediatR domain notifications
```

FastEndpoints is an exposure choice, not a message source. All message-source and exposure combinations are valid. After confirmation, summarize the selection, packages, and files to change before editing.

## Packages

Always add `Offside`. Add only the selected integrations:

| Selection | Packages |
|---|---|
| Local JSON | no additional message-source package |
| Azure App Configuration | `Offside.AzureAppConfiguration`, plus the host's Azure App Configuration provider |
| Custom resolver | no additional Offside package |
| Standard ASP.NET Core | `Offside.AspNetCore` |
| FastEndpoints | `Offside.AspNetCore`, `Offside.FastEndpoint` |
| FluentValidation | `Offside.FluentValidation`; FastEndpoints hosts also use their normal FastEndpoints validation package |
| MediatR domain notifications | `Offside.MediatR`; the host remains responsible for registering MediatR and `IPublisher` |

Prefer the latest stable packages from nuget.org unless the repository pins versions or uses a local feed.

## Register the selected message source

### Local JSON

Use the catalogs created by `offside init` and copy them to output:

```csharp
builder.Services.AddOffside(options =>
{
    options.AddJsonFile(CultureInfo.InvariantCulture, "errors/errors.json");
});
```

```xml
<None Update="errors\**\*.json" CopyToOutputDirectory="PreserveNewest" />
```

The invariant `errors/errors.json` catalog is required. Prefer `AddJsonFile` (relative paths resolve against `AppContext.BaseDirectory`); a missing file fails at startup and names the path.

### Azure App Configuration

Use `offside-azure-app-configuration`. Register `AddOffsideAzureAppConfiguration(configuration)` **instead of** `AddOffside`; do not also register the JSON resolver.

### Custom resolver

Implement `IErrorMessageResolver` and register that implementation directly. Do not call `AddOffside`, which registers the JSON resolver too. Return `error.Code` when no message exists.

## Register the selected exposure

- No HTTP: keep HTTP packages and types out of domain/application projects.
- Standard ASP.NET Core: call `AddOffsideAspNetCore()` in the host and map results using `offside-aspnet`.
- FastEndpoints: call `AddOffsideAspNetCore()` and configure the pipeline using `offside-fastendpoint`.

Use `offside-fluentvalidation` when standalone FluentValidation mapping was selected. FastEndpoints validation failures are integrated by `offside-fastendpoint`.

Use `offside-mediatr` when MediatR domain notifications were selected. Configure MediatR first, then call `AddOffsideMediatR()`; do not register MediatR or `IPublisher` implicitly.

## Verify

Build the affected projects and run relevant tests. Confirm there is exactly one `IErrorMessageResolver`, HTTP types remain in the host, and the selected source resolves the invariant/default catalog.

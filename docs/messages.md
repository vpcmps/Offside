# Messages and cultures

*[Português](pt-BR/messages.md) · [Back to docs](README.md)*

Error text lives in JSON catalogs, one per culture. C# holds the metadata — code, kind, arguments — and never the wording.

## Catalog format

A flat JSON object mapping error code to message template:

```json
{
  "not_found": "{resource} '{id}' was not found.",
  "conflict": "Conflict on {resource}.",
  "validation": "{field} is invalid.",
  "unexpected": "An unexpected error occurred."
}
```

The eleven default codes — `not_found`, `gone`, `conflict`, `validation`, `bad_request`, `unauthorized`, `forbidden`, `precondition_failed`, `unprocessable`, `too_many_requests`, `unexpected` — cover every built-in factory. Add one entry per custom code you introduce with `Error.Custom`.

## Registration

```csharp
builder.Services.AddOffside(options =>
{
    options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors/errors.json"));
    options.AddJson(new CultureInfo("pt-BR"),     File.ReadAllText("errors/errors.pt-BR.json"));
    options.AddJson(new CultureInfo("es"),        File.ReadAllText("errors/errors.es.json"));
});
```

`AddJson` takes the catalog **content**, not a path. There is also a `Stream` overload for embedded resources:

```csharp
options.AddJson(CultureInfo.InvariantCulture,
    typeof(Program).Assembly.GetManifestResourceStream("MyApp.errors.json")!);
```

Catalogs are parsed once, at registration. A malformed file fails at startup, not on the first failing request.

**An invariant-culture catalog is required.** Without it, `AddOffside` throws `InvalidOperationException`. It is the final fallback, so every code should appear there.

## Culture fallback

Resolution tries three lookups in order and stops at the first hit:

1. The exact culture — `pt-BR`
2. Its parent — `pt`
3. The invariant catalog

So a `pt` catalog serves `pt-BR` and `pt-PT` alike, and you translate the specific ones only where the wording actually differs. If no catalog defines the code at all, the resolver returns **the code itself** — a response stays well-formed and the missing entry is visible rather than silently blank.

In an ASP.NET Core host the culture comes from the `Accept-Language` header unless you pass one explicitly. See [Cultures](aspnet-guide.md#cultures).

## Interpolation

Template tokens are `{name}`, filled from `Error.Arguments`:

```csharp
Error.NotFound("order", 42)
// arguments: resource = "order", id = 42
```

```json
{ "not_found": "{resource} '{id}' was not found." }
```

```
order '42' was not found.
```

Three behaviours to know:

- **A token with no matching argument stays literal.** `Error.NotFound("order")` against the template above yields `order '{id}' was not found.` — a null argument is skipped, not blanked. Visible, not silent.
- **Values format with `InvariantCulture`.** Numbers and dates come out stable regardless of the request's culture. Format them yourself before passing them in if you need locale-aware output.
- **This is token replacement, not `string.Format`.** There is no `{0}`, no format specifiers such as `{amount:C}`, and no escaping — braces that match no argument survive as written.

## Adding a language

1. Copy `errors/errors.json` to `errors/errors.<culture>.json`.
2. Translate the values. Leave the keys and the `{tokens}` untouched.
3. Register it with `options.AddJson(new CultureInfo("<culture>"), ...)`.

```json
{
  "not_found": "{resource} '{id}' não foi encontrado.",
  "conflict": "Conflito em {resource}.",
  "validation": "{field} é inválido.",
  "unexpected": "Ocorreu um erro inesperado."
}
```

A translated catalog does not have to be complete. Anything it omits falls back to the parent culture and then to the invariant catalog, so you can ship a partial translation and fill it in over time.

`offside init` writes an English and a Brazilian Portuguese catalog to start from — see the [CLI page](cli.md).

## Azure App Configuration

Install the optional integration when Azure App Configuration is your catalog source:

```bash
dotnet add package Offside.AzureAppConfiguration
dotnet add package Microsoft.Extensions.Configuration.AzureAppConfiguration
dotnet add package Microsoft.Azure.AppConfiguration.AspNetCore # ASP.NET Core refresh
```

The package reads a section from the host's `IConfiguration`; it does not connect to Azure or choose labels. The default section is `Errors`, followed by a culture and message code:

```text
Errors:default:not_found = missing {resource}
Errors:pt-BR:not_found   = nao encontrado {resource}
```

`default` is required and is the final fallback. You can instead store a culture catalog under `Errors:pt-BR` with content type `application/json`; Azure flattens it into the same hierarchy:

```json
{ "not_found": "nao encontrado {resource}" }
```

Configure Azure in the host, select `Errors:*` and enable refresh. Register this resolver **instead of** `AddOffside`:

```csharp
using Azure.Identity;
using Offside.AzureAppConfiguration;

builder.Configuration.AddAzureAppConfiguration(options => options
    .Connect(new Uri(builder.Configuration["AppConfig:Endpoint"]!), new DefaultAzureCredential())
    .Select("Errors:*")
    .ConfigureRefresh(refresh => refresh.RegisterAll()));

builder.Services.AddAzureAppConfiguration();
builder.Services.AddOffsideAzureAppConfiguration(builder.Configuration);

var app = builder.Build();
app.UseAzureAppConfiguration();
```

The resolver reads configuration for every lookup, so a completed refresh affects the next response without a restart. Workers must trigger the refresher themselves; labels, credentials, selectors, and refresh intervals remain host concerns. To use another root, pass `options => options.SectionName = "MyErrors"`.

## Custom resolvers

`AddOffside` registers a `JsonErrorMessageResolver` as the singleton `IErrorMessageResolver`. To source messages from somewhere else — a database, satellite resource assemblies, a translation service — implement the interface and register it instead:

```csharp
public sealed class ResxErrorMessageResolver : IErrorMessageResolver
{
    public string GetMessage(Error error, CultureInfo culture) =>
        Messages.ResourceManager.GetString(error.Code, culture) ?? error.Code;
}

builder.Services.AddSingleton<IErrorMessageResolver, ResxErrorMessageResolver>();
```

Do not call `AddOffside` in that case — it would register the JSON resolver alongside yours. Returning `error.Code` for an unknown code is the convention worth keeping: it degrades to something diagnosable instead of an empty string.

# FastEndpoints

*[Português](pt-BR/fastendpoints.md) · [Back to docs](README.md)*

`Offside.FastEndpoint` is the only Offside package that references FastEndpoints. Validation failures and `Result` failures both become the same `OffsideProblem` document.

```bash
dotnet add package Offside
dotnet add package Offside.AspNetCore
dotnet add package Offside.FastEndpoint
```

Targets: `net8.0`, `net10.0`. Depends on FastEndpoints 7.x.

## Startup

```csharp
builder.Services.AddOffside(options => { /* catalogs */ });
builder.Services.AddOffsideAspNetCore();
builder.Services.AddFastEndpoints();

app.UseFastEndpoints(c => c.UseOffside());
```

`UseOffside` does four things:

- Sets `Errors.ResponseBuilder` so FluentValidation failures serialize as `OffsideProblem`.
- Sets `Errors.ProducesMetadataType` to `typeof(OffsideProblem)`.
- Sets `Errors.ContentType` to `application/problem+json`.
- Registers `Produces<OffsideProblem>` for every Offside status (400, 401, 403, 404, 409, 410, 412, 422, 429, 500) on all endpoints.

FastEndpoints does not expose the previous `Endpoints.Configurator` to other assemblies. If you already have one, pass it in:

```csharp
app.UseFastEndpoints(c => c.UseOffside(ep =>
{
    ep.AllowAnonymous();
}));
```

Do not assign `c.Endpoints.Configurator` after `UseOffside` — that replaces the Offside metadata.

## Opt out

Health and other endpoints that should not advertise Offside error statuses:

```csharp
public override void Configure()
{
    Get("/health");
    Definition.DontProduceOffside();
}
```

## Sending a Result

```csharp
public override Task HandleAsync(CancellationToken ct) =>
    orders.Get(id).SendOffsideAsync(HttpContext, ct);
```

Success is 204 for `Result` and 200 with the value for `Result<T>`. Failure is the usual Offside problem document.

## FluentValidation

Use `.WithErrorCode("email.required")` to set the catalog key (`Error.Code`). The screen identifier for those errors is `VALIDATION`. See [FluentValidation](fluentvalidation.md).

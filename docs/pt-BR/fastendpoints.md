# FastEndpoints

*[English](../fastendpoints.md) · [Voltar à docs](README.md)*

`Offside.FastEndpoint` é o único pacote Offside que referencia FastEndpoints. Falhas de validação e falhas de `Result` viram o mesmo documento `OffsideProblem`.

```bash
dotnet add package Offside
dotnet add package Offside.AspNetCore
dotnet add package Offside.FastEndpoint
```

Targets: `net8.0`, `net10.0`. Depende de FastEndpoints 7.x.

## Registro

```csharp
builder.Services.AddOffside(options => { /* catálogos */ });
builder.Services.AddOffsideAspNetCore();
builder.Services.AddFastEndpoints();

app.UseFastEndpoints(c => c.UseOffside());
```

`UseOffside` faz quatro coisas:

- Define `Errors.ResponseBuilder` para serializar falhas FluentValidation como `OffsideProblem`.
- Define `Errors.ProducesMetadataType` como `typeof(OffsideProblem)`.
- Define `Errors.ContentType` como `application/problem+json`.
- Regista `Produces<OffsideProblem>` para cada status Offside (400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504) em todos os endpoints.

O FastEndpoints não expõe o `Endpoints.Configurator` anterior a outros assemblies. Se já tiver um, passe-o:

```csharp
app.UseFastEndpoints(c => c.UseOffside(ep =>
{
    ep.AllowAnonymous();
}));
```

Não atribua `c.Endpoints.Configurator` depois de `UseOffside` — isso substitui os metadados Offside.

## Opt-out

Health e outros endpoints que não devem anunciar status de erro Offside:

```csharp
public override void Configure()
{
    Get("/health");
    Definition.DontProduceOffside();
}
```

## Enviar um Result

```csharp
public override Task HandleAsync(CancellationToken ct) =>
    orders.Get(id).SendOffsideAsync(HttpContext, ct);
```

Sucesso é 204 para `Result` e 200 com o valor para `Result<T>`. Falha é o documento de problema Offside habitual.

## FluentValidation

Use `.WithErrorCode("email.required")` para a chave do catálogo (`Error.Code`). O identificador de tela desses erros é `VALIDATION`. Ver [FluentValidation](fluentvalidation.md).

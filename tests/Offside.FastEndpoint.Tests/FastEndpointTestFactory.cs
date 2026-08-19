using System.Globalization;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Offside.AspNetCore;
using Offside.FastEndpoint;

namespace Offside.FastEndpoint.Tests;

internal sealed class FastEndpointTestFactory : IDisposable
{
    private readonly WebApplication _app;

    public FastEndpointTestFactory()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints();
        builder.Services.AddOffside(options =>
        {
            options.AddJson(
                CultureInfo.InvariantCulture,
                """
                {
                  "validation": "{field}",
                  "email.required": "email is required",
                  "not_found": "{resource}"
                }
                """);
        });
        builder.Services.AddOffsideAspNetCore();

        _app = builder.Build();
        _app.UseFastEndpoints(config => config.UseOffside());
        _app.Start();
    }

    public IServiceProvider Services => _app.Services;

    public HttpClient CreateClient() => _app.GetTestClient();

    public void Dispose() => ((IHost)_app).Dispose();
}

internal sealed class CreateUserRequest
{
    public string Email { get; set; } = "";
}

internal sealed class CreateUserValidator : Validator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithErrorCode("email.required");
    }
}

internal sealed class CreateUserEndpoint : Endpoint<CreateUserRequest>
{
    public override void Configure()
    {
        Post("/users");
        AllowAnonymous();
    }

    public override Task HandleAsync(CreateUserRequest req, CancellationToken ct) =>
        Send.OkAsync(cancellation: ct);
}

internal sealed class HealthEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Definition.DontProduceOffside();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync("ok", cancellation: ct);
}

internal sealed class GetOrderEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/orders/{id}");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Result.Failure(Error.NotFound("order", "missing")).SendOffsideAsync(HttpContext, ct);
}

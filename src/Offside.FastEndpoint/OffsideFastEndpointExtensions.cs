using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Offside.AspNetCore;

namespace Offside.FastEndpoint;

/// <summary>
/// Wires Offside into FastEndpoints: Problem Details responses, OpenAPI metadata, and the
/// global expected-error statuses.
/// </summary>
public static class OffsideFastEndpointExtensions
{
    internal const string SkipProducesTag = "Offside.DontProduce";

    /// <summary>
    /// Sends Offside Problem Details for FastEndpoints validation failures, documents
    /// <see cref="OffsideProblem"/> as the error DTO, and registers every Offside status
    /// as an expected response on all endpoints.
    /// Pass <paramref name="configure"/> for extra per-endpoint setup; FastEndpoints does
    /// not expose the previous configurator to other assemblies.
    /// </summary>
    /// <param name="config">The FastEndpoints configuration.</param>
    /// <param name="configure">Optional extra configuration run before Offside metadata is applied.</param>
    /// <returns><paramref name="config"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    public static Config UseOffside(this Config config, Action<EndpointDefinition>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.Endpoints.Configurator = definition =>
        {
            configure?.Invoke(definition);
            ProduceOffsideErrors(definition);
        };

        config.Errors.ResponseBuilder = static (failures, httpContext, _) =>
            OffsideValidationResponse.Create(failures, httpContext);
        config.Errors.ProducesMetadataType = typeof(OffsideProblem);
        config.Errors.ContentType = "application/problem+json";
        return config;
    }

    /// <summary>
    /// Opts this endpoint out of the global Offside <c>Produces</c> metadata.
    /// Call from <c>Configure()</c>.
    /// </summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <see langword="null"/>.</exception>
    public static void DontProduceOffside(this EndpointDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Tags(SkipProducesTag);
    }

    private static void ProduceOffsideErrors(EndpointDefinition definition)
    {
        if (definition.EndpointTags?.Contains(SkipProducesTag) is true)
            return;

        definition.Description(builder =>
        {
            builder.ClearDefaultProduces(400);
            foreach (var status in OffsideHttp.StatusCodes)
                builder.Produces<OffsideProblem>(status, "application/problem+json");
        });
    }
}

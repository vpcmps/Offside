using global::MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Offside.MediatR.Compatibility.Tests;

public sealed class MediatRCompatibilityTests
{
    [Fact]
    public async Task Configured_mediatr_can_publish_and_collect_offside_errors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<MediatRCompatibilityTests>());
        services.AddOffsideMediatR();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = Error.NotFound("order", 42);

        var result = await Result.Failure(error).PublishDomainNotificationsAsync(
            scope.ServiceProvider.GetRequiredService<IPublisher>());

        Assert.Same(error, Assert.Single(result.Errors));
        Assert.Same(
            error,
            Assert.Single(scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>().Errors));
    }
}

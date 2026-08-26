using global::MediatR;
using Microsoft.Extensions.DependencyInjection;
using Offside.Testing;
using Xunit;

namespace Offside.MediatR.Tests;

public sealed class DomainNotificationCollectorTests
{
    [Fact]
    public void Registration_rejects_null_service_collection()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => services.AddOffsideMediatR());

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void Empty_collector_converts_to_success_results()
    {
        using var provider = CreateCollectorProvider();
        using var scope = provider.CreateScope();
        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();

        var result = collector.ToResult();
        var resultT = collector.ToResult(42);

        Assert.False(collector.HasNotifications);
        Assert.Empty(collector.Errors);
        result.ShouldBeSuccess();
        resultT.ShouldBeSuccess();
        Assert.Equal(42, resultT.Value);
    }

    [Fact]
    public async Task Published_notifications_are_collected_and_convert_to_failures()
    {
        using var provider = CreateMediatRProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();
        var error = Error.NotFound("order", 7);

        await publisher.Publish(new DomainNotification(error));

        Assert.True(collector.HasNotifications);
        Assert.Same(error, Assert.Single(collector.Errors));
        Assert.Same(error, Assert.Single(collector.ToResult().Errors));
        Assert.Same(error, Assert.Single(collector.ToResult("unused").Errors));
    }

    [Fact]
    public async Task Error_snapshots_are_persistent_and_independent()
    {
        using var provider = CreateMediatRProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();
        var first = Error.NotFound("order", 1);
        var second = Error.NotFound("order", 2);

        await publisher.Publish(new DomainNotification(first));
        var firstSnapshot = collector.Errors;
        await publisher.Publish(new DomainNotification(second));

        Assert.Equal(new[] { first }, firstSnapshot);
        Assert.Equal(new[] { first, second }, collector.Errors);
    }

    [Fact]
    public async Task Collector_is_isolated_per_scope()
    {
        using var provider = CreateMediatRProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstError = Error.Conflict("order");

        await firstScope.ServiceProvider.GetRequiredService<IPublisher>()
            .Publish(new DomainNotification(firstError));

        Assert.Same(
            firstError,
            Assert.Single(firstScope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>().Errors));
        Assert.Empty(secondScope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>().Errors);
    }

    [Fact]
    public async Task Repeated_registration_collects_each_notification_once()
    {
        var services = new ServiceCollection();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<DomainNotificationCollectorTests>());
        services.AddOffsideMediatR();
        services.AddOffsideMediatR();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = Error.BadRequest();

        await scope.ServiceProvider.GetRequiredService<IPublisher>()
            .Publish(new DomainNotification(error));

        Assert.Same(
            error,
            Assert.Single(scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>().Errors));
    }

    [Fact]
    public void Collector_and_notification_handler_resolve_to_the_same_scoped_instance()
    {
        using var provider = CreateCollectorProvider();
        using var scope = provider.CreateScope();

        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();
        var handler = scope.ServiceProvider.GetRequiredService<INotificationHandler<DomainNotification>>();

        Assert.Same(collector, handler);
    }

    [Fact]
    public async Task Separate_notification_instances_with_the_same_error_are_both_collected()
    {
        using var provider = CreateMediatRProvider();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();
        var error = Error.BadRequest();

        await publisher.Publish(new DomainNotification(error));
        await publisher.Publish(new DomainNotification(error));

        Assert.Equal(new[] { error, error }, collector.Errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Integration_assembly_scanning_does_not_duplicate_collection(bool scanBeforeRegistration)
    {
        var services = new ServiceCollection();
        if (scanBeforeRegistration)
            AddMediatRScanningIntegration(services);

        services.AddOffsideMediatR();

        if (!scanBeforeRegistration)
            AddMediatRScanningIntegration(services);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = Error.Unprocessable();

        await scope.ServiceProvider.GetRequiredService<IPublisher>()
            .Publish(new DomainNotification(error));

        Assert.Same(
            error,
            Assert.Single(scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>().Errors));
    }

    [Fact]
    public async Task Concurrent_handlers_do_not_lose_notifications()
    {
        using var provider = CreateCollectorProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<INotificationHandler<DomainNotification>>();
        var notifications = Enumerable.Range(1, 100)
            .Select(index => new DomainNotification(Error.NotFound("order", index)))
            .ToArray();

        await Task.WhenAll(notifications.Select(notification =>
            handler.Handle(notification, CancellationToken.None)));

        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();
        Assert.Equal(100, collector.Errors.Count);
    }

    [Fact]
    public void Registration_does_not_add_mediatr_publisher()
    {
        var services = new ServiceCollection();
        services.AddOffsideMediatR();
        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<IPublisher>());
    }

    private static ServiceProvider CreateCollectorProvider()
    {
        var services = new ServiceCollection();
        services.AddOffsideMediatR();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateMediatRProvider()
    {
        var services = new ServiceCollection();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<DomainNotificationCollectorTests>());
        services.AddOffsideMediatR();
        return services.BuildServiceProvider();
    }

    private static void AddMediatRScanningIntegration(IServiceCollection services) =>
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DomainNotification).Assembly));
}

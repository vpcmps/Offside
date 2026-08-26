using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Offside.MediatR;
using Offside.Testing;
using Xunit;

namespace Offside.OpenTelemetry.MediatR.Tests;

public class DomainNotificationTelemetryTests
{
    private sealed class RecordingRecorder : IDomainErrorRecorder
    {
        public List<Error> Recorded { get; } = new();

        public void Record(Error error, IReadOnlyDictionary<string, string>? properties = null) =>
            Recorded.Add(error);

        /// <summary>The recorded errors as a failed result, so the Offside assertions apply.</summary>
        public Result AsResult() => Result.Failure(Recorded);
    }

    private static ServiceProvider Provider(RecordingRecorder recorder, bool withCollector = false)
    {
        var services = new ServiceCollection()
            .AddSingleton<IDomainErrorRecorder>(recorder)
            .AddMediatR(configuration =>
                configuration.RegisterServicesFromAssemblyContaining<DomainNotificationTelemetryTests>())
            .AddOffsideOpenTelemetryMediatR();

        if (withCollector)
            services.AddOffsideMediatR();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task A_published_notification_is_recorded()
    {
        var recorder = new RecordingRecorder();
        using var provider = Provider(recorder);
        using var scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IPublisher>()
            .Publish(new DomainNotification(Error.Conflict("order", "already shipped")));

        recorder.AsResult()
            .ShouldHaveOnlyError("conflict")
            .WithKind(ErrorKind.Conflict)
            .WithArgument("reason", "already shipped");
    }

    [Fact]
    public async Task A_failed_result_reaches_telemetry_through_the_publisher()
    {
        var recorder = new RecordingRecorder();
        using var provider = Provider(recorder);
        using var scope = provider.CreateScope();
        var result = Result.Failure(Error.NotFound("order", 42), Error.Unauthorized("token expired"));

        await result.PublishDomainNotificationsAsync(
            scope.ServiceProvider.GetRequiredService<IPublisher>());

        recorder.AsResult().ShouldHaveErrorsInOrder("not_found", "unauthorized");
    }

    [Fact]
    public async Task The_bridge_runs_alongside_the_collector()
    {
        var recorder = new RecordingRecorder();
        using var provider = Provider(recorder, withCollector: true);
        using var scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IPublisher>()
            .Publish(new DomainNotification(Error.NotFound("order", 42)));

        var collector = scope.ServiceProvider.GetRequiredService<IDomainNotificationCollector>();
        Assert.Single(recorder.Recorded);
        Assert.Single(collector.Errors);
    }

    [Fact]
    public void Registering_twice_records_each_notification_once()
    {
        var services = new ServiceCollection()
            .AddSingleton<IDomainErrorRecorder>(new RecordingRecorder())
            .AddOffsideOpenTelemetryMediatR()
            .AddOffsideOpenTelemetryMediatR();

        Assert.Single(
            services,
            descriptor => descriptor.ImplementationType == typeof(DomainNotificationTelemetryHandler));
    }

    [Fact]
    public void A_null_recorder_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new DomainNotificationTelemetryHandler(null!));
    }
}

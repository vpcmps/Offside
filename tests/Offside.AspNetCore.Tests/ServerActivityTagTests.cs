using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class ServerActivityTagTests
{
    [Theory]
    [InlineData(false, false, ActivityStatusCode.Error)]
    [InlineData(true, false, ActivityStatusCode.Ok)]
    [InlineData(true, true, ActivityStatusCode.Error)]
    public async Task Tags_server_span_and_applies_only_opted_in_client_status(
        bool enabled, bool serverError, ActivityStatusCode status)
    {
        using var server = new Activity("server").Start();
        server.SetStatus(ActivityStatusCode.Error);
        using var child = new Activity("child").Start();
        var error = serverError ? Error.Unexpected("private") : Error.NotFound("order", 42);

        await ProblemHttpHarness.Execute(Result.Failure(error), new OffsideAspNetCoreOptions
        {
            ActivityTagPrefix = "business.",
            HandledProblemStatus = enabled ? ActivityStatusPolicy.OkForClientErrors : ActivityStatusPolicy.Unchanged
        }, http => http.Features.Set<IHttpActivityFeature>(new ServerActivity(server)));

        Assert.Equal(error.Kind.ToString(), server.GetTagItem("business.outcome"));
        Assert.Equal(error.ErrorCode, server.GetTagItem("business.errorCode"));
        Assert.Null(child.GetTagItem("business.outcome"));
        Assert.Equal(status, server.Status);
    }

    [Fact]
    public async Task Client_status_wins_over_recorder_on_the_same_activity()
    {
        using var server = new Activity("server").Start();
        using var services = new ServiceCollection()
            .AddSingleton<IDomainErrorRecorder>(new FailingRecorder())
            .BuildServiceProvider();
        await ProblemHttpHarness.Execute(
            Result.Failure(Error.NotFound("order")),
            new OffsideAspNetCoreOptions { HandledProblemStatus = ActivityStatusPolicy.OkForClientErrors },
            http =>
            {
                http.RequestServices = services;
                http.Features.Set<IHttpActivityFeature>(new ServerActivity(server));
            });
        Assert.Equal(ActivityStatusCode.Ok, server.Status);
    }

    private sealed class FailingRecorder : IDomainErrorRecorder
    {
        public void Record(Error error, IReadOnlyDictionary<string, string>? properties = null) =>
            Activity.Current!.SetStatus(ActivityStatusCode.Error);
    }

    private sealed class ServerActivity(Activity activity) : IHttpActivityFeature
    {
        public Activity Activity { get; set; } = activity;
    }
}

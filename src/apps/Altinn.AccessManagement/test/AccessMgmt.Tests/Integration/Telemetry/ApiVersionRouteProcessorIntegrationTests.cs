using System.Diagnostics;
using Altinn.AccessManagement.Telemetry;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Altinn.AccessManagement.Tests.Integration.Telemetry;

/// <summary>
/// Runs <see cref="ApiVersionRouteProcessor"/> against a real ASP.NET Core request pipeline with
/// OpenTelemetry instrumentation, to verify that the route recorded on the request activity is
/// resolved to the version the caller used.
/// </summary>
[IntegrationTest]
public class ApiVersionRouteProcessorIntegrationTests(ApiVersionRouteProcessorIntegrationTests.TelemetryHostFixture fixture) : IClassFixture<ApiVersionRouteProcessorIntegrationTests.TelemetryHostFixture>
{
    [Fact]
    public void VersionedEndpoint_RecordsRouteWithResolvedVersion()
    {
        var request = fixture.RequestTo("/accessmanagement/api/v2/enduser/clientdelegations/my/clients");

        request.GetTagItem("http.route").Should().Be("accessmanagement/api/v2/enduser/clientdelegations/my/clients");
        request.DisplayName.Should().Be("GET accessmanagement/api/v2/enduser/clientdelegations/my/clients");
    }

    [Fact]
    public void VersionedEndpointWithRouteParameter_KeepsOtherParametersAsPlaceholders()
    {
        var request = fixture.RequestTo("/accessmanagement/api/v2/enduser/clientdelegations/e5b6d1ad-8c4d-4e4a-9f6c-6dbb9b1a8d10");

        request.GetTagItem("http.route").Should().Be("accessmanagement/api/v2/enduser/clientdelegations/{delegationId}");
    }

    [Fact]
    public void UnversionedEndpoint_RecordsRouteUnchanged()
    {
        var request = fixture.RequestTo("/accessmanagement/api/v1/enduser/clientdelegations/clients");

        request.GetTagItem("http.route").Should().Be("accessmanagement/api/v1/enduser/clientdelegations/clients");
    }

    /// <summary>
    /// Hosts the endpoints under test and calls them all, waiting for each request activity to be
    /// captured before moving on, so that every activity is available when the assertions run.
    /// </summary>
    public sealed class TelemetryHostFixture : IAsyncLifetime
    {
        private static readonly string[] Paths =
        [
            "/accessmanagement/api/v2/enduser/clientdelegations/my/clients",
            "/accessmanagement/api/v2/enduser/clientdelegations/e5b6d1ad-8c4d-4e4a-9f6c-6dbb9b1a8d10",
            "/accessmanagement/api/v1/enduser/clientdelegations/clients",
        ];

        private static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(30);

        private readonly ActivityCapture _capture = new(Paths);
        private readonly Dictionary<string, Activity> _requests = [];

        public async ValueTask InitializeAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddProcessor(sp => new ApiVersionRouteProcessor(sp.GetRequiredService<IHttpContextAccessor>()))
                .AddProcessor(_capture));

            await using var app = builder.Build();
            app.MapGet("accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/my/clients", () => Results.Ok());
            app.MapGet("accessmanagement/api/v{version:apiVersion}/enduser/clientdelegations/{delegationId}", (string delegationId) => Results.Ok());
            app.MapGet("accessmanagement/api/v1/enduser/clientdelegations/clients", () => Results.Ok());

            await app.StartAsync(TestContext.Current.CancellationToken);

            using (var client = app.GetTestClient())
            {
                foreach (var path in Paths)
                {
                    var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
                    response.EnsureSuccessStatusCode();

                    // The test server returns the response before the hosting pipeline stops the
                    // request activity, so the activity is only guaranteed to exist once captured.
                    _requests[path] = await _capture.ActivityFor(path)
                        .WaitAsync(CaptureTimeout, TestContext.Current.CancellationToken);
                }
            }

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Activity RequestTo(string path) => _requests.Should().ContainKey(path).WhoseValue;

        /// <summary>
        /// Captures the request activity for each path under test. ASP.NET Core instrumentation listens
        /// on a process-wide activity source, so requests made by other test classes running in parallel
        /// also reach this processor on their own threads; only the paths under test are kept, and each
        /// of them is published through a completion source rather than a shared mutable collection.
        /// </summary>
        private sealed class ActivityCapture : BaseProcessor<Activity>
        {
            private readonly Dictionary<string, TaskCompletionSource<Activity>> _byPath;

            public ActivityCapture(IEnumerable<string> paths) =>
                _byPath = paths.ToDictionary(
                    path => path,
                    _ => new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously),
                    StringComparer.Ordinal);

            public Task<Activity> ActivityFor(string path) => _byPath[path].Task;

            public override void OnEnd(Activity activity)
            {
                if (activity.Kind == ActivityKind.Server
                    && activity.GetTagItem("url.path") is string path
                    && _byPath.TryGetValue(path, out var captured))
                {
                    captured.TrySetResult(activity);
                }
            }
        }
    }
}

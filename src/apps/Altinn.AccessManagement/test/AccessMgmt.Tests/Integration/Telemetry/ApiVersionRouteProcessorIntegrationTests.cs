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
    /// Hosts the endpoints under test, calls them all and shuts the host down again, so that every
    /// request activity is completed and captured before any assertion runs.
    /// </summary>
    public sealed class TelemetryHostFixture : IAsyncLifetime
    {
        private static readonly string[] Paths =
        [
            "/accessmanagement/api/v2/enduser/clientdelegations/my/clients",
            "/accessmanagement/api/v2/enduser/clientdelegations/e5b6d1ad-8c4d-4e4a-9f6c-6dbb9b1a8d10",
            "/accessmanagement/api/v1/enduser/clientdelegations/clients",
        ];

        private readonly List<Activity> _exported = [];

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
                .AddProcessor(new CaptureProcessor(_exported)));

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
                }
            }

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Activity RequestTo(string path) =>
            _exported.Should().ContainSingle(a => a.Kind == ActivityKind.Server && Equals(a.GetTagItem("url.path"), path)).Subject;

        private sealed class CaptureProcessor(List<Activity> exported) : BaseProcessor<Activity>
        {
            public override void OnEnd(Activity activity) => exported.Add(activity);
        }
    }
}

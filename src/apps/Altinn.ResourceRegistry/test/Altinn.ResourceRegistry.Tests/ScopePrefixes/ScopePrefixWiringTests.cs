using System.Net;
using System.Net.Http;
using System.Text;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.ScopePrefixes;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Integration.Clients;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Tests.ScopePrefixes;

/// <summary>
/// Checks that the host wires the real client, its settings and the validator together. The other
/// scope prefix tests either build the client by hand or replace it, so a wrong configuration
/// section name or registration would otherwise go unnoticed.
/// </summary>
public class ScopePrefixWiringTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    // Set the same way values.yaml does: SamarbeidsportalenSettings__PrefixEndpoint
    private const string Endpoint = "https://prefix-api.example.test/api/v1/prefix/all";

    private readonly RecordingHandler _handler = new();

    protected override void ConfigureTestConfiguration(IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SamarbeidsportalenSettings:PrefixEndpoint"] = Endpoint,
        });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Replace only the network below the real typed client, so no request leaves the test
        services.AddHttpClient(nameof(ISamarbeidsportalenClient))
            .ConfigurePrimaryHttpMessageHandler(() => _handler);

        base.ConfigureTestServices(services);
    }

    [Fact]
    public void Settings_AreBoundFromConfiguration()
    {
        var settings = Services.GetRequiredService<IOptions<SamarbeidsportalenSettings>>().Value;

        Assert.Equal(Endpoint, settings.PrefixEndpoint);
    }

    [Fact]
    public void RealClientAndValidator_AreRegistered()
    {
        Assert.IsType<SamarbeidsportalenClient>(Services.GetRequiredService<ISamarbeidsportalenClient>());

        // singleton, so every request shares the cached prefix list
        var validator = Services.GetRequiredService<IScopePrefixValidator>();
        Assert.IsType<ScopePrefixValidator>(validator);
        Assert.Same(validator, Services.GetRequiredService<IScopePrefixValidator>());
    }

    [Fact]
    public async Task Validator_FetchesFromConfiguredEndpoint_ThroughRealClient()
    {
        var validator = Services.GetRequiredService<IScopePrefixValidator>();

        var unowned = await validator.GetUnownedScopes("974761076", ["skd:scope", "altinn:scope"]);

        Assert.Equal(["altinn:scope"], unowned);
        Assert.Equal(Endpoint, Assert.Single(_handler.RequestUris));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private const string Body = """
            [
              { "prefix": "skd", "owner_orgno": "974761076", "active": true },
              { "prefix": "altinn", "owner_orgno": "991825827", "active": true }
            ]
            """;

        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (RequestUris)
            {
                RequestUris.Add(request.RequestUri!.ToString());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json"),
            });
        }
    }
}

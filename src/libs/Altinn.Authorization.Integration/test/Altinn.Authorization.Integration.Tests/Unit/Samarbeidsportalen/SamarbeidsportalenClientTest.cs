#nullable enable

using System.Net;
using System.Text;
using Altinn.Authorization.Integration.Platform.Extensions;
using Altinn.Authorization.Integration.Platform.Samarbeidsportalen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Tests.Unit.Samarbeidsportalen;

/// <summary>
/// Unit tests for <see cref="SamarbeidsportalenClient"/>, using a fake HTTP handler, and for its registration.
/// </summary>
[UnitTest]
public class SamarbeidsportalenClientTest
{
    private const string Endpoint = "https://api.test.samarbeid.digdir.no/api/v1/prefix/all";

    [Fact]
    public async Task GetScopePrefixes_ParsesResponse_FromConfiguredEndpoint()
    {
        const string json = """
            [
              {
                "prefix": "altinn",
                "owner_orgno": "991825827",
                "organization_name": "DIGITALISERINGSDIREKTORATET",
                "created": "2019-05-29T13:49:43Z",
                "last_updated": "2019-05-29T13:49:43Z",
                "active": true
              },
              {
                "prefix": "skatteetaten",
                "owner_orgno": "974761076",
                "created": "2020-01-01T00:00:00Z",
                "last_updated": "2021-01-01T00:00:00Z",
                "active": false
              }
            ]
            """;

        var handler = new FakeHandler(HttpStatusCode.OK, json);
        var target = CreateClient(new Uri(Endpoint), handler);

        IReadOnlyList<MaskinportenScopePrefix> result = await target.GetScopePrefixes(TestContext.Current.CancellationToken);

        Assert.Equal(Endpoint, handler.Request?.RequestUri?.ToString());
        Assert.Null(handler.Request?.Headers.Authorization);
        Assert.Equal(2, result.Count);

        Assert.Equal("altinn", result[0].Prefix);
        Assert.Equal("991825827", result[0].OwnerOrgNo);
        Assert.Equal("DIGITALISERINGSDIREKTORATET", result[0].OrganizationName);
        Assert.Equal(new DateTimeOffset(2019, 5, 29, 13, 49, 43, TimeSpan.Zero), result[0].Created);
        Assert.True(result[0].Active);

        // organization_name is missing on some records
        Assert.Null(result[1].OrganizationName);
        Assert.False(result[1].Active);
    }

    [Fact]
    public async Task GetScopePrefixes_NonSuccessStatus_Throws()
    {
        var target = CreateClient(new Uri(Endpoint), new FakeHandler(HttpStatusCode.InternalServerError, "oops"));

        await Assert.ThrowsAsync<HttpRequestException>(() => target.GetScopePrefixes(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetScopePrefixes_NullBody_Throws()
    {
        var target = CreateClient(new Uri(Endpoint), new FakeHandler(HttpStatusCode.OK, "null"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetScopePrefixes(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetScopePrefixes_EndpointNotConfigured_Throws()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "[]");
        var target = CreateClient(null, handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetScopePrefixes(TestContext.Current.CancellationToken));
        Assert.Null(handler.Request);
    }

    [Fact]
    public void AddSamarbeidsportalen_RegistersClientAndValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSamarbeidsportalen(opts => opts.PrefixEndpoint = new Uri(Endpoint));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<SamarbeidsportalenClient>(provider.GetRequiredService<ISamarbeidsportalenClient>());
        Assert.IsType<ScopePrefixValidator>(provider.GetRequiredService<IScopePrefixValidator>());
        Assert.Equal(new Uri(Endpoint), provider.GetRequiredService<IOptions<SamarbeidsportalenOptions>>().Value.PrefixEndpoint);
    }

    [Fact]
    public void AddSamarbeidsportalen_StaleAgeLongerThanExpiredAge_FailsValidation()
    {
        var services = new ServiceCollection();
        services.AddSamarbeidsportalen(opts =>
        {
            opts.PrefixCacheStaleAge = TimeSpan.FromHours(2);
            opts.PrefixCacheExpiredAge = TimeSpan.FromHours(1);
        });

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SamarbeidsportalenOptions>>().Value);
    }

    private static SamarbeidsportalenClient CreateClient(Uri? endpoint, FakeHandler handler)
    {
        var options = Options.Create(new SamarbeidsportalenOptions { PrefixEndpoint = endpoint });
        return new SamarbeidsportalenClient(new FakeHttpClientFactory(handler), options);
    }

    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(SamarbeidsportalenClient.HttpClientName, name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }
}

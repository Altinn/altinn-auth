using System.Net;
using System.Net.Http;
using System.Text;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Integration.Clients;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Altinn.ResourceRegistry.Tests.ScopePrefixes;

public class SamarbeidsportalenClientTests
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

        HttpRequestMessage? request = null;
        var target = CreateClient(Endpoint, HttpStatusCode.OK, json, r => request = r);

        IReadOnlyList<MaskinportenScopePrefix> result = await target.GetScopePrefixes();

        Assert.Equal(Endpoint, request?.RequestUri?.ToString());
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
        var target = CreateClient(Endpoint, HttpStatusCode.InternalServerError, "oops");

        await Assert.ThrowsAsync<HttpRequestException>(() => target.GetScopePrefixes());
    }

    [Fact]
    public async Task GetScopePrefixes_NullBody_Throws()
    {
        var target = CreateClient(Endpoint, HttpStatusCode.OK, "null");

        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetScopePrefixes());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetScopePrefixes_EndpointNotConfigured_Throws(string? endpoint)
    {
        var target = CreateClient(endpoint, HttpStatusCode.OK, "[]");

        await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetScopePrefixes());
    }

    private static SamarbeidsportalenClient CreateClient(string? endpoint, HttpStatusCode status, string body, Action<HttpRequestMessage>? onRequest = null)
    {
        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, _) => onRequest?.Invoke(r))
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });

        var settings = Options.Create(new SamarbeidsportalenSettings { PrefixEndpoint = endpoint! });
        return new SamarbeidsportalenClient(new HttpClient(handler.Object), settings);
    }
}

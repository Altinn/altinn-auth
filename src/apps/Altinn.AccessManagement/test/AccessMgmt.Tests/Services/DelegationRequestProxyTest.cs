using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Integration.Configuration;
using Altinn.AccessManagement.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AccessMgmt.Tests.Services;

public class DelegationRequestProxyTest
{
    private static DelegationRequestProxy CreateSut(
        HttpStatusCode statusCode,
        string responseBody,
        out List<Uri> capturedUris)
    {
        var uris = new List<Uri>();
        capturedUris = uris;

        var handler = new FakeHttpMessageHandler(statusCode, responseBody, uris);
        var httpClient = new HttpClient(handler);

        var settings = Options.Create(new SblBridgeSettings
        {
            BaseApiUrl = "https://sbl-bridge.example.com/",
        });

        var logger = Mock.Of<ILogger<DelegationRequestProxy>>();

        return new DelegationRequestProxy(httpClient, settings, logger);
    }

    [Fact]
    public async Task GetDelegationRequestsAsync_OkResponse_ReturnsDeserializedRequests()
    {
        var expected = new List<DelegationRequest>
        {
            new() { Guid = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), CoveredBy = "12345678", OfferedBy = "98765432", CoveredByName = "A", OfferedByName = "B", RequestStatus = RestAuthorizationRequestStatus.Unopened },
        };
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var sut = CreateSut(HttpStatusCode.OK, json, out _);

        var result = await sut.GetDelegationRequestsAsync(
            "98765432",
            "ABC",
            1,
            RestAuthorizationRequestDirection.Both,
            [RestAuthorizationRequestStatus.Unopened],
            null);

        result.Should().HaveCount(1);
        result[0].Guid.Should().Be(expected[0].Guid);
        result[0].CoveredBy.Should().Be("12345678");
    }

    [Fact]
    public async Task GetDelegationRequestsAsync_NonOkResponse_ReturnsNull()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, string.Empty, out _);

        var result = await sut.GetDelegationRequestsAsync(
            "who",
            null,
            null,
            RestAuthorizationRequestDirection.Incoming,
            null,
            null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDelegationRequestsAsync_AllOptionalParams_IncludedInQueryString()
    {
        var json = JsonSerializer.Serialize(new List<DelegationRequest>(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var sut = CreateSut(HttpStatusCode.OK, json, out var capturedUris);

        await sut.GetDelegationRequestsAsync(
            "who123",
            "ServiceCode1",
            42,
            RestAuthorizationRequestDirection.Outgoing,
            [RestAuthorizationRequestStatus.Accepted, RestAuthorizationRequestStatus.Rejected],
            "token-abc");

        capturedUris.Should().HaveCount(1);
        var query = capturedUris[0].Query;
        query.Should().Contain("who=who123");
        query.Should().Contain("serviceCode=ServiceCode1");
        query.Should().Contain("serviceEditionCode=42");
        query.Should().Contain("direction=Outgoing");
        query.Should().Contain("status=Accepted");
        query.Should().Contain("status=Rejected");
        query.Should().Contain("continuation=token-abc");
    }

    [Fact]
    public async Task GetDelegationRequestsAsync_EmptyServiceCodeAndNullEditionCode_OmittedFromQuery()
    {
        var json = JsonSerializer.Serialize(new List<DelegationRequest>(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var sut = CreateSut(HttpStatusCode.OK, json, out var capturedUris);

        await sut.GetDelegationRequestsAsync(
            "who456",
            string.Empty,
            null,
            RestAuthorizationRequestDirection.Incoming,
            null,
            string.Empty);

        capturedUris.Should().HaveCount(1);
        var query = capturedUris[0].Query;
        query.Should().NotContain("serviceCode");
        query.Should().NotContain("serviceEditionCode");
        query.Should().NotContain("status");
        query.Should().NotContain("continuation");
        query.Should().Contain("who=who456");
        query.Should().Contain("direction=Incoming");
    }

    [Fact]
    public async Task GetDelegationRequestsAsync_MultipleItems_AllDeserialized()
    {
        var items = Enumerable.Range(1, 3).Select(i => new DelegationRequest
        {
            Guid = Guid.NewGuid(),
            CoveredBy = $"cov{i}",
            OfferedBy = $"off{i}",
            CoveredByName = $"CovName{i}",
            OfferedByName = $"OffName{i}",
            RequestStatus = RestAuthorizationRequestStatus.Created,
        }).ToList();

        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var sut = CreateSut(HttpStatusCode.OK, json, out _);

        var result = await sut.GetDelegationRequestsAsync(
            "who",
            null,
            null,
            RestAuthorizationRequestDirection.None,
            null,
            null);

        result.Should().HaveCount(3);
        result.Select(r => r.CoveredBy).Should().BeEquivalentTo(["cov1", "cov2", "cov3"]);
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string body, List<Uri> capturedUris) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capturedUris.Add(request.RequestUri!);
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            };
            return Task.FromResult(response);
        }
    }
}

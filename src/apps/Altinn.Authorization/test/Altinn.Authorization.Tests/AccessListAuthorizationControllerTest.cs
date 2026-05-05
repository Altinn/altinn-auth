using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.IntegrationTests.Fixtures;
using Altinn.Platform.Authorization.IntegrationTests.Util;
using Altinn.Platform.Authorization.Models;
using Xunit;

namespace Altinn.Platform.Authorization.IntegrationTests;

public class AccessListAuthorizationControllerTest : IClassFixture<AuthorizationApiFixture>
{
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public AccessListAuthorizationControllerTest(AuthorizationApiFixture fixture)
    {
        _client = fixture.BuildClient();
    }

    /// <summary>
    /// Tests the scenario where the request does not have a valid platform access token.
    /// </summary>
    [Fact]
    public async Task AccessList_Authorization_Unauthorized_MissingPlatformAccessToken()
    {
        // Act
        HttpResponseMessage response = await _client.SendAsync(GetPostRequestMessage("Permit_WithoutActionFilter"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Tests the scenario where the subject organization has access to the resource 'ttd-accesslist-resource' through access list membership without any action filter.
    /// </summary>
    [Fact]
    public async Task AccessList_Authorization_Permit_WithoutActionFilter()
    {
        string testCase = "Permit_WithoutActionFilter";
        AccessListAuthorizationResponse expected = GetExpectedResponse("Permit_WithoutActionFilter");

        // Act
        HttpResponseMessage response = await _client.SendAsync(GetPostRequestMessage(testCase, PrincipalUtil.GetAccessToken("access-management", "platform")), TestContext.Current.CancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AccessListAuthorizationResponse actual = JsonSerializer.Deserialize<AccessListAuthorizationResponse>(responseContent, _serializerOptions);
        AssertionUtil.AssertEqual(expected, actual);
    }

    private static HttpRequestMessage GetPostRequestMessage(string testCase, string platformAccessToken = null)
    {
        string requestPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(AccessListAuthorizationControllerTest).Assembly.Location).LocalPath), "Data", "Json", "AccessListAuthorization");
        string requestText = File.ReadAllText(Path.Combine(requestPath, testCase + "_Request.json"));

        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "authorization/api/v1/accesslist/accessmanagement/authorization")
        {
            Content = new StringContent(requestText, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(platformAccessToken))
        {
            message.Headers.Add("PlatformAccessToken", platformAccessToken);
        }

        return message;
    }

    private static AccessListAuthorizationResponse GetExpectedResponse(string testCase)
    {
        string requestPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(AccessListAuthorizationControllerTest).Assembly.Location).LocalPath), "Data", "Json", "AccessListAuthorization");
        return (AccessListAuthorizationResponse)JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(requestPath, testCase + "_Response.json")), typeof(AccessListAuthorizationResponse), _serializerOptions);
    }
}

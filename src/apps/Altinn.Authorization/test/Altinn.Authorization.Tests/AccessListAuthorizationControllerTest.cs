using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.Authentication.Configuration;
using Altinn.Platform.Authorization.Controllers;
using Altinn.Platform.Authorization.IntegrationTests.MockServices;
using Altinn.Platform.Authorization.IntegrationTests.Util;
using Altinn.Platform.Authorization.IntegrationTests.Webfactory;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Events.Tests.Mocks;
using Altinn.ResourceRegistry.Tests.Mocks;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Altinn.Platform.Authorization.IntegrationTests;

public class AccessListAuthorizationControllerTest : IClassFixture<CustomWebApplicationFactory<AccessListAuthorizationController>>
{
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    private readonly CustomWebApplicationFactory<AccessListAuthorizationController> _factory;

    public AccessListAuthorizationControllerTest(CustomWebApplicationFactory<AccessListAuthorizationController> fixture)
    {
        _factory = fixture;
    }

    /// <summary>
    /// Tests the scenario where the request does not have a valid platform access token.
    /// </summary>
    [Fact]
    public async Task AccessList_Authorization_Unauthorized_MissingPlatformAccessToken()
    {
        // Act
        HttpResponseMessage response = await GetTestClient().SendAsync(GetPostRequestMessage("Permit_WithoutActionFilter"));

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
        HttpResponseMessage response = await GetTestClient().SendAsync(GetPostRequestMessage(testCase, PrincipalUtil.GetAccessToken("access-management", "platform")));
        string responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AccessListAuthorizationResponse actual = JsonSerializer.Deserialize<AccessListAuthorizationResponse>(responseContent, _serializerOptions);
        AssertionUtil.AssertEqual(expected, actual);
    }

    private HttpClient GetTestClient()
    {
        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IParties, PartiesMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
                services.AddSingleton<IRegisterService, RegisterServiceMock>();
                services.AddSingleton<IResourceRegistry, ResourceRegistryMock>();
                services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProviderMock>();
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        return client;
    }

    private static HttpRequestMessage GetPostRequestMessage(string testCase, string platformAccessToken = null)
    {
        string requestPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(AccessListAuthorizationControllerTest).Assembly.Location).LocalPath),  "Data", "Json", "AccessListAuthorization");
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

using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Controllers;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Common.AccessToken.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Migrated from WebApplicationFixture/AcceptanceCriteriaComposer to LegacyApiFixture
// as part of sub-step 16.4a (Phase 2.2). The single previous theory row had a
// no-op WithAssertResourceExistsInDb assertion, so the migration reduces to a
// direct HTTP call with a platform access token.
//
// Why LegacyApiFixture: the endpoint writes through the Dapper-backed
// ResourceMetadataRepo into accessmanagement.resource (Yuniql schema).
// ApiFixture alone only provisions the EF dbo schemas; LegacyApiFixture adds
// the production Yuniql migration pipeline on top.
namespace Altinn.AccessManagement.Tests.Controllers;

/// <summary>
/// <see cref="ResourceController"/>
/// </summary>
public class V2ResourceControllerTest : IClassFixture<LegacyApiFixture>
{
    private readonly HttpClient _client;

    public V2ResourceControllerTest(LegacyApiFixture fixture)
    {
        fixture.ConfigureServices(services =>
        {
            // PlatformAccessToken is signed by {issuer}-org.pem; default
            // PublicSigningKeyProviderMock only accepts the static test key.
            services.RemoveAll<IPublicSigningKeyProvider>();
            services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }

    private static readonly AccessManagementResource TestAltinnApp = new()
    {
        Created = DateTime.Today,
        Modified = DateTime.Today,
        ResourceId = 1,
        ResourceRegistryId = "test_id123",
        ResourceType = ResourceType.AltinnApp
    };

    /// <summary>
    /// <see cref="ResourceController.Post(List{AccessManagementResource})"/>
    /// </summary>
    [Fact(DisplayName = "POST_UpsertResource")]
    public async Task POST_UpsertResource()
    {
        // GIVEN a resource is upserted in resource registry
        // WHEN resource registry forwards the resource
        // THEN the resource should be stored
        using var request = new HttpRequestMessage(HttpMethod.Post, "accessmanagement/api/v1/internal/resources")
        {
            Content = JsonContent.Create<IEnumerable<AccessManagementResource>>([TestAltinnApp]),
        };
        request.Headers.Add("PlatformAccessToken", PrincipalUtil.GetAccessToken("platform", "resourceregistry"));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.True(
            response.IsSuccessStatusCode,
            $"expected successful status code, got {(int)response.StatusCode}: {response.StatusCode}");
    }
}

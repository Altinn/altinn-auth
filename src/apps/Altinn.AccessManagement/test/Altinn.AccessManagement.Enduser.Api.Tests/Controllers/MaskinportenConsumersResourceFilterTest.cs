using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Integration tests for the optional <c>consumer</c> and <c>resource</c> query filters on
/// <c>GET /accessmanagement/api/v1/enduser/maskinportenconsumers/resources</c>.
/// </summary>
/// <remarks>
/// Seed data (one supplier receiving MaskinportenSchema delegations from two consumers):
/// <list type="bullet">
/// <item>Supplier: Han Solo Enterprise. Consumers: Baker Johnsen (C1) and Svendsen Automobil (C2).</item>
/// <item>Resource A is delegated to the supplier by both C1 and C2; Resource B only by C1.</item>
/// </list>
/// Without filters the endpoint returns both resources (A with two permissions). The tests assert
/// that supplying <c>consumer</c> and/or <c>resource</c> narrows the result accordingly.
/// </remarks>
public class MaskinportenConsumersResourceFilterTest : IClassFixture<ApiFixture>
{
    private const string Route = "accessmanagement/api/v1/enduser/maskinportenconsumers";

    private const string ResourceARefId = "mp_test_3205_resource_a";
    private const string ResourceBRefId = "mp_test_3205_resource_b";

    private static readonly Guid Supplier = TestData.HanSoloEnterprise.Id;
    private static readonly Guid Consumer1 = TestData.BakerJohnsen.Id;
    private static readonly Guid Consumer2 = TestData.SvendsenAutomobil.Id;
    private const string Consumer1OrgNo = "913456785"; // Baker Johnsen
    private const string Consumer2OrgNo = "876543214"; // Svendsen Automobil

    public MaskinportenConsumersResourceFilterTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi);
        Fixture.EnsureSeedOnce<MaskinportenConsumersResourceFilterTest>(db =>
        {
            var maskinportenType = db.ResourceTypes.FirstOrDefault(t => t.Name == "MaskinportenSchema");
            if (maskinportenType is null)
            {
                maskinportenType = new ResourceType { Id = Guid.NewGuid(), Name = "MaskinportenSchema" };
                db.ResourceTypes.Add(maskinportenType);
                db.SaveChanges();
            }

            var resourceA = new Resource
            {
                Name = "Maskinporten test resource A",
                Description = "Maskinporten test resource A",
                RefId = ResourceARefId,
                TypeId = maskinportenType.Id,
                ProviderId = ProviderConstants.Altinn3.Id,
            };
            var resourceB = new Resource
            {
                Name = "Maskinporten test resource B",
                Description = "Maskinporten test resource B",
                RefId = ResourceBRefId,
                TypeId = maskinportenType.Id,
                ProviderId = ProviderConstants.Altinn3.Id,
            };

            var supplierFromConsumer1 = new Assignment { FromId = Consumer1, ToId = Supplier, RoleId = RoleConstants.Supplier.Id };
            var supplierFromConsumer2 = new Assignment { FromId = Consumer2, ToId = Supplier, RoleId = RoleConstants.Supplier.Id };

            db.Resources.Add(resourceA);
            db.Resources.Add(resourceB);
            db.Assignments.Add(supplierFromConsumer1);
            db.Assignments.Add(supplierFromConsumer2);
            db.SaveChanges();

            // Consumer 1 delegates both resources; Consumer 2 delegates only resource A.
            db.AssignmentResources.Add(new AssignmentResource { AssignmentId = supplierFromConsumer1.Id, ResourceId = resourceA.Id });
            db.AssignmentResources.Add(new AssignmentResource { AssignmentId = supplierFromConsumer1.Id, ResourceId = resourceB.Id });
            db.AssignmentResources.Add(new AssignmentResource { AssignmentId = supplierFromConsumer2.Id, ResourceId = resourceA.Id });
            db.SaveChanges();
        });
    }

    public ApiFixture Fixture { get; }

    private HttpClient CreateClient()
    {
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, Supplier.ToString()));
            claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private async Task<List<ResourcePermissionDto>> GetResources(string query)
    {
        HttpResponseMessage response = await CreateClient().GetAsync($"{Route}/resources?{query}", TestContext.Current.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        List<ResourcePermissionDto> result = JsonSerializer.Deserialize<List<ResourcePermissionDto>>(
            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);

        // Only consider the resources seeded by this test class.
        return result.Where(r => r.Resource.RefId is ResourceARefId or ResourceBRefId).ToList();
    }

    [Fact]
    public async Task GetResources_NoConsumerOrResourceFilter_ReturnsAllDelegatedResources()
    {
        List<ResourcePermissionDto> result = await GetResources($"party={Supplier}");

        Assert.Equal(2, result.Count);

        ResourcePermissionDto resourceA = result.Single(r => r.Resource.RefId == ResourceARefId);
        Assert.Equal(2, resourceA.Permissions.Count());

        ResourcePermissionDto resourceB = result.Single(r => r.Resource.RefId == ResourceBRefId);
        Assert.Single(resourceB.Permissions);
    }

    [Fact]
    public async Task GetResources_ConsumerFilter_ReturnsOnlyThatConsumersDelegations()
    {
        List<ResourcePermissionDto> result = await GetResources($"party={Supplier}&consumer={Consumer2OrgNo}");

        // Consumer 2 only delegated resource A.
        ResourcePermissionDto resourceA = Assert.Single(result);
        Assert.Equal(ResourceARefId, resourceA.Resource.RefId);

        PermissionDto permission = Assert.Single(resourceA.Permissions);
        Assert.Equal(Consumer2, permission.From.Id);
    }

    [Fact]
    public async Task GetResources_ResourceFilter_ReturnsOnlyThatResource()
    {
        List<ResourcePermissionDto> result = await GetResources($"party={Supplier}&resource={ResourceBRefId}");

        ResourcePermissionDto resourceB = Assert.Single(result);
        Assert.Equal(ResourceBRefId, resourceB.Resource.RefId);
    }

    [Fact]
    public async Task GetResources_ConsumerAndResourceFilter_ReturnsOnlyTheMatchingDelegation()
    {
        List<ResourcePermissionDto> result = await GetResources($"party={Supplier}&consumer={Consumer1OrgNo}&resource={ResourceARefId}");

        ResourcePermissionDto resourceA = Assert.Single(result);
        Assert.Equal(ResourceARefId, resourceA.Resource.RefId);

        PermissionDto permission = Assert.Single(resourceA.Permissions);
        Assert.Equal(Consumer1, permission.From.Id);
    }
}

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// HTTP integration tests for the resource delegation endpoints on
/// <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.MaskinportenSuppliersController"/>,
/// exercising the full pipeline (routing, scope authorization, real services + database via
/// <see cref="ApiFixture"/>, resource registry and policy mocks).
///
/// The tests cover the Resource Registry delegable flag: a MaskinportenSchema resource with
/// delegable=false must not be delegated through this API, even though the dedicated maskinporten
/// API is otherwise allowed to delegate MaskinportenSchema resources. Mirrors the Bruno test
/// <c>Negative_POST_Delegate_non_Delegable_Maskinporten_resource_To_supplier_Org</c>.
///
/// Test data: the resource <c>non_delegable_maskinportenschema</c> is seeded in the database via
/// <see cref="TestDataSeeds"/> and defined with delegable=false in the resource registry mock data.
/// Its policy grants the same roles and packages as <c>nav_sykepenger_dialog</c>, so Malin Emilie
/// (managing director of Dumbo Adventures) has delegable rights on it; the delegable flag is the
/// only blocker.
/// </summary>
[IntegrationTest]
public class MaskinportenSuppliersControllerResourceDelegationTest : IClassFixture<ApiFixture>
{
    private const string Route = "accessmanagement/api/v1/enduser/maskinportensuppliers";
    private const string NonDelegableResource = "non_delegable_maskinportenschema";

    private static readonly Guid Consumer = TestData.DumboAdventures.Id;
    private static readonly string SupplierOrgNo = TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MaskinportenSuppliersControllerResourceDelegationTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi);
        Fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
        });
    }

    private ApiFixture Fixture { get; }

    /// <summary>
    /// Malin (MD of Dumbo Adventures) checks the non-delegable MaskinportenSchema resource.
    /// She has role and package access, but the resource is delegable=false in the Resource
    /// Registry, so every right must come back denied with reason ResourceNotDelegable.
    /// </summary>
    [Fact]
    public async Task DelegationCheck_WhenResourceNotDelegable_Returns200WithAllRightsDeniedAsResourceNotDelegable()
    {
        var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.GetAsync(
            $"{Route}/resources/delegationcheck?party={Consumer}&resource={NonDelegableResource}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<ResourceCheckDto>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Rights);
        Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
        Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
    }

    /// <summary>
    /// Delegating the non-delegable MaskinportenSchema resource to a supplier must fail with
    /// 400 "The resource is not available for delegation" instead of writing the delegation.
    /// </summary>
    [Fact]
    public async Task AddResource_WhenResourceNotDelegable_Returns400WithResourceNotDelegable()
    {
        var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_WRITE);

        var response = await client.PostAsync(
            $"{Route}/resources?party={Consumer}&supplier={SupplierOrgNo}&resource={NonDelegableResource}", null, TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Body: {body}");

        using var problem = JsonDocument.Parse(body);
        Assert.Equal("The resource is not available for delegation", problem.RootElement.GetProperty("detail").GetString());
    }

    private HttpClient CreateClient(Guid partyUuid, params string[] scopes)
    {
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
            claims.Add(new Claim("scope", string.Join(" ", scopes)));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}

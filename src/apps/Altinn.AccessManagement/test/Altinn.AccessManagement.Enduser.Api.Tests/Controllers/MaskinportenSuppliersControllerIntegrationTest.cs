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
/// HTTP integration tests for <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.MaskinportenSuppliersController"/>,
/// exercising the full pipeline (routing, scope authorization, model binding, real service + database via
/// <see cref="ApiFixture"/>). These mirror the Bruno collection
/// <c>test/EnduserAPI/Maskinporten/Suppliers_API</c> for the supplier-management endpoints.
///
/// Note on authorization: each endpoint requires a scope policy AND an EndUserResourceAccessRequirement
/// (the "API-administrator for this party" check). <see cref="ApiFixture"/> registers a permissive PDP
/// (PermitPdpMock), so the resource-access policy always passes here; the access-based 403 (e.g. a
/// tilgangsstyrer) is covered by EndUserResourceAccessHandlerTest. The scope policy is real, so the
/// scope-based 403 is asserted below.
///
/// Reuses the pre-seeded <see cref="TestEntities"/> organizations and seeds only the supplier
/// assignments needed by the list/remove cases, using distinct pairs so the tests stay order-independent.
/// </summary>
public class MaskinportenSuppliersControllerIntegrationTest : IClassFixture<ApiFixture>
{
    private const string Route = "accessmanagement/api/v1/enduser/maskinportensuppliers";

    // Add / self-delegation / scope pair (no pre-seeded connection).
    private static readonly Guid AddConsumer = TestEntities.OrganizationOrsta.Id;
    private static readonly string AddConsumerOrgNo = TestEntities.OrganizationOrsta.Entity.OrganizationIdentifier;
    private static readonly Guid AddSupplier = TestEntities.OrganizationNufExampleNUF.Id;
    private static readonly string AddSupplierOrgNo = TestEntities.OrganizationNufExampleNUF.Entity.OrganizationIdentifier;

    // List pair (pre-seeded connection).
    private static readonly Guid ListConsumer = TestEntities.MainUnitKarlstad.Id;
    private static readonly Guid ListSupplier = TestEntities.SubunitKarlstad.Id;
    private static readonly string ListSupplierOrgNo = TestEntities.SubunitKarlstad.Entity.OrganizationIdentifier;

    // Remove pair (pre-seeded connection).
    private static readonly Guid RemoveConsumer = TestEntities.OrganizationOkernBorettslag.Id;
    private static readonly Guid RemoveSupplier = TestEntities.OrganizationVerdiqAS.Id;
    private static readonly string RemoveSupplierOrgNo = TestEntities.OrganizationVerdiqAS.Entity.OrganizationIdentifier;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MaskinportenSuppliersControllerIntegrationTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi);
        Fixture.EnsureSeedOnce<MaskinportenSuppliersControllerIntegrationTest>(db =>
        {
            db.Assignments.AddRange(
                SupplierAssignment(ListConsumer, ListSupplier),
                SupplierAssignment(RemoveConsumer, RemoveSupplier));

            db.SaveChanges();
        });
    }

    private ApiFixture Fixture { get; }

    [Fact]
    public async Task AddSupplier_WithWriteScope_ReturnsOkWithSupplierAssignment()
    {
        var client = CreateClient(AddConsumer, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_WRITE);

        var response = await client.PostAsync(
            $"{Route}?party={AddConsumer}&supplier={AddSupplierOrgNo}", null, TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var dto = JsonSerializer.Deserialize<AssignmentDto>(body, JsonOpts);
        dto.Should().NotBeNull();
        dto.FromId.Should().Be(AddConsumer);
        dto.ToId.Should().Be(AddSupplier);
        dto.RoleId.Should().Be(RoleConstants.Supplier.Id);
    }

    [Fact]
    public async Task GetSuppliers_WithExistingConnection_ReturnsSupplierInList()
    {
        var client = CreateClient(ListConsumer, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.GetAsync($"{Route}?party={ListConsumer}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var connections = JsonSerializer.Deserialize<List<ConnectionDto>>(body, JsonOpts);
        connections.Should().NotBeNull();
        var supplier = connections.Should().ContainSingle(c => c.Party.OrganizationIdentifier == ListSupplierOrgNo).Subject;
        supplier.Party.Type.Should().Be("Organisasjon");
        supplier.Roles.Should().Contain(r => r.Code == "supplier" && r.Urn == "urn:altinn:role:supplier");
    }

    [Fact]
    public async Task RemoveSupplier_WithExistingConnection_ReturnsNoContentAndRemovesFromList()
    {
        var client = CreateClient(RemoveConsumer, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_WRITE);

        var delete = await client.DeleteAsync(
            $"{Route}?party={RemoveConsumer}&supplier={RemoveSupplierOrgNo}", TestContext.Current.CancellationToken);
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listClient = CreateClient(RemoveConsumer, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);
        var list = await listClient.GetAsync($"{Route}?party={RemoveConsumer}", TestContext.Current.CancellationToken);
        var body = await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var connections = JsonSerializer.Deserialize<List<ConnectionDto>>(body, JsonOpts);
        connections.Should().NotContain(c => c.Party.OrganizationIdentifier == RemoveSupplierOrgNo);
    }

    [Fact]
    public async Task AddSupplier_WithReadScope_ReturnsForbidden()
    {
        var client = CreateClient(AddConsumer, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.PostAsync(
            $"{Route}?party={AddConsumer}&supplier={AddSupplierOrgNo}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddSupplier_SelfDelegation_ReturnsBadRequest()
    {
        var client = CreateClient(AddConsumer, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_WRITE);

        // party and supplier resolve to the same organization.
        var response = await client.PostAsync(
            $"{Route}?party={AddConsumer}&supplier={AddConsumerOrgNo}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private static Assignment SupplierAssignment(Guid consumerId, Guid supplierId) => new()
    {
        FromId = consumerId,
        ToId = supplierId,
        RoleId = RoleConstants.Supplier.Id,
    };
}

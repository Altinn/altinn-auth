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

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// HTTP integration tests for <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.MaskinportenConsumersController"/>
/// (the supplier's view of the organizations that have made them a Maskinporten supplier), exercising the full
/// pipeline via <see cref="ApiFixture"/>. Mirrors the Bruno collection <c>test/EnduserAPI/Maskinporten/Consumers_API</c>
/// for the consumer-list and connection-removal endpoints. The <c>/resources</c> filter endpoint is already covered
/// by <see cref="MaskinportenConsumersResourceFilterTest"/>.
///
/// As in the suppliers integration tests, ApiFixture's permissive PDP means the access-based 403 is not reproduced
/// here (covered by EndUserResourceAccessHandlerTest); the scope-based 403 is asserted.
/// </summary>
[IntegrationTest]
public class MaskinportenConsumersControllerIntegrationTest : IClassFixture<ApiFixture>
{
    private const string Route = "accessmanagement/api/v1/enduser/maskinportenconsumers";

    // List pair: consumer Orsta has made NUF its supplier.
    private static readonly Guid ListConsumer = TestEntities.OrganizationOrsta.Id;
    private static readonly string ListConsumerOrgNo = TestEntities.OrganizationOrsta.Entity.OrganizationIdentifier;
    private static readonly Guid ListSupplier = TestEntities.OrganizationNufExampleNUF.Id;

    // Remove pair: consumer Okern has made Verdiq its supplier.
    private static readonly Guid RemoveConsumer = TestEntities.OrganizationOkernBorettslag.Id;
    private static readonly string RemoveConsumerOrgNo = TestEntities.OrganizationOkernBorettslag.Entity.OrganizationIdentifier;
    private static readonly Guid RemoveSupplier = TestEntities.OrganizationVerdiqAS.Id;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MaskinportenConsumersControllerIntegrationTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi);
        Fixture.EnsureSeedOnce<MaskinportenConsumersControllerIntegrationTest>(db =>
        {
            db.Assignments.AddRange(
                SupplierAssignment(ListConsumer, ListSupplier),
                SupplierAssignment(RemoveConsumer, RemoveSupplier));

            db.SaveChanges();
        });
    }

    private ApiFixture Fixture { get; }

    [Fact]
    public async Task GetConsumers_WithExistingConnection_ReturnsConsumerInList()
    {
        var client = CreateClient(ListSupplier, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ);

        var response = await client.GetAsync($"{Route}?party={ListSupplier}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var connections = JsonSerializer.Deserialize<List<ConnectionDto>>(body, JsonOpts);
        connections.Should().NotBeNull();
        var consumer = connections.FirstOrDefault(c => c.Party.OrganizationIdentifier == ListConsumerOrgNo);
        consumer.Should().NotBeNull();
        consumer.Party.Type.Should().Be("Organisasjon");
        consumer.Roles.Should().Contain(r => r.Code == "supplier" && r.Urn == "urn:altinn:role:supplier");
    }

    [Fact]
    public async Task RemoveConsumer_WithExistingConnection_ReturnsNoContentAndRemovesFromList()
    {
        var client = CreateClient(RemoveSupplier, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_WRITE);

        var delete = await client.DeleteAsync(
            $"{Route}?party={RemoveSupplier}&consumer={RemoveConsumerOrgNo}", TestContext.Current.CancellationToken);
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listClient = CreateClient(RemoveSupplier, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ);
        var list = await listClient.GetAsync($"{Route}?party={RemoveSupplier}", TestContext.Current.CancellationToken);
        var body = await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var connections = JsonSerializer.Deserialize<List<ConnectionDto>>(body, JsonOpts);
        connections.Should().NotContain(c => c.Party.OrganizationIdentifier == RemoveConsumerOrgNo);
    }

    [Fact]
    public async Task RemoveConsumer_WithReadScope_Returns403Forbidden()
    {
        var client = CreateClient(RemoveSupplier, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ);

        var response = await client.DeleteAsync(
            $"{Route}?party={RemoveSupplier}&consumer={RemoveConsumerOrgNo}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

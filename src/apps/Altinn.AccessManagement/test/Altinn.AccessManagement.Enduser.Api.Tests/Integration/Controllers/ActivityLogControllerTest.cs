using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// HTTP integration tests for the enduser activity log areas, pinning the authorization
/// boundary and validation that only exist at the HTTP level: the directional scope per
/// anchor side, the dual-anchor rejection, typeId values outside the area, and the party
/// anchor surviving an own-field filter lookup. Log content semantics are covered by the
/// service-level integration tests.
/// </summary>
/// <remarks>
/// Seed data (own parties from the shared catalog, own assignments):
/// - Verdiq AS -> Paula: Agent assignment (connections slice, Verdiq outgoing)
/// - Nordis AS -> Verdiq AS: Rightholder assignment (Verdiq incoming, pins the anchor)
/// - Nordis AS -> Ørsta: Supplier assignment (the Maskinporten schema slice)
/// </remarks>
[IntegrationTest]
public class ActivityLogControllerTest : IClassFixture<ApiFixture>
{
    private const string ConnectionsRoute = "accessmanagement/api/v2/enduser/connections/activitylog";

    private const string MaskinportenRoute = "accessmanagement/api/v2/enduser/maskinporten/activitylog";

    private static readonly Guid Verdiq = TestEntities.OrganizationVerdiqAS.Id;
    private static readonly Guid Nordis = TestEntities.OrganizationNordisAS.Id;
    private static readonly Guid Paula = TestEntities.PersonPaula.Id;
    private static readonly Guid Orsta = TestEntities.OrganizationOrsta.Id;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ActivityLogControllerTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserConnectionsActivityLogApi);
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenActivityLogApi);
        Fixture.EnsureSeedOnce<ActivityLogControllerTest>(db =>
        {
            db.Assignments.AddRange(
                new Assignment { FromId = Verdiq, ToId = Paula, RoleId = RoleConstants.Agent },
                new Assignment { FromId = Nordis, ToId = Verdiq, RoleId = RoleConstants.Rightholder },
                new Assignment { FromId = Nordis, ToId = Orsta, RoleId = RoleConstants.Supplier });

            db.SaveChanges();
        });
    }

    private ApiFixture Fixture { get; }

    [Fact]
    public async Task Connections_FromAnchor_WithToOthersScope_Returns200()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&from={Verdiq}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.ToId == Paula);
        Assert.DoesNotContain(result.Items, e => e.FromId == Nordis);
    }

    [Fact]
    public async Task Connections_FromAnchor_WithFromOthersScope_Returns403()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&from={Verdiq}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Connections_ToAnchor_WithFromOthersScope_Returns200()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&to={Verdiq}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.FromId == Nordis);
    }

    /// <summary>
    /// Anchoring both sides is rejected even when the caller holds both directional scopes:
    /// the scope policy passes on either rule, so the direction would otherwise be ambiguous.
    /// </summary>
    [Fact]
    public async Task Connections_DualAnchor_WithBothScopes_Returns400()
    {
        var client = CreateClient(
            Verdiq,
            AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ,
            AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&from={Verdiq}&to={Verdiq}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Connections_TypeIdOutsideArea_Returns400()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync(
            $"{ConnectionsRoute}?party={Verdiq}&from={Verdiq}&typeId={ActivityTypeConstants.RequestCreated.Id}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// A lookup on the to field ignores the caller's own to filter, but never the party
    /// anchor: Verdiq's outgoing counterparts appear, while Verdiq itself (the to side of the
    /// Nordis assignment) must not.
    /// </summary>
    [Fact]
    public async Task Connections_FilterToLookup_KeepsPartyAnchor()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync(
            $"{ConnectionsRoute}/filter/to?party={Verdiq}&from={Verdiq}&to={Paula}",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogFilterValueDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, v => v.Id == Paula);
        Assert.DoesNotContain(result.Items, v => v.Id == Verdiq);
    }

    [Fact]
    public async Task Maskinporten_FromAnchor_WithSuppliersScope_Returns200()
    {
        var client = CreateClient(Nordis, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.GetAsync($"{MaskinportenRoute}?party={Nordis}&from={Nordis}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.ToId == Orsta);
    }

    /// <summary>
    /// The received direction requires the consumer read scope, exactly like the neighboring
    /// consumers endpoints — the supplier scope alone must not unlock it.
    /// </summary>
    [Fact]
    public async Task Maskinporten_ToAnchor_WithSuppliersScope_Returns403()
    {
        var client = CreateClient(Orsta, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.GetAsync($"{MaskinportenRoute}?party={Orsta}&to={Orsta}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Maskinporten_ToAnchor_WithConsumersScope_Returns200()
    {
        var client = CreateClient(Orsta, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ);

        var response = await client.GetAsync($"{MaskinportenRoute}?party={Orsta}&to={Orsta}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.FromId == Nordis);
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

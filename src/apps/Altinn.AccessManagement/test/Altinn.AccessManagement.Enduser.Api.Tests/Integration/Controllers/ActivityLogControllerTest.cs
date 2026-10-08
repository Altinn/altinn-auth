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
/// boundary and validation that only exist at the HTTP level: the directional scope keyed on
/// the required direction parameter, missing/via direction, from/to as plain filters that may
/// equal the party, typeId values outside the area, and the party anchor surviving an
/// own-field filter lookup. Log content semantics are covered by the service-level
/// integration tests.
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
    public async Task Connections_DirectionFrom_WithToOthersScope_Returns200()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&direction=from", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.ToId == Paula);
        Assert.DoesNotContain(result.Items, e => e.FromId == Nordis);
    }

    [Fact]
    public async Task Connections_DirectionFrom_WithFromOthersScope_Returns403()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&direction=from", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Connections_DirectionTo_WithFromOthersScope_Returns200()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&direction=to", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.FromId == Nordis);
    }

    [Fact]
    public async Task Connections_DirectionTo_WithToOthersScope_Returns403()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&direction=to", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Missing direction fails the directional scope rules before validation, like the
    /// connections endpoints behave for an unanchored query.
    /// </summary>
    [Fact]
    public async Task Connections_MissingDirection_Returns403()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// from/to are plain filters and may equal the party: the party's own side is overwritten
    /// by the anchor, so filtering the counterpart side to the party itself is a valid
    /// (self-events) query, not an error.
    /// </summary>
    [Fact]
    public async Task Connections_DirectionFrom_WithToFilterEqualToParty_Returns200()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync($"{ConnectionsRoute}?party={Verdiq}&direction=from&to={Verdiq}", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, e => e.ToId == Paula);
    }

    /// <summary>
    /// The bound model must ignore prefixed keys (query.party=…): the authorization handlers
    /// read the raw party/direction keys, so binding from the prefixed ones would let a
    /// caller authorize one party and query another.
    /// </summary>
    [Fact]
    public async Task Connections_ConflictingPrefixedParameters_AreIgnored()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync(
            $"{ConnectionsRoute}?party={Verdiq}&direction=from&query.party={Nordis}&query.direction=from",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, e => e.FromId == Verdiq);
        Assert.DoesNotContain(result.Items, e => e.FromId == Nordis);
    }

    [Fact]
    public async Task Connections_TypeIdOutsideArea_Returns400()
    {
        var client = CreateClient(Verdiq, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

        var response = await client.GetAsync(
            $"{ConnectionsRoute}?party={Verdiq}&direction=from&typeId={ActivityTypeConstants.RequestCreated.Id}",
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
            $"{ConnectionsRoute}/filter/to?party={Verdiq}&direction=from&to={Paula}",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        var result = JsonSerializer.Deserialize<PaginatedResult<ActivityLogFilterValueDto>>(body, JsonOpts);
        Assert.NotNull(result);
        Assert.Contains(result.Items, v => v.Id == Paula);
        Assert.DoesNotContain(result.Items, v => v.Id == Verdiq);
    }

    [Fact]
    public async Task Maskinporten_DirectionFrom_WithSuppliersScope_Returns200()
    {
        var client = CreateClient(Nordis, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.GetAsync($"{MaskinportenRoute}?party={Nordis}&direction=from", TestContext.Current.CancellationToken);

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
    public async Task Maskinporten_DirectionTo_WithSuppliersScope_Returns403()
    {
        var client = CreateClient(Orsta, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ);

        var response = await client.GetAsync($"{MaskinportenRoute}?party={Orsta}&direction=to", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Maskinporten_DirectionTo_WithConsumersScope_Returns200()
    {
        var client = CreateClient(Orsta, AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ);

        var response = await client.GetAsync($"{MaskinportenRoute}?party={Orsta}&direction=to", TestContext.Current.CancellationToken);

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

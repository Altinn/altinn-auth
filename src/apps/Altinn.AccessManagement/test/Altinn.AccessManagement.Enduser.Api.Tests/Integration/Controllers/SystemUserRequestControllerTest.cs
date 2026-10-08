using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

public class SystemUserRequestControllerTest
{
    public const string Route = "accessmanagement/api/v1/systemuser/request";

    private const string KommOrgNo = "910000004";
    private const string FylkOrgNo = "920000002";
    private const string StatOrgNo = "930000000";

    private static void SeedConsumers(ApiFixture fixture)
    {
        fixture.EnsureSeedOnce<SystemUserRequestControllerTest>(db =>
        {
            db.Entities.AddRange(
                ConsumerOrg("5c0e8f0a-1d1f-4b8e-9a51-0b6a6f3f0a01", KommOrgNo, "Test Kommune", EntityVariantConstants.KOMM.Id),
                ConsumerOrg("5c0e8f0a-1d1f-4b8e-9a51-0b6a6f3f0a02", FylkOrgNo, "Test Fylkeskommune", EntityVariantConstants.FYLK.Id),
                ConsumerOrg("5c0e8f0a-1d1f-4b8e-9a51-0b6a6f3f0a03", StatOrgNo, "Test Direktorat", EntityVariantConstants.STAT.Id));
            db.SaveChanges();
        });
    }

    private static Entity ConsumerOrg(string id, string orgNo, string name, Guid variantId) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        OrganizationIdentifier = orgNo,
        RefId = orgNo,
        TypeId = EntityTypeConstants.Organization,
        VariantId = variantId,
    };

    /// <summary>
    /// Creates an HTTP client with a system user token (Maskinporten integration) carrying the
    /// dedicated system user requests write scope and an <c>authorization_details</c> claim
    /// identifying the system user.
    /// </summary>
    private static HttpClient CreateSystemUserClient(ApiFixture fixture, Guid systemUserId, string scope = AuthzConstants.SCOPE_ENDUSER_SYSTEMUSER_REQUESTS_WRITE, string consumerOrgNo = KommOrgNo)
    {
        var client = fixture.Server.CreateClient();
        var authorizationDetails = $$"""{"type":"urn:altinn:systemuser","systemuser_id":["{{systemUserId}}"]}""";
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("scope", scope));
            claims.Add(new Claim("authorization_details", authorizationDetails));
            if (consumerOrgNo is not null)
            {
                claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{consumerOrgNo}" })));
            }
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    #region POST /package — Create system user package request

    [IntegrationTest]
    public class CreatePackageRequest : IClassFixture<ApiFixture>
    {
        public CreatePackageRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableSystemUserRequests);
            SeedConsumers(fixture);
        }

        public ApiFixture Fixture { get; }

        [Theory]
        [InlineData(KommOrgNo)]
        [InlineData(FylkOrgNo)]
        [InlineData(StatOrgNo)]
        public async Task SystemUserWithScope_CanCreatePackageRequest_ReturnsPending(string consumerOrgNo)
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, consumerOrgNo: consumerOrgNo);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={TestData.BakerJohnsen.Entity.OrganizationIdentifier}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var obj = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Pending, obj.Status);
            Assert.Equal(TestEntities.SystemUserStandard.Id, obj.From.Id);
        }

        [Fact]
        public async Task ConsumerNotPublicSector_IsRejected_ReturnsForbidden()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, consumerOrgNo: TestData.DumboAdventures.Entity.OrganizationIdentifier);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={TestData.BakerJohnsen.Entity.OrganizationIdentifier}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MissingConsumerClaim_IsRejected_ReturnsForbidden()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, consumerOrgNo: null);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={TestData.BakerJohnsen.Entity.OrganizationIdentifier}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("12345678")]
        [InlineData("913456786")]
        [InlineData("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d")]
        public async Task InvalidOrganizationNumber_ReturnsBadRequest(string organization)
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={organization}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UnknownOrganization_ReturnsBadRequest()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization=940000009&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task NonSystemUserSender_IsRejected_ReturnsForbidden()
        {
            // Token carries the scope, but the authorization_details id resolves to a
            // non-system-user entity, so the request must not be created on its behalf.
            var client = CreateSystemUserClient(Fixture, TestData.BakerJohnsen.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={TestData.DumboAdventures.Entity.OrganizationIdentifier}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region GET /sent and PUT /sent/withdraw

    [IntegrationTest]
    public class SentRequests : IClassFixture<ApiFixture>
    {
        public SentRequests(ApiFixture fixture)
        {
            Fixture = fixture;
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableSystemUserRequests);
            SeedConsumers(fixture);
        }

        public ApiFixture Fixture { get; }

        private async Task<RequestDto> CreateRequest(HttpClient client, string organization)
        {
            var response = await client.PostAsync(
                $"{Route}/package?organization={organization}&package={PackageConstants.Agriculture.Entity.Urn}",
                null,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
        }

        private static async Task<List<Guid>> GetSentIds(HttpClient client, string query)
        {
            var response = await client.GetAsync($"{Route}/sent{query}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return doc.RootElement.GetProperty("data").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();
        }

        [Fact]
        public async Task SystemUser_CanListAndWithdrawOwnRequest()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserClient.Id);
            var created = await CreateRequest(client, TestData.SvendsenAutomobil.Entity.OrganizationIdentifier);

            var pending = await GetSentIds(client, $"?organization={TestData.SvendsenAutomobil.Entity.OrganizationIdentifier}&status=Pending");
            Assert.Contains(created.Id, pending);

            var otherOrg = await GetSentIds(client, $"?organization={TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier}");
            Assert.DoesNotContain(created.Id, otherOrg);

            var withdraw = await client.PutAsync($"{Route}/sent/withdraw?id={created.Id}", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, withdraw.StatusCode);
            var withdrawn = await withdraw.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Withdrawn, withdrawn.Status);

            var stillPending = await GetSentIds(client, "?status=Pending");
            Assert.DoesNotContain(created.Id, stillPending);
        }

        [Fact]
        public async Task SystemUser_CannotWithdrawAnotherSystemUsersRequest()
        {
            var owner = CreateSystemUserClient(Fixture, TestEntities.SystemUserClient.Id);
            var created = await CreateRequest(owner, TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier);

            var other = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);
            var otherSent = await GetSentIds(other, string.Empty);
            Assert.DoesNotContain(created.Id, otherSent);

            var withdraw = await other.PutAsync($"{Route}/sent/withdraw?id={created.Id}", null, TestContext.Current.CancellationToken);
            Assert.False(withdraw.IsSuccessStatusCode);
        }

        [Fact]
        public async Task GetSent_InvalidOrganizationNumber_ReturnsBadRequest()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);

            var response = await client.GetAsync($"{Route}/sent?organization=12345678", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSent_ConsumerNotPublicSector_ReturnsForbidden()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, consumerOrgNo: TestData.DumboAdventures.Entity.OrganizationIdentifier);

            var response = await client.GetAsync($"{Route}/sent", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Withdraw_ConsumerNotPublicSector_ReturnsForbidden()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, consumerOrgNo: TestData.DumboAdventures.Entity.OrganizationIdentifier);

            var response = await client.PutAsync($"{Route}/sent/withdraw?id={Guid.NewGuid()}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetSent_WithoutScope_ReturnsForbidden()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, AuthzConstants.SCOPE_ENDUSER_REQUESTS_WRITE);

            var response = await client.GetAsync($"{Route}/sent", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region POST /package — Authorization

    [IntegrationTest]
    public class CreatePackageRequestForbidden : IClassFixture<ApiFixture>
    {
        public CreatePackageRequestForbidden(ApiFixture fixture)
        {
            Fixture = fixture;
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableSystemUserRequests);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task SystemUserWithoutScope_GetsForbidden()
        {
            // System user token carrying an unrelated scope must not pass the scope policy.
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id, AuthzConstants.SCOPE_ENDUSER_REQUESTS_WRITE);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={TestData.BakerJohnsen.Entity.OrganizationIdentifier}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region POST /package — Feature gate

    [IntegrationTest]
    public class CreatePackageRequestFeatureDisabled : IClassFixture<ApiFixture>
    {
        public CreatePackageRequestFeatureDisabled(ApiFixture fixture)
        {
            Fixture = fixture;
            fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.EnableSystemUserRequests);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task FeatureDisabled_ReturnsNotFound()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?organization={TestData.BakerJohnsen.Entity.OrganizationIdentifier}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    #endregion
}

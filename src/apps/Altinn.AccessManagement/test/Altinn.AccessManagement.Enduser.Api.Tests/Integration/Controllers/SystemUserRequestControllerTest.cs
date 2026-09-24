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
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

public class SystemUserRequestControllerTest
{
    public const string Route = "accessmanagement/api/v1/systemuser/request";

    /// <summary>
    /// Creates an HTTP client with a system user token (Maskinporten integration) carrying the
    /// dedicated system user requests write scope and an <c>authorization_details</c> claim
    /// identifying the system user.
    /// </summary>
    private static HttpClient CreateSystemUserClient(ApiFixture fixture, Guid systemUserId, string scope = AuthzConstants.SCOPE_ENDUSER_SYSTEMUSER_REQUESTS_WRITE)
    {
        var client = fixture.Server.CreateClient();
        var authorizationDetails = $$"""{"type":"urn:altinn:systemuser","systemuser_id":["{{systemUserId}}"]}""";
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("scope", scope));
            claims.Add(new Claim("authorization_details", authorizationDetails));
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
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task SystemUserWithScope_CanCreatePackageRequest_ReturnsPending()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?to={TestData.BakerJohnsen.Id}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var obj = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Pending, obj.Status);
            Assert.Equal(TestEntities.SystemUserStandard.Id, obj.From.Id);
        }

        [Fact]
        public async Task SystemUserAsRecipient_IsHardBlocked_ReturnsForbidden()
        {
            var client = CreateSystemUserClient(Fixture, TestEntities.SystemUserStandard.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?to={TestEntities.SystemUserClient.Id}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

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
                $"{Route}/package?to={TestData.BakerJohnsen.Id}&package={packageUrn}",
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
                $"{Route}/package?to={TestData.BakerJohnsen.Id}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    #endregion
}

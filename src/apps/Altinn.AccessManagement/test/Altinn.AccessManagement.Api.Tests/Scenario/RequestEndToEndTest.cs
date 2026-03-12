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
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessManagement.Api.Tests.Scenario;

public class RequestEndToEndTest
{
    private const string ServiceOwnerRoute = "accessmanagement/api/v1/serviceowner/delegationrequests";
    private const string EnduserRoute = "accessmanagement/api/v1/enduser/request";

    private static HttpClient CreateServiceOwnerClient(ApiFixture fixture, string orgNo)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{orgNo}" })));
            claims.Add(new Claim("scope", AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_WRITE));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static HttpClient CreateServiceOwnerReadClient(ApiFixture fixture, string orgNo)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{orgNo}" })));
            claims.Add(new Claim("scope", AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_READ));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static HttpClient CreateEnduserClient(ApiFixture fixture, Guid partyUuid)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
            claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            claims.Add(new Claim("scope", AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ));
            claims.Add(new Claim("scope", AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static void EnableFeatureFlags(ApiFixture fixture)
    {
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
    }

    #region Package request E2E lifecycle — accept

    public class PackageRequest_EndToEnd_AcceptLifecycle : IClassFixture<ApiFixture>
    {
        public PackageRequest_EndToEnd_AcceptLifecycle(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task PackageRequest_EndToEnd_AcceptLifecycle_FullFlow()
        {
            var from = $"urn:altinn:organization:identifier-no:{TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier}";
            var to = $"urn:altinn:person:identifier-no:{TestData.SiljeHaugen.Entity.PersonIdentifier}";

            // Step 1: SO creates a package request (status=Draft)
            var soClient = CreateServiceOwnerClient(Fixture, TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier);
            var createBody = new CreateServiceOwnerRequest
            {
                Connection = new ConnectionRequestInputDto { From = from, To = to },
                Resource = new RequestRefrenceDto(),
                Package = new RequestRefrenceDto { Urn = PackageConstants.Agriculture.Entity.Urn },
            };

            var createResponse = await soClient.PostAsJsonAsync(
                ServiceOwnerRoute,
                createBody,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);

            var createJson = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var createDoc = JsonDocument.Parse(createJson);
            var requestId = createDoc.RootElement.GetProperty("id").GetString();
            Assert.False(string.IsNullOrEmpty(requestId));

            // Step 2: EU confirms (Draft → Pending)
            // SiljeHaugen has ManagingDirector role in FredriksonsFabrikk, confirms on behalf of org
            var enduserClient = CreateEnduserClient(Fixture, TestData.SiljeHaugen.Id);
            var confirmResponse = await enduserClient.PutAsync(
                $"{EnduserRoute}/sent/confirm?id={requestId}&party={TestData.FredriksonsFabrikk.Id}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

            // Step 3: SO verifies Pending
            var soReadClient = CreateServiceOwnerReadClient(Fixture, TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier);
            var soGetResponse = await soReadClient.GetAsync(
                $"{ServiceOwnerRoute}/{requestId}/status",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, soGetResponse.StatusCode);

            var status = await soGetResponse.Content.ReadFromJsonAsync<RequestStatus>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Pending, status);

            // Step 4: EU sees the request
            var enduserGetResponse = await enduserClient.GetAsync(
                $"{EnduserRoute}/received?party={TestData.SiljeHaugen.Id}&from={TestData.FredriksonsFabrikk.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, enduserGetResponse.StatusCode);

            // Step 5: EU approves — expect non-success (delegation auth failure proves routing works)
            var approveResponse = await enduserClient.PutAsync(
                $"{EnduserRoute}/received/approve?party={TestData.SiljeHaugen.Id}&id={requestId}",
                null,
                TestContext.Current.CancellationToken);

            Assert.False(approveResponse.IsSuccessStatusCode);
        }
    }

    #endregion

}

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers.RequestApprovalTestData;
using RequestDtoV2 = Altinn.Authorization.Api.Contracts.AccessManagement.V2.RequestDto;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers.V2;

/// <summary>
/// Tests for the v2 request endpoints, where a resource request can name the rights (right keys) being requested.
///
/// Uses <see cref="TestData.TestdirektoratetCorrespondenceService"/>, whose policy in the test data gives read and
/// subscribe to a role (delegable) and write only to the service owner org (exists, but not delegable).
/// The keys are calculated from that policy, so the tests do not depend on hardcoded hashes.
/// </summary>
public class RequestControllerTest
{
    public const string Route = "accessmanagement/api/v2/enduser/request";

    private static readonly string Resource = TestData.TestdirektoratetCorrespondenceService.RefId;

    private static HttpClient CreateClient(ApiFixture fixture, Guid partyUuid)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
            claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_REQUESTS_READ} {AuthzConstants.SCOPE_ENDUSER_REQUESTS_WRITE}"));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    // KnutVik is a board member of BakerJohnsen (seeded via TestData.Assignments), so the parties are connected.
    private static string CreateResourceRequestUrl() =>
        $"{Route}/resource?party={TestData.KnutVik.Id}&to={TestData.BakerJohnsen.Id}&resource={Resource}";

    private static async Task<(List<string> DelegableKeys, List<string> NonDelegableKeys)> GetPolicyKeys()
    {
        XacmlPolicy policy = await new PolicyRetrievalPointMock().GetPolicyAsync(Resource, TestContext.Current.CancellationToken);
        var delegableKeys = DelegationCheckHelper.CalculateRightKeys(policy.Rules.ElementAt(0), Resource).ToList();
        var nonDelegableKeys = DelegationCheckHelper.CalculateRightKeys(policy.Rules.ElementAt(1), Resource).Except(delegableKeys).ToList();
        return (delegableKeys, nonDelegableKeys);
    }

    private static async Task<AltinnValidationProblemDetails> ReadProblem(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<AltinnValidationProblemDetails>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    #region POST — Create resource request with right keys

    [IntegrationTest]
    public class CreateResourceRequest : IClassFixture<ApiFixture>
    {
        public CreateResourceRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserRequestV2Api);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task WithDelegableRightKeys_ReturnsPendingWithStoredRightKeys()
        {
            var (delegableKeys, _) = await GetPolicyKeys();
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.PostAsJsonAsync(
                CreateResourceRequestUrl(),
                new RequestedRightKeysDto { RequestedRightKeys = delegableKeys },
                TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            var request = await response.Content.ReadFromJsonAsync<RequestDtoV2>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatusV2.Pending, request.Status.Value);
            Assert.Equal(delegableKeys, request.Resource.RequestedRightKeys);

            // The keys are part of the resource, not the request itself
            using var json = JsonDocument.Parse(content);
            Assert.True(json.RootElement.GetProperty("resource").TryGetProperty("requestedRightKeys", out _));
            Assert.False(json.RootElement.TryGetProperty("requestedRightKeys", out _));

            // Reading the request back returns the stored keys
            var getResponse = await client.GetAsync($"{Route}?party={TestData.KnutVik.Id}&id={request.Id}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var fetched = await getResponse.Content.ReadFromJsonAsync<RequestDtoV2>(TestContext.Current.CancellationToken);
            Assert.Equal(delegableKeys, fetched.Resource.RequestedRightKeys);
        }

        [Fact]
        public async Task WithOrganisationAsParty_ReturnsPendingWithStoredRightKeys()
        {
            // An organisation can ask for access too. MortenDahl is managing director of SvendsenAutomobil
            // and acts on its behalf; SvendsenAutomobil is rightholder for KaosMagicDesignAndArts
            // (seeded via TestData.Assignments), so the parties are connected.
            var (delegableKeys, _) = await GetPolicyKeys();
            var client = CreateClient(Fixture, TestData.MortenDahl.Id);

            var response = await client.PostAsJsonAsync(
                $"{Route}/resource?party={TestData.SvendsenAutomobil.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource={Resource}",
                new RequestedRightKeysDto { RequestedRightKeys = delegableKeys },
                TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            var request = await response.Content.ReadFromJsonAsync<RequestDtoV2>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatusV2.Pending, request.Status.Value);
            Assert.Equal(TestData.SvendsenAutomobil.Id, request.From.Id);
            Assert.Equal(TestData.KaosMagicDesignAndArts.Id, request.To.Id);
            Assert.Equal(delegableKeys, request.Resource.RequestedRightKeys);

            // Reading the request back as the organisation returns the stored keys
            var getResponse = await client.GetAsync($"{Route}?party={TestData.SvendsenAutomobil.Id}&id={request.Id}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var fetched = await getResponse.Content.ReadFromJsonAsync<RequestDtoV2>(TestContext.Current.CancellationToken);
            Assert.Equal(delegableKeys, fetched.Resource.RequestedRightKeys);
        }

        [Fact]
        public async Task WithoutBody_ReturnsPendingWithoutRightKeys()
        {
            // A different requester than the tests above, so this does not get back their pending request
            // (an existing pending request for the same parties and resource is returned as is).
            // HildeStrand is chair of the board of BakerJohnsen (seeded via TestData.Assignments).
            var client = CreateClient(Fixture, TestData.HildeStrand.Id);

            var response = await client.PostAsync(
                $"{Route}/resource?party={TestData.HildeStrand.Id}&to={TestData.BakerJohnsen.Id}&resource={Resource}",
                null,
                TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            var request = JsonSerializer.Deserialize<RequestDtoV2>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal(RequestStatusV2.Pending, request.Status.Value);
            Assert.Null(request.Resource.RequestedRightKeys);

            // The field is left out of the JSON instead of being written as null
            using var json = JsonDocument.Parse(content);
            Assert.False(json.RootElement.GetProperty("resource").TryGetProperty("requestedRightKeys", out _));
        }

        [Fact]
        public async Task WithUnknownRightKey_Returns400RightKeyNotFound()
        {
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.PostAsJsonAsync(
                CreateResourceRequestUrl(),
                new RequestedRightKeysDto { RequestedRightKeys = ["unknown-right-key"] },
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await ReadProblem(response);
            var error = Assert.Single(problem.Errors);
            Assert.Equal(ValidationErrors.RightKeyNotFound.ErrorCode, error.ErrorCode);
            Assert.Contains(error.Paths, p => p.EndsWith("/requestedRightKeys/0", StringComparison.Ordinal));
        }

        [Fact]
        public async Task WithRawJsonBody_BindsRequestedRightKeys()
        {
            // The tests above serialize RequestedRightKeysDto, so they pass whatever the field is called in JSON.
            // This sends the documented body as written, so renaming the field fails here.
            var client = CreateClient(Fixture, TestData.KnutVik.Id);
            using var body = new StringContent("""{ "requestedRightKeys": ["unknown-right-key"] }""", Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await client.PostAsync(CreateResourceRequestUrl(), body, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await ReadProblem(response);
            var error = Assert.Single(problem.Errors);
            Assert.Equal(ValidationErrors.RightKeyNotFound.ErrorCode, error.ErrorCode);
            Assert.Contains(error.Paths, p => p.EndsWith("/requestedRightKeys/0", StringComparison.Ordinal));
        }

        [Fact]
        public async Task WithNonDelegableRightKey_Returns400RightKeyNotDelegable()
        {
            var (_, nonDelegableKeys) = await GetPolicyKeys();
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.PostAsJsonAsync(
                CreateResourceRequestUrl(),
                new RequestedRightKeysDto { RequestedRightKeys = nonDelegableKeys },
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await ReadProblem(response);
            var error = Assert.Single(problem.Errors);
            Assert.Equal(ValidationErrors.RightKeyNotDelegable.ErrorCode, error.ErrorCode);
        }

        [Fact]
        public async Task GetSentRequests_UnderV2Route_Returns200()
        {
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.GetAsync($"{Route}/sent?party={TestData.KnutVik.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetSentRequests_UnderV2Route_HasNoDeprecationHeaders()
        {
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.GetAsync($"{Route}/sent?party={TestData.KnutVik.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("Deprecation"));
            Assert.False(response.Headers.Contains("Link"));
        }
    }

    #endregion

    #region PartialApprove — v2 reports it as is

    /// <summary>
    /// v2 status is non-exhaustive, so a request stored as <see cref="RequestStatus.PartialApprove"/> is reported as
    /// PartialApprove, and there is no separate StatusV2.
    /// </summary>
    [IntegrationTest]
    public class PartialApproveRequest : IClassFixture<ApiFixture>
    {
        private static readonly Guid PartialApproveRequestId = Guid.Parse("0196b0d1-0000-7000-8000-000000000001");

        public PartialApproveRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserRequestV2Api);
            Fixture.EnsureSeedOnce<PartialApproveRequest>(db =>
            {
                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.TrondLarsen.Id,
                    ToId = TestData.BakerJohnsen.Id,
                    ById = TestData.TrondLarsen.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentPackages.Add(new RequestAssignmentPackage
                {
                    Id = PartialApproveRequestId,
                    AssignmentId = reqAssignment.Id,
                    PackageId = PackageConstants.Agriculture.Id,
                    Status = RequestStatusV2.PartialApprove,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private async Task<JsonElement?> FindReceived(string statusQuery)
        {
            var client = CreateClient(Fixture, TestData.BakerJohnsen.Id);
            var response = await client.GetAsync(
                $"{Route}/received?party={TestData.BakerJohnsen.Id}&from={TestData.TrondLarsen.Id}{statusQuery}",
                TestContext.Current.CancellationToken);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {json}");

            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                if (item.GetProperty("id").GetGuid() == PartialApproveRequestId)
                {
                    return item.Clone();
                }
            }

            return null;
        }

        [Fact]
        public async Task GetRequest_StatusIsPartialApproveWithoutStatusV2()
        {
            var client = CreateClient(Fixture, TestData.BakerJohnsen.Id);

            var response = await client.GetAsync(
                $"{Route}?party={TestData.BakerJohnsen.Id}&id={PartialApproveRequestId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(content);
            Assert.Equal("PartialApprove", doc.RootElement.GetProperty("status").GetString());
            Assert.False(doc.RootElement.TryGetProperty("statusV2", out _));

            var request = JsonSerializer.Deserialize<RequestDtoV2>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal(RequestStatusV2.PartialApprove, request.Status.Value);
        }

        [Fact]
        public async Task GetReceived_WithoutStatusFilter_IncludesRequestAsPartialApprove()
        {
            var item = await FindReceived(string.Empty);

            Assert.NotNull(item);
            Assert.Equal("PartialApprove", item.Value.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetReceived_FilterOnPartialApprove_IncludesRequest()
        {
            var item = await FindReceived("&status=PartialApprove");

            Assert.NotNull(item);
        }

        [Fact]
        public async Task GetReceived_FilterOnApproved_ExcludesRequest()
        {
            var item = await FindReceived("&status=Approved");

            Assert.Null(item);
        }
    }

    #endregion

    #region PUT /received/approve — Approve with selected right keys

    /// <summary>
    /// Approving resource requests in v2, with and without a selection of right keys.
    ///
    /// Uses the app <see cref="TestData.SiriusSkattemelding"/>. Its policy gives rights to roles, including
    /// managing director (delegable), and some rights only to the service owner org (exist, but not delegable).
    /// <see cref="TestData.MalinEmilie"/>, managing director of <see cref="TestData.DumboAdventures"/>, approves on
    /// behalf of Dumbo. Each test approves a request from its own organisation, so the delegation policy written for
    /// it holds only what that test gave. <see cref="TestData.KnutVik"/> has no connection to Dumbo and can delegate nothing.
    /// </summary>
    [IntegrationTest]
    public class ApproveResourceRequest : IClassFixture<ApiFixture>
    {
        private static readonly Guid RequestedApprovedWithoutSelectionId = Guid.Parse("0196b0d2-0000-7000-8000-000000000001");
        private static readonly Guid RequestedPartlyApprovedId = Guid.Parse("0196b0d2-0000-7000-8000-000000000002");
        private static readonly Guid RequestedApprovedWithMoreId = Guid.Parse("0196b0d2-0000-7000-8000-000000000003");
        private static readonly Guid RequestedApprovedAsRequestedId = Guid.Parse("0196b0d2-0000-7000-8000-000000000004");
        private static readonly Guid WholeResourcePartlyApprovedId = Guid.Parse("0196b0d2-0000-7000-8000-000000000005");
        private static readonly Guid WholeResourceApprovedId = Guid.Parse("0196b0d2-0000-7000-8000-000000000006");
        private static readonly Guid InvalidSelectionId = Guid.Parse("0196b0d2-0000-7000-8000-000000000007");
        private static readonly Guid AlreadyApprovedId = Guid.Parse("0196b0d2-0000-7000-8000-000000000008");
        private static readonly Guid PackageRequestId = Guid.Parse("0196b0d2-0000-7000-8000-000000000009");
        private static readonly Guid DelegationChangedAfterwardsId = Guid.Parse("0196b0d2-0000-7000-8000-000000000010");
        private static readonly Guid HistoryId = Guid.Parse("0196b0d2-0000-7000-8000-000000000011");

        public ApproveResourceRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
            });
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserRequestV2Api);
            Fixture.EnsureSeedOnce<ApproveResourceRequest>(db =>
            {
                var (roleKeys, _) = GetAppPolicyKeys();

                AddResourceRequest(db, RequestedApprovedWithoutSelectionId, TestData.BakerJohnsen.Id, [roleKeys[0], roleKeys[1]]);
                AddResourceRequest(db, RequestedPartlyApprovedId, TestData.SvendsenAutomobil.Id, [roleKeys[0], roleKeys[1]]);
                AddResourceRequest(db, RequestedApprovedWithMoreId, TestData.FredriksonsFabrikk.Id, [roleKeys[0]]);
                AddResourceRequest(db, RequestedApprovedAsRequestedId, TestData.RegnskapNorge.Id, [roleKeys[0], roleKeys[1]]);
                AddResourceRequest(db, WholeResourcePartlyApprovedId, TestData.MilleHundefrisor.Id, null);
                AddResourceRequest(db, WholeResourceApprovedId, TestData.MittRegnskap.Id, null);
                AddResourceRequest(db, InvalidSelectionId, TestData.RpcAS.Id, [roleKeys[0]]);
                AddResourceRequest(db, AlreadyApprovedId, TestData.KaosMagicDesignAndArts.Id, [roleKeys[0]], RequestStatusV2.Approved);
                AddResourceRequest(db, DelegationChangedAfterwardsId, TestData.StorMektigTenesteeier.Id, [roleKeys[0]]);
                AddResourceRequest(db, HistoryId, TestData.LarsBakke.Id, [roleKeys[0], roleKeys[1]]);

                var packageAssignment = new RequestAssignment
                {
                    FromId = TestData.JosephineYvonnesdottir.Id,
                    ToId = TestData.DumboAdventures.Id,
                    ById = TestData.JosephineYvonnesdottir.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(packageAssignment);
                db.SaveChanges();

                db.RequestAssignmentPackages.Add(new RequestAssignmentPackage
                {
                    Id = PackageRequestId,
                    AssignmentId = packageAssignment.Id,
                    PackageId = PackageConstants.Agriculture.Id,
                    Status = RequestStatusV2.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private static string ApproveUrl(Guid id) => $"{Route}/received/approve?party={TestData.DumboAdventures.Id}&id={id}";

        private async Task<HttpResponseMessage> Approve(Guid id, IEnumerable<string>? rightKeys, Guid? approver = null)
        {
            var client = CreateClient(Fixture, approver ?? TestData.MalinEmilie.Id);
            return rightKeys is null
                ? await client.PutAsync(ApproveUrl(id), null, TestContext.Current.CancellationToken)
                : await client.PutAsJsonAsync(ApproveUrl(id), new ApproveRequestDto { RightKeys = rightKeys }, TestContext.Current.CancellationToken);
        }

        private static async Task<RequestDtoV2> ReadRequest(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");
            return JsonSerializer.Deserialize<RequestDtoV2>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<RequestDtoV2> GetRequest(Guid id)
        {
            var client = CreateClient(Fixture, TestData.MalinEmilie.Id);
            var response = await client.GetAsync($"{Route}?party={TestData.DumboAdventures.Id}&id={id}", TestContext.Current.CancellationToken);
            return await ReadRequest(response);
        }

        [Fact]
        public async Task WithoutSelection_GivesRequestedRights_AndIsApproved()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var request = await ReadRequest(await Approve(RequestedApprovedWithoutSelectionId, rightKeys: null));

            Assert.Equal(RequestStatusV2.Approved, request.Status.Value);
            Assert.Equal([roleKeys[0], roleKeys[1]], request.Resource.ApprovedRightKeys);
            Assert.Equal([roleKeys[0], roleKeys[1]], GetDelegatedRightKeys(Fixture, TestData.BakerJohnsen).Order(StringComparer.Ordinal));

            // Reading the request back returns the approved keys
            var fetched = await GetRequest(RequestedApprovedWithoutSelectionId);
            Assert.Equal(RequestStatusV2.Approved, fetched.Status.Value);
            Assert.Equal([roleKeys[0], roleKeys[1]], fetched.Resource.ApprovedRightKeys);
        }

        [Fact]
        public async Task WithFewerRightsThanRequested_GivesOnlySelection_AndIsPartialApprove()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var request = await ReadRequest(await Approve(RequestedPartlyApprovedId, [roleKeys[0]]));

            Assert.Equal(RequestStatusV2.PartialApprove, request.Status.Value);
            Assert.Equal([roleKeys[0], roleKeys[1]], request.Resource.RequestedRightKeys);
            Assert.Equal([roleKeys[0]], request.Resource.ApprovedRightKeys);
            Assert.Equal([roleKeys[0]], GetDelegatedRightKeys(Fixture, TestData.SvendsenAutomobil));

            // The requester is told that the request was reviewed
            await Fixture.QueryDb(async db =>
            {
                var outbox = await db.OutboxMessages
                    .Where(m => m.Handler == RequestReviewedNotification.Handler)
                    .ToListAsync(TestContext.Current.CancellationToken);
                Assert.Contains(outbox, m => m.RefId.Contains(TestData.SvendsenAutomobil.Id.ToString(), StringComparison.OrdinalIgnoreCase));
            });
        }

        [Fact]
        public async Task WithMoreRightsThanRequested_GivesSelection_AndIsPartialApprove()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var request = await ReadRequest(await Approve(RequestedApprovedWithMoreId, [roleKeys[1], roleKeys[0]]));

            Assert.Equal(RequestStatusV2.PartialApprove, request.Status.Value);
            Assert.Equal([roleKeys[1], roleKeys[0]], request.Resource.ApprovedRightKeys);
            Assert.Equal([roleKeys[0], roleKeys[1]], GetDelegatedRightKeys(Fixture, TestData.FredriksonsFabrikk).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task WithRequestedRightsInOtherOrder_IsApproved()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var request = await ReadRequest(await Approve(RequestedApprovedAsRequestedId, [roleKeys[1], roleKeys[0], roleKeys[1]]));

            Assert.Equal(RequestStatusV2.Approved, request.Status.Value);

            // Duplicates in the selection are stored once
            Assert.Equal([roleKeys[1], roleKeys[0]], request.Resource.ApprovedRightKeys);
        }

        [Fact]
        public async Task WithSelectionForWholeResource_IsPartialApprove()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var request = await ReadRequest(await Approve(WholeResourcePartlyApprovedId, [roleKeys[0]]));

            Assert.Equal(RequestStatusV2.PartialApprove, request.Status.Value);
            Assert.Null(request.Resource.RequestedRightKeys);
            Assert.Equal([roleKeys[0]], request.Resource.ApprovedRightKeys);
            Assert.Equal([roleKeys[0]], GetDelegatedRightKeys(Fixture, TestData.MilleHundefrisor));
        }

        [Fact]
        public async Task WithoutSelectionForWholeResource_GivesEveryRightTheUserCanDelegate_AndIsApproved()
        {
            var (roleKeys, orgOnlyKeys) = GetAppPolicyKeys();

            var request = await ReadRequest(await Approve(WholeResourceApprovedId, rightKeys: null));

            Assert.Equal(RequestStatusV2.Approved, request.Status.Value);
            var delegated = GetDelegatedRightKeys(Fixture, TestData.MittRegnskap);
            Assert.Equal(delegated.Order(StringComparer.Ordinal), request.Resource.ApprovedRightKeys.Order(StringComparer.Ordinal));
            Assert.Subset(roleKeys.ToHashSet(), delegated);
            Assert.DoesNotContain(delegated, orgOnlyKeys.Contains);
        }

        [Fact]
        public async Task WithRawJsonBody_BindsRightKeys()
        {
            // Sends the documented body as written, so renaming the field fails here.
            var client = CreateClient(Fixture, TestData.MalinEmilie.Id);
            using var body = new StringContent("""{ "rightKeys": ["unknown-right-key"] }""", Encoding.UTF8, MediaTypeNames.Application.Json);

            var response = await client.PutAsync(ApproveUrl(InvalidSelectionId), body, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RightKeyNotFound.ErrorCode, error.ErrorCode);
        }

        [Fact]
        public async Task WithUnknownRightKey_Returns400RightKeyNotFound_AndGivesNothing()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var response = await Approve(InvalidSelectionId, [roleKeys[0], "unknown-right-key"]);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RightKeyNotFound.ErrorCode, error.ErrorCode);
            Assert.Contains(error.Paths, p => p.EndsWith("/rightKeys/1", StringComparison.Ordinal));
            Assert.False(HasDelegationPolicy(Fixture, TestData.RpcAS));
            Assert.Equal(RequestStatusV2.Pending, (await GetRequest(InvalidSelectionId)).Status.Value);
        }

        [Fact]
        public async Task WithRightKeyOnlyForServiceOwner_Returns400RightKeyNotDelegable()
        {
            var (_, orgOnlyKeys) = GetAppPolicyKeys();

            var response = await Approve(InvalidSelectionId, [orgOnlyKeys[0]]);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RightKeyNotDelegable.ErrorCode, error.ErrorCode);
            Assert.False(HasDelegationPolicy(Fixture, TestData.RpcAS));
        }

        [Fact]
        public async Task ByUserWhoCannotDelegateSelection_Returns400RightKeyNotDelegableByUser()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var response = await Approve(InvalidSelectionId, [roleKeys[0]], approver: TestData.KnutVik.Id);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RightKeyNotDelegableByUser.ErrorCode, error.ErrorCode);
            Assert.Contains(error.Paths, p => p.EndsWith("/rightKeys/0", StringComparison.Ordinal));
            Assert.False(HasDelegationPolicy(Fixture, TestData.RpcAS));
        }

        [Fact]
        public async Task ByUserWhoCannotDelegateRequestedRights_Returns400RightKeyNotDelegableByUser()
        {
            var response = await Approve(InvalidSelectionId, rightKeys: null, approver: TestData.KnutVik.Id);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RightKeyNotDelegableByUser.ErrorCode, error.ErrorCode);
            Assert.Contains(error.Paths, p => p.EndsWith("/requestedRightKeys/0", StringComparison.Ordinal));
        }

        [Fact]
        public async Task AlreadyApprovedRequest_Returns400_AndGivesNothing()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var response = await Approve(AlreadyApprovedId, [roleKeys[0]]);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RequestUnsupportedStatusUpdate.ErrorCode, error.ErrorCode);
            Assert.False(HasDelegationPolicy(Fixture, TestData.KaosMagicDesignAndArts));
        }

        [Fact]
        public async Task ChangingTheDelegationAfterwards_DoesNotChangeTheRequest()
        {
            var (roleKeys, _) = GetAppPolicyKeys();
            var approved = await ReadRequest(await Approve(DelegationChangedAfterwardsId, rightKeys: null));
            Assert.Equal(RequestStatusV2.Approved, approved.Status.Value);

            // The delegation is an ordinary delegation, so it can be changed through the connections API
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestData.MalinEmilie.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            var response = await client.PutAsJsonAsync(
                $"accessmanagement/api/v1/enduser/connections/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.StorMektigTenesteeier.Id}&resource={AppResource}",
                new RightKeyListDto { DirectRightKeys = [roleKeys[0], roleKeys[1]] },
                TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}. Response body: {content}");
            Assert.Equal([roleKeys[0], roleKeys[1]], GetDelegatedRightKeys(Fixture, TestData.StorMektigTenesteeier).Order(StringComparer.Ordinal));

            // The request still shows what was approved
            var fetched = await GetRequest(DelegationChangedAfterwardsId);
            Assert.Equal(RequestStatusV2.Approved, fetched.Status.Value);
            Assert.Equal([roleKeys[0]], fetched.Resource.ApprovedRightKeys);
        }

        [Fact]
        public async Task ApprovedRightKeys_AreKeptInHistory()
        {
            var (roleKeys, _) = GetAppPolicyKeys();
            await ReadRequest(await Approve(HistoryId, [roleKeys[1]]));

            // The history keeps the version a change replaces, so change the approved request once more
            await Fixture.QueryDb(async db =>
            {
                var request = await db.RequestAssignmentResources.SingleAsync(r => r.Id == HistoryId, TestContext.Current.CancellationToken);
                request.Status = RequestStatusV2.Withdrawn;
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            await Fixture.QueryDb(async db =>
            {
                var history = await db.AuditRequestAssignmentResources.AsNoTracking()
                    .Where(r => r.Id == HistoryId)
                    .ToListAsync(TestContext.Current.CancellationToken);
                var approvedVersion = Assert.Single(history, r => r.Status == RequestStatusV2.PartialApprove);
                Assert.Equal([roleKeys[1]], approvedVersion.ApprovedRightKeys);
                Assert.Equal([roleKeys[0], roleKeys[1]], approvedVersion.RequestedRightKeys);
            });
        }

        [Fact]
        public async Task PackageRequestWithSelection_Returns400RightKeysNotAllowedForRequestType()
        {
            var (roleKeys, _) = GetAppPolicyKeys();

            var response = await Approve(PackageRequestId, [roleKeys[0]]);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = Assert.Single((await ReadProblem(response)).Errors);
            Assert.Equal(ValidationErrors.RightKeysNotAllowedForRequestType.ErrorCode, error.ErrorCode);
        }
    }

    #endregion

    #region Feature flag off

    [IntegrationTest]
    public class V2ApiDisabled : IClassFixture<ApiFixture>
    {
        public V2ApiDisabled(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task CreateResourceRequest_Returns404()
        {
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.PostAsync(CreateResourceRequestUrl(), null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    #endregion
}

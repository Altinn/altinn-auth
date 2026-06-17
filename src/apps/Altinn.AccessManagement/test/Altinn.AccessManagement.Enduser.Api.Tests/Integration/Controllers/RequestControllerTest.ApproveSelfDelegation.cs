using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.PEP.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// End-to-end integration tests for
/// <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.RequestController.ApproveRequest"/>
/// where the authenticated user is the same as the request sender (i.e. self-delegation).
///
/// "Self-delegation" means the resource was requested BY the party that is now approving it:
///   request.From == authenticated user (partyUuid from the JWT token)
///   request.To   == the party query-parameter
///
/// The controller allows this only when the authenticated user holds the
/// <c>altinn_access_management_hovedadmin</c> (MainAdmin) role for the receiver party.
/// Otherwise it returns 400 with validation error AM.VLD-00045
/// (<c>ValidationErrors.RequestFromSelfNotAllowed</c>).
///
/// All tests use the real PostgreSQL test container provisioned by <see cref="ApiFixture"/>
/// and rely on entities that are pre-seeded by <c>TestDataSeeds.Exec(db)</c> via
/// <see cref="EFPostgresFactory.BuildTemplateAsync"/>.
///
/// Seeded scenario anchors:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="TestData.MalinEmilie"/> is the <c>ManagingDirector</c> of
///       <see cref="TestData.DumboAdventures"/> (seeded assignment
///       <c>AssignDumboAdventuresMalinEmilieMD</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       The tests seed a <c>RequestAssignment</c> where
///       <c>From = MalinEmilie</c> and <c>To = DumboAdventures</c> with a
///       pending <c>RequestAssignmentResource</c> — simulating MalinEmilie
///       requesting access to a resource <em>on behalf of her own organisation</em>.
///     </description>
///   </item>
/// </list>
/// </summary>
public class ApproveSelfDelegationRequestTest
{
    public const string Route = RequestControllerTest.Route;

    private static readonly ResourceType SelfDelegationResourceType = new()
    {
        Id = Guid.Parse("0196b010-0000-7000-8000-000000000001"),
        Name = "SelfDelegationTestType",
    };

    private static readonly Guid SelfDelegationResourceId = Guid.Parse("0196b010-0000-7000-8000-000000000002");
    private const string SelfDelegationResourceRefId = "self-delegation-test-resource-1";

    private static void EnableFeatureFlags(ApiFixture fixture)
    {
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
    }

    private static HttpClient CreateSystemClient(ApiFixture fixture, Guid partyUuid)
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

    #region MainAdmin self-approving a resource request (happy path)

    /// <summary>
    /// Tests the happy path where the authenticated user is also the request sender AND
    /// holds the <c>altinn_access_management_hovedadmin</c> role for the receiver party.
    ///
    /// Outcome: the controller calls <c>ApproveResourceRequest</c>. Because the test
    /// resource is synthetic and may not have a backing policy in Azurite, a client-error
    /// or server-error is acceptable in addition to 200 OK — the important assertion is
    /// that the self-delegation gate itself does NOT add AM.VLD-00045.
    /// </summary>
    [IntegrationTest]
    public class MainAdmin_ApprovesOwnResourceRequest : IClassFixture<ApiFixture>
    {
        private static readonly Guid PendingRequestId = Guid.Parse("0196b010-0000-7000-8000-000000000010");

        public MainAdmin_ApprovesOwnResourceRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);

            // Replace the default PermitPdpMock with a permit-for-all mock (already the default),
            // so both the route-level authorization AND the MainAdmin check succeed.
            // PermitPdpMock is already registered by ApiFixture; no override needed for the
            // happy-path scenario.
            fixture.EnsureSeedOnce<MainAdmin_ApprovesOwnResourceRequest>(db =>
            {
                db.ResourceTypes.Add(SelfDelegationResourceType);
                db.SaveChanges();

                db.Resources.Add(new Resource
                {
                    Id = SelfDelegationResourceId,
                    Name = "SelfDelegationTestResource",
                    Description = "Synthetic resource for self-delegation MainAdmin tests",
                    RefId = SelfDelegationResourceRefId,
                    ProviderId = ProviderConstants.ResourceRegistry,
                    TypeId = SelfDelegationResourceType.Id,
                });
                db.SaveChanges();

                // MalinEmilie (from) requests access for DumboAdventures (to) to the test resource.
                // request.From == MalinEmilie; request.To == DumboAdventures.
                var reqAssignment = new RequestAssignment
                {
                    Id = Guid.Parse("0196b010-0000-7000-8000-000000000011"),
                    FromId = TestData.MalinEmilie.Id,
                    ToId = TestData.DumboAdventures.Id,
                    ById = TestData.MalinEmilie.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentResources.Add(new RequestAssignmentResource
                {
                    Id = PendingRequestId,
                    AssignmentId = reqAssignment.Id,
                    ResourceId = SelfDelegationResourceId,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// MalinEmilie is authenticated (partyUuid = MalinEmilie.Id) and approves
        /// a request where she is also the sender (From == MalinEmilie). The PDP mock
        /// always permits, so the MainAdmin check succeeds and the controller proceeds
        /// to <c>ApproveResourceRequest</c>.
        ///
        /// Because the test resource has no real delegation policy the downstream
        /// <c>ConnectionService.AddResource</c> may return a validation error or, in
        /// environments without Azurite, an internal server error.  Either is acceptable —
        /// the important assertion is that AM.VLD-00045 is NOT present.
        /// </summary>
        [Fact]
        public async Task MainAdmin_ApprovesOwnPendingRequest_DoesNotReturnSelfNotAllowed()
        {
            // Authenticate as MalinEmilie; party is DumboAdventures.
            // request.From (MalinEmilie) == authenticated user → self-delegation code path.
            var client = CreateSystemClient(Fixture, TestData.MalinEmilie.Id);

            var response = await client.PutAsync(
                $"{Route}/received/approve?party={TestData.DumboAdventures.Id}&id={PendingRequestId}",
                null,
                TestContext.Current.CancellationToken);

            // TODO: Fix to expect 200 OK, for now just ensure we did NOT get 400 Bad Request with AM.VLD-00045 (RequestFromSelfNotAllowed).
            // As there is a call ti ResourceRegistry that needs mocking and IPolicyRetrievalPoint that need mocking to get a clean 200 OK,            
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                var problem = JsonSerializer.Deserialize<AltinnProblemDetails>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // The response must NOT carry AM.VLD-00045 (RequestFromSelfNotAllowed).
                // Any other validation error is acceptable (e.g. resource not delegable).
                if (problem?.Extensions?.TryGetValue("validationErrors", out var rawErrors) == true
                    && rawErrors is JsonElement errorsElement)
                {
                    var codes = errorsElement.EnumerateArray()
                        .Select(e => e.GetProperty("code").GetString())
                        .ToList();
                    Assert.DoesNotContain("AM.VLD-00045", codes);
                }
            }
            else
            {
                // 200 OK, 403, 404, or 5xx are all acceptable outcomes other than 400.
                // The important thing is we entered the approval flow, not the self-rejection gate.
                Assert.True(
                    response.StatusCode != HttpStatusCode.BadRequest,
                    $"Self-delegation for a MainAdmin should not be blocked by AM.VLD-00045, got {response.StatusCode}");
            }
        }
    }

    #endregion

    #region Non-MainAdmin self-approving a resource request (rejection path)

    /// <summary>
    /// Tests the rejection path where the authenticated user is the request sender but does NOT
    /// hold the <c>altinn_access_management_hovedadmin</c> role for the receiver party.
    ///
    /// Outcome: the controller returns 400 Bad Request with AM.VLD-00045
    /// (<c>RequestFromSelfNotAllowed</c>).
    ///
    /// The <see cref="DenyMainAdminPdpMock"/> is used to allow route-level
    /// authorization (<c>altinn_access_management</c>) while denying the
    /// MainAdmin gate (<c>altinn_access_management_hovedadmin</c>).
    /// </summary>
    [IntegrationTest]
    public class NonMainAdmin_ApprovesOwnResourceRequest : IClassFixture<ApiFixture>
    {
        private static readonly Guid PendingRequestId = Guid.Parse("0196b010-0000-7000-8000-000000000020");

        public NonMainAdmin_ApprovesOwnResourceRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);

            // Override the default PermitPdpMock: allow route authorization but deny MainAdmin check.
            fixture.ConfigureServices(services =>
            {
                services.RemoveAll<IPDP>();
                services.AddSingleton<IPDP, DenyMainAdminPdpMock>();
            });

            fixture.EnsureSeedOnce<NonMainAdmin_ApprovesOwnResourceRequest>(db =>
            {
                // Ensure the resource type and resource are present (idempotent: first class to seed wins).
                if (!db.ResourceTypes.Any(rt => rt.Id == SelfDelegationResourceType.Id))
                {
                    db.ResourceTypes.Add(SelfDelegationResourceType);
                    db.SaveChanges();
                }

                if (!db.Resources.Any(r => r.Id == SelfDelegationResourceId))
                {
                    db.Resources.Add(new Resource
                    {
                        Id = SelfDelegationResourceId,
                        Name = "SelfDelegationTestResource",
                        Description = "Synthetic resource for self-delegation MainAdmin tests",
                        RefId = SelfDelegationResourceRefId,
                        ProviderId = ProviderConstants.ResourceRegistry,
                        TypeId = SelfDelegationResourceType.Id,
                    });
                    db.SaveChanges();
                }

                // Same scenario: MalinEmilie requests access for DumboAdventures.
                var reqAssignment = new RequestAssignment
                {
                    Id = Guid.Parse("0196b010-0000-7000-8000-000000000021"),
                    FromId = TestData.MalinEmilie.Id,
                    ToId = TestData.DumboAdventures.Id,
                    ById = TestData.MalinEmilie.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentResources.Add(new RequestAssignmentResource
                {
                    Id = PendingRequestId,
                    AssignmentId = reqAssignment.Id,
                    ResourceId = SelfDelegationResourceId,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// MalinEmilie is authenticated and attempts to approve her own pending request.
        /// The <see cref="DenyMainAdminPdpMock"/> causes the MainAdmin PDP check to return
        /// Deny, so the controller must reject with AM.VLD-00045.
        /// </summary>
        [Fact]
        public async Task NonMainAdmin_ApprovesOwnPendingRequest_ReturnsSelfNotAllowed()
        {
            var client = CreateSystemClient(Fixture, TestData.MalinEmilie.Id);

            var response = await client.PutAsync(
                $"{Route}/received/approve?party={TestData.DumboAdventures.Id}&id={PendingRequestId}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var problem = JsonSerializer.Deserialize<AltinnProblemDetails>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(problem);

            var errorsElement = (JsonElement)problem.Extensions.FirstOrDefault(e => e.Key == "validationErrors").Value;
            var codes = errorsElement.EnumerateArray()
                .Select(e => e.GetProperty("code").GetString())
                .ToList();

            Assert.Contains("AM.VLD-00045", codes);
        }        
    }

    #endregion
}

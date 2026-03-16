using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public class RequestControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/request";

    private static HttpClient CreateClient(ApiFixture fixture, Guid partyUuid)
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
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
    }

    #region POST — Create package request

    public class CreatePackageRequest : IClassFixture<ApiFixture>
    {
        public CreatePackageRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task PersonWithRoleInOrg_CanCreatePackageRequest_ReturnsPending()
        {
            // LarsBakke er daglig leder i BakerJohnsen (seeded via TestData.Assignments)
            var client = CreateClient(Fixture, TestData.LarsBakke.Id);
            var package = PackageConstants.Agriculture.Entity.Id;

            var response = await client.PostAsync(
                $"{Route}/package?party={TestData.LarsBakke.Id}&to={TestData.BakerJohnsen.Id}&packageId={package}", 
                null, 
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal((int)RequestStatus.Pending, root.GetProperty("status").GetInt32());
            Assert.Equal(TestData.BakerJohnsen.Id.ToString(), root.GetProperty("from").GetProperty("id").GetString());
            Assert.Equal(TestData.LarsBakke.Id.ToString(), root.GetProperty("to").GetProperty("id").GetString());
        }
    }

    #endregion

    #region POST — Create resource request

    public class CreateResourceRequest : IClassFixture<ApiFixture>
    {
        private static readonly ResourceType TestResourceType = new()
        {
            Id = Guid.Parse("0196b001-0000-7000-8000-000000000001"),
            Name = "CreateResourceTestType",
        };

        private static readonly Guid TestResourceId = Guid.Parse("0196b001-0000-7000-8000-000000000002");
        private const string TestResourceRefId = "create-resource-test-1";

        public CreateResourceRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                db.ResourceTypes.Add(TestResourceType);
                db.SaveChanges();
                db.Resources.Add(new Resource
                {
                    Id = TestResourceId,
                    Name = "CreateResourceTest",
                    Description = "Test resource for create resource request",
                    RefId = TestResourceRefId,
                    ProviderId = ProviderConstants.ResourceRegistry,
                    TypeId = TestResourceType.Id,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task PersonWithRoleInOrg_CanCreateResourceRequest_ReturnsPending()
        {
            // KnutVik er styremedlem i BakerJohnsen (seeded via TestData.Assignments)
            var client = CreateClient(Fixture, TestData.KnutVik.Id);

            var response = await client.PostAsync(
                $"{Route}/resource?party={TestData.KnutVik.Id}&to={TestData.BakerJohnsen.Id}&resource={TestResourceRefId}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal((int)RequestStatus.Pending, root.GetProperty("status").GetInt32());
        }
    }

    #endregion

    #region POST — Create request forbidden (no connection)

    public class CreateRequestForbidden : IClassFixture<ApiFixture>
    {
        public CreateRequestForbidden(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task PersonWithNoConnection_GetsNonSuccessResponse()
        {
            // BjornMoe er daglig leder i RegnskapNorge, men har ingen rolle i BakerJohnsen
            var client = CreateClient(Fixture, TestData.VegardSolberg.Id);
            var packageUrn = PackageConstants.Agriculture.Entity.Urn;

            var response = await client.PostAsync(
                $"{Route}/package?party={TestData.BjornMoe.Id}&to={TestData.BakerJohnsen.Id}&package={packageUrn}",
                null,
                TestContext.Current.CancellationToken);

            Assert.False(response.IsSuccessStatusCode, "Person without connection should not be able to create request");
        }

        [Fact]
        public async Task PersonWithKeyConnection_GetsSuccessResponse()
        {
            // BjornMoe er daglig leder i RegnskapNorge, og har da nøkkel rolle til BakerJohnsen
            var client = CreateClient(Fixture, TestData.BjornMoe.Id);
            var package = PackageConstants.Agriculture.Entity.Id;

            var response = await client.PostAsync(
                $"{Route}/package?party={TestData.BjornMoe.Id}&to={TestData.BakerJohnsen.Id}&packageId={package}",
                null,
                TestContext.Current.CancellationToken);

            Assert.True(response.IsSuccessStatusCode, "Person without keyrole connection should be able to create request");
        }
    }

    #endregion

    #region GET /sent — Sender sees sent requests

    public class GetSentRequests : IClassFixture<ApiFixture>
    {
        private static readonly Guid PendingPackageRequestId = Guid.Parse("0196b002-0000-7000-8000-000000000001");

        public GetSentRequests(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.BakerJohnsen.Id,
                    ToId = TestData.LarsBakke.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentPackages.Add(new RequestAssignmentPackage
                {
                    Id = PendingPackageRequestId,
                    AssignmentId = reqAssignment.Id,
                    PackageId = PackageConstants.Agriculture.Id,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task Sender_GetSentRequests_ContainsSeededRequest()
        {
            var client = CreateClient(Fixture, TestData.BakerJohnsen.Id);

            var response = await client.GetAsync(
                $"{Route}/sent?party={TestData.BakerJohnsen.Id}&to={TestData.LarsBakke.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("data");

            var found = false;
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("id").GetString() == PendingPackageRequestId.ToString())
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found, "Sender should see the seeded request in sent list");
        }
    }

    #endregion

    #region GET /received — Receiver sees package requests

    public class GetReceivedPackageRequests : IClassFixture<ApiFixture>
    {
        private static readonly Guid PendingPackageRequestId = Guid.Parse("0196b003-0000-7000-8000-000000000001");

        public GetReceivedPackageRequests(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.BakerJohnsen.Id,
                    ToId = TestData.LarsBakke.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentPackages.Add(new RequestAssignmentPackage
                {
                    Id = PendingPackageRequestId,
                    AssignmentId = reqAssignment.Id,
                    PackageId = PackageConstants.Agriculture.Id,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task Receiver_GetReceivedRequests_SeesPackageRequest()
        {
            var client = CreateClient(Fixture, TestData.LarsBakke.Id);

            var response = await client.GetAsync(
                $"{Route}/received?party={TestData.LarsBakke.Id}&from={TestData.BakerJohnsen.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("data");

            JsonElement? match = null;
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("id").GetString() == PendingPackageRequestId.ToString())
                {
                    match = item;
                    break;
                }
            }

            Assert.True(match.HasValue, "Receiver should see the package request");
            Assert.Equal(TestData.BakerJohnsen.Id.ToString(), match.Value.GetProperty("from").GetProperty("id").GetString());
            Assert.Equal(TestData.LarsBakke.Id.ToString(), match.Value.GetProperty("to").GetProperty("id").GetString());
        }
    }

    #endregion

    #region GET /received — Receiver sees resource requests

    public class GetReceivedResourceRequests : IClassFixture<ApiFixture>
    {
        private static readonly ResourceType TestResourceType = new()
        {
            Id = Guid.Parse("0196b004-0000-7000-8000-000000000001"),
            Name = "ReceivedResourceTestType",
        };

        private static readonly Guid TestResourceId = Guid.Parse("0196b004-0000-7000-8000-000000000002");
        private static readonly Guid PendingResourceRequestId = Guid.Parse("0196b004-0000-7000-8000-000000000003");

        public GetReceivedResourceRequests(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                db.ResourceTypes.Add(TestResourceType);
                db.SaveChanges();
                db.Resources.Add(new Resource
                {
                    Id = TestResourceId,
                    Name = "ReceivedResourceTest",
                    Description = "Test resource for received resource request",
                    RefId = "received-resource-test-1",
                    ProviderId = ProviderConstants.ResourceRegistry,
                    TypeId = TestResourceType.Id,
                });
                db.SaveChanges();

                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.SvendsenAutomobil.Id,
                    ToId = TestData.MortenDahl.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentResources.Add(new RequestAssignmentResource
                {
                    Id = PendingResourceRequestId,
                    AssignmentId = reqAssignment.Id,
                    ResourceId = TestResourceId,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task Receiver_GetReceivedRequests_SeesResourceRequest()
        {
            var client = CreateClient(Fixture, TestData.MortenDahl.Id);

            var response = await client.GetAsync(
                $"{Route}/received?party={TestData.MortenDahl.Id}&from={TestData.SvendsenAutomobil.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("data");

            var found = false;
            foreach (var item in items.EnumerateArray())
            {
                if (item.GetProperty("id").GetString() == PendingResourceRequestId.ToString())
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found, "Receiver should see the resource request");
        }
    }

    #endregion

    #region PUT /received/reject — Reject resource request

    public class RejectResourceRequest : IClassFixture<ApiFixture>
    {
        private static readonly ResourceType TestResourceType = new()
        {
            Id = Guid.Parse("0196b006-0000-7000-8000-000000000001"),
            Name = "RejectResourceTestType",
        };

        private static readonly Guid TestResourceId = Guid.Parse("0196b006-0000-7000-8000-000000000002");
        private static readonly Guid PendingResourceRequestId = Guid.Parse("0196b006-0000-7000-8000-000000000003");

        public RejectResourceRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                db.ResourceTypes.Add(TestResourceType);
                db.SaveChanges();
                db.Resources.Add(new Resource
                {
                    Id = TestResourceId,
                    Name = "RejectResourceTest",
                    Description = "Test resource for reject resource request",
                    RefId = "reject-resource-test-1",
                    ProviderId = ProviderConstants.ResourceRegistry,
                    TypeId = TestResourceType.Id,
                });
                db.SaveChanges();

                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.SvendsenAutomobil.Id,
                    ToId = TestData.MortenDahl.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentResources.Add(new RequestAssignmentResource
                {
                    Id = PendingResourceRequestId,
                    AssignmentId = reqAssignment.Id,
                    ResourceId = TestResourceId,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task Receiver_RejectsPendingResourceRequest_ReturnsRejected()
        {
            var client = CreateClient(Fixture, TestData.SvendsenAutomobil.Id);

            var response = await client.PutAsync(
                $"{Route}/received/reject?party={TestData.SvendsenAutomobil.Id}&id={PendingResourceRequestId}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Rejected, result.Status);
        }
    }

    #endregion

    #region PUT /sent/withdraw — Withdraw request

    public class WithdrawRequest : IClassFixture<ApiFixture>
    {
        private static readonly Guid PendingPackageRequestId = Guid.Parse("0196b007-0000-7000-8000-000000000001");

        public WithdrawRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.BakerJohnsen.Id,
                    ToId = TestData.HildeStrand.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentPackages.Add(new RequestAssignmentPackage
                {
                    Id = PendingPackageRequestId,
                    AssignmentId = reqAssignment.Id,
                    PackageId = PackageConstants.Agriculture.Id,
                    Status = RequestStatus.Pending,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task Sender_WithdrawsPendingRequest_ReturnsWithdrawn()
        {
            var client = CreateClient(Fixture, TestData.HildeStrand.Id);

            var response = await client.PutAsync(
                $"{Route}/sent/withdraw?party={TestData.HildeStrand.Id}&id={PendingPackageRequestId}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Withdrawn, result.Status);
        }
    }

    #endregion

    #region PUT /sent/confirm — Confirm draft request

    public class ConfirmDraftRequest : IClassFixture<ApiFixture>
    {
        private static readonly Guid DraftPackageRequestId = Guid.Parse("0196b008-0000-7000-8000-000000000001");

        public ConfirmDraftRequest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                var reqAssignment = new RequestAssignment
                {
                    FromId = TestData.BakerJohnsen.Id,
                    ToId = TestData.LarsBakke.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.RequestAssignments.Add(reqAssignment);
                db.SaveChanges();

                db.RequestAssignmentPackages.Add(new RequestAssignmentPackage
                {
                    Id = DraftPackageRequestId,
                    AssignmentId = reqAssignment.Id,
                    PackageId = PackageConstants.Agriculture.Id,
                    Status = RequestStatus.Draft,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task Sender_ConfirmsDraftRequest_ReturnsPending()
        {
            var client = CreateClient(Fixture, TestData.LarsBakke.Id);

            var response = await client.PutAsync(
                $"{Route}/sent/confirm?party={TestData.LarsBakke.Id}&id={DraftPackageRequestId}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.Equal(RequestStatus.Pending, result.Status);
        }
    }

    #endregion

}

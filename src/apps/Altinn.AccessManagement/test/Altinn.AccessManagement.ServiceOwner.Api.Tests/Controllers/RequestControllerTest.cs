using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.ServiceOwner.Api.Tests.Controllers;

public class RequestControllerTest
{
    public const string Route = "accessmanagement/api/v1/serviceowner/delegationrequests";

    private static HttpClient CreateClient(ApiFixture fixture, string orgNo)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{orgNo}" })));
            claims.Add(new Claim("scope", $"{AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_WRITE}"));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static HttpClient CreateReadOnlyClient(ApiFixture fixture, string orgNo)
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

    private static HttpClient CreateUnauthenticatedClient(ApiFixture fixture)
    {
        return fixture.Server.CreateClient();
    }

    private static void EnableFeatureFlags(ApiFixture fixture)
    {
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
    }

    #region GET _meta/urns/party

    public class GetValidUrnsTest : IClassFixture<ApiFixture>
    {
        public GetValidUrnsTest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task GetValidUrns_ReturnsOk_WithExpectedUrnPrefixes()
        {
            var client = Fixture.Server.CreateClient();

            var response = await client.GetAsync(
                $"{Route}/_meta/urns/party",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var urns = doc.RootElement.EnumerateArray().Select(e => e.GetString()).ToList();

            Assert.Contains("urn:altinn:person:identifier-no", urns);
            Assert.Contains("urn:altinn:organization:identifier-no", urns);
        }

        [Fact]
        public async Task GetValidUrns_ReturnsExactlyFourUrns()
        {
            var client = Fixture.Server.CreateClient();

            var response = await client.GetAsync(
                $"{Route}/_meta/urns/party",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(json);
            var count = doc.RootElement.GetArrayLength();

            Assert.Equal(2, count);
        }
    }

    #endregion

    #region POST (create request with resource)

    public class CreateResourceRequestTest : IClassFixture<ApiFixture>
    {
        private static readonly ResourceType TestResourceType = new()
        {
            Id = Guid.Parse("0196c001-0000-7000-8000-000000000001"),
            Name = "ServiceOwnerTestResourceType",
        };

        public CreateResourceRequestTest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                db.ResourceTypes.Add(TestResourceType);
                db.SaveChanges();

                db.Resources.Add(new Resource
                {
                    Id = Guid.CreateVersion7(),
                    Name = "TestResource",
                    Description = "Test resource for ServiceOwner API tests",
                    RefId = "test-resource-so-1",
                    ProviderId = TestData.ServiceOwnerNAV,
                    TypeId = TestResourceType.Id,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task CreateRequest_WithResource_Returns202Accepted()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);
            var to = $"urn:altinn:organization:identifier-no:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var from = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}";

            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto { ReferenceId = "test-resource-so-1" },
                Package = new RequestReferenceDto(),
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            await Fixture.QueryDb(static async db =>
            {
                var outbox = await db.OutboxMessages.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(outbox);
                Assert.NotNull(outbox.Data);

                var data = JsonSerializer.Deserialize<ResourceRequestPendingNotificationMessage>(outbox.Data);
                Assert.NotNull(data);

                Assert.Equal(TestData.BakerJohnsen, data.RecipientId);
                Assert.Equal(TestData.LarsBakke, data.RequesterId);
                Assert.NotEmpty(data.ResourceIds);
                Assert.Empty(data.PackageIds);
                Assert.Equal(1, data.Updated);
            });

            var obj = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(obj);
        }

        [Fact]
        public async Task CreateRequest_WithInvalidFromUrn_Returns400()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var body = new CreateServiceOwnerRequest
            {
                From = "urn:invalid:prefix:12345",
                To = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}",
                Resource = new RequestReferenceDto { ReferenceId = "test-resource-so-1" },
                Package = new RequestReferenceDto(),
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRequest_WithInvalidResourceProvider_Returns400()
        {
            var client = CreateClient(Fixture, TestData.BakerJohnsen.Entity.OrganizationIdentifier);
            var from = $"urn:altinn:organization:identifier-no:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var to = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}";

            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto { ReferenceId = "test-resource-so-1" },
                Package = new RequestReferenceDto(),
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRequest_WithEmptyResourceId_Returns400()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);
            var from = $"urn:altinn:organization:identifier-no:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var to = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}";

            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto { ReferenceId = string.Empty },
                Package = new RequestReferenceDto(),
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    #endregion

    #region POST (create request with package)

    public class CreatePackageRequestTest : IClassFixture<ApiFixture>
    {
        public CreatePackageRequestTest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task CreateRequest_WithPackage_Returns202Accepted()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);
            var from = $"urn:altinn:organization:identifier-no:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var to = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}";

            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto(),
                Package = new RequestReferenceDto { ReferenceId = PackageConstants.Agriculture.Entity.Urn },
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            var obj = await response.Content.ReadFromJsonAsync<RequestDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(obj);
        }

        [Fact]
        public async Task CreateRequest_WithInvalidFromUrn_Returns400()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var body = new CreateServiceOwnerRequest
            {
                From = "urn:invalid:prefix:12345",
                To = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}",
                Resource = new RequestReferenceDto(),
                Package = new RequestReferenceDto { Id = PackageConstants.Agriculture.Id },
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRequest_WithEmptyPackageUrn_Returns400()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);
            var from = $"urn:altinn:organization:identifier-no:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var to = $"urn:altinn:person:identifier-no:{TestData.LarsBakke.Entity.PersonIdentifier}";

            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto(),
                Package = new RequestReferenceDto { ReferenceId = string.Empty },
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    #endregion

    #region End-to-end: GetValidUrns then Create Request

    public class GetValidUrnsThenCreateRequestTest : IClassFixture<ApiFixture>
    {
        private static readonly ResourceType TestResourceType = new()
        {
            Id = Guid.Parse("0196c002-0000-7000-8000-000000000001"),
            Name = "E2ETestResourceType",
        };

        public GetValidUrnsThenCreateRequestTest(ApiFixture fixture)
        {
            Fixture = fixture;
            EnableFeatureFlags(fixture);
            fixture.EnsureSeedOnce(db =>
            {
                db.ResourceTypes.Add(TestResourceType);
                db.SaveChanges();

                db.Resources.Add(new Resource
                {
                    Id = Guid.CreateVersion7(),
                    Name = "E2ETestResource",
                    Description = "End-to-end test resource",
                    RefId = "test-resource-e2e-1",
                    ProviderId = TestData.ServiceOwnerNAV,
                    TypeId = TestResourceType.Id,
                });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task GetValidUrns_ThenCreateResourceRequest_EndToEnd()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            // Step 1: Get valid URN prefixes
            var urnsResponse = await client.GetAsync(
                $"{Route}/_meta/urns/party",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, urnsResponse.StatusCode);

            var urnsJson = await urnsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var urnsDoc = JsonDocument.Parse(urnsJson);
            var urnPrefixes = urnsDoc.RootElement.EnumerateArray().Select(e => e.GetString()).ToList();

            var orgPrefix = urnPrefixes.First(u => u.Contains("organization"));
            var personPrefix = urnPrefixes.First(u => u.Contains("person"));

            // Step 2: Build URNs using the returned prefixes
            var from = $"{orgPrefix}:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var to = $"{personPrefix}:{TestData.LarsBakke.Entity.PersonIdentifier}";

            // Step 3: Create request with resource
            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto { ReferenceId = "test-resource-e2e-1" },
                Package = new RequestReferenceDto()
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        [Fact]
        public async Task GetValidUrns_ThenCreatePackageRequest_EndToEnd()
        {
            var client = CreateClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            // Step 1: Get valid URN prefixes
            var urnsResponse = await client.GetAsync(
                $"{Route}/_meta/urns/party",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, urnsResponse.StatusCode);

            var urnsJson = await urnsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var urnsDoc = JsonDocument.Parse(urnsJson);
            var urnPrefixes = urnsDoc.RootElement.EnumerateArray().Select(e => e.GetString()).ToList();

            var orgPrefix = urnPrefixes.First(u => u.Contains("organization"));
            var personPrefix = urnPrefixes.First(u => u.Contains("person"));

            // Step 2: Build URNs using the returned prefixes
            var from = $"{orgPrefix}:{TestData.BakerJohnsen.Entity.OrganizationIdentifier}";
            var to = $"{personPrefix}:{TestData.LarsBakke.Entity.PersonIdentifier}";

            // Step 3: Create request with package
            var body = new CreateServiceOwnerRequest
            {
                From = from,
                To = to,
                Resource = new RequestReferenceDto(),
                Package = new RequestReferenceDto { ReferenceId = PackageConstants.Agriculture.Entity.Urn },
            };

            var response = await client.PostAsJsonAsync(
                Route,
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }
    }

    #endregion
}

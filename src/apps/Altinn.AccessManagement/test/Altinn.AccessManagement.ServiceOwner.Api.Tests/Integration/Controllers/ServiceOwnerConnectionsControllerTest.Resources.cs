using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Xml.Linq;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.Register;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.ServiceOwner.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests for the resource delegation endpoints in <see cref="ConnectionsController"/> in the ServiceOwner API.
/// </summary>
public partial class ServiceOwnerConnectionsControllerTest
{
    #region accessmanagement/api/v1/serviceowner/connections/resources

    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetResourceRights(string, CancellationToken)"/>,
    /// <see cref="ConnectionsController.AddResource(ServiceOwnerResourceDelegation, CancellationToken)"/> and
    /// <see cref="ConnectionsController.RevokeResource(ServiceOwnerResourceDelegation, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Provider "Skatteetaten" with the organization number of Stor og Mektig Tjenesteeier as RefId
    /// - Resource "Skattemelding" (app_skd_sirius-skattemelding-v1) moved to the Skatteetaten provider, so the
    ///   test service owner owns the resource
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for the available right keys of the resource
    /// - <see cref="PolicyRetrievalPointWithWrittenPoliciesMock"/> for XACML policy lookups, including delegation policies written during the test
    /// - <see cref="PolicyFactoryMock"/> captures written XACML policies
    /// </para>
    /// <para>
    /// Actors:
    /// - Stor og Mektig Tjenesteeier: the authenticated service owner, owner of the Skattemelding resource
    /// - Baker Johnsen: another organization, used as a service owner without ownership of the resource
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class AddRevokeResources : IClassFixture<ApiFixture>
    {
        private const string Resource = "app_skd_sirius-skattemelding-v1";

        private static readonly Guid SkatteetatenProviderId = Guid.Parse("0196b130-0000-7000-8000-000000000001");

        public AddRevokeResources(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableServiceOwnerResourceDelegation);
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointWithWrittenPoliciesMock>();
            });

            Fixture.EnsureSeedOnce<AddRevokeResources>(db =>
            {
                db.Providers.Add(new Provider()
                {
                    Id = SkatteetatenProviderId,
                    Name = "Skatteetaten",
                    Code = "skd",
                    RefId = TestData.StorMektigTenesteeier.Entity.OrganizationIdentifier,
                    TypeId = ProviderTypeConstants.ServiceOwner,
                });
                db.SaveChanges();

                Resource resource = db.Resources.Single(r => r.Id == TestData.SiriusSkattemelding.Id);
                resource.ProviderId = SkatteetatenProviderId;
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient(string orgNumber = null, string scope = AuthzConstants.SCOPE_SERVICEOWNER_RESOURCE_DELEGATION_WRITE)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.Org, "SKD"));
                claims.Add(new Claim("scope", scope));
                claims.Add(new Claim("consumer", GetConsumerClaimJson(orgNumber ?? TestData.StorMektigTenesteeier.Entity.OrganizationIdentifier)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        private static string GetConsumerClaimJson(string orgNumber)
        {
            return $$"""{ "authority":"iso6523-actorid-upis", "ID":"0192:{{orgNumber}}"}""";
        }

        private static ServiceOwnerResourceDelegation CreateRequest(ServiceOwnerConnectionPartyUrn from, ServiceOwnerConnectionPartyUrn to, IEnumerable<string> rightKeys = null)
        {
            return new ServiceOwnerResourceDelegation()
            {
                From = from,
                To = to,
                Resource = Resource,
                RightKeys = rightKeys is null ? null : new RightKeyListDto { DirectRightKeys = rightKeys },
            };
        }

        private static ServiceOwnerConnectionPartyUrn Person(ConstantDefinition<Entity> entity)
        {
            return ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(entity.Entity.PersonIdentifier));
        }

        private static ServiceOwnerConnectionPartyUrn Organization(ConstantDefinition<Entity> entity)
        {
            return ServiceOwnerConnectionPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(entity.Entity.OrganizationIdentifier));
        }

        /// <summary>
        /// Gets the available right keys for the resource through the rights endpoint.
        /// </summary>
        private async Task<List<string>> GetAvailableRightKeys()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/resources/rights?resource={Resource}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var rights = await response.Content.ReadFromJsonAsync<List<RightDto>>(TestContext.Current.CancellationToken);
            Assert.NotNull(rights);
            Assert.NotEmpty(rights);

            return rights.Select(r => r.Key).ToList();
        }

        /// <summary>
        /// Delegates all available right keys on the resource from one party to another as the test service owner.
        /// </summary>
        private async Task<ServiceOwnerResourceDelegation> AddResource(ServiceOwnerConnectionPartyUrn from, ServiceOwnerConnectionPartyUrn to)
        {
            var request = CreateRequest(from, to, await GetAvailableRightKeys());
            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            return request;
        }

        private async Task<AssignmentResource> GetAssignmentResource(Guid fromId, Guid toId)
        {
            AssignmentResource assignmentResource = null;
            await Fixture.QueryDb(async db =>
            {
                assignmentResource = await db.AssignmentResources
                    .Include(ar => ar.Assignment)
                    .Where(ar => ar.Assignment.FromId == fromId)
                    .Where(ar => ar.Assignment.ToId == toId)
                    .Where(ar => ar.Assignment.RoleId == RoleConstants.Rightholder)
                    .Where(ar => ar.ResourceId == TestData.SiriusSkattemelding.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
            });

            return assignmentResource;
        }

        private async Task<Assignment> GetRightholderAssignment(Guid fromId, Guid toId)
        {
            Assignment assignment = null;
            await Fixture.QueryDb(async db =>
            {
                assignment = await db.Assignments
                    .Where(a => a.FromId == fromId)
                    .Where(a => a.ToId == toId)
                    .Where(a => a.RoleId == RoleConstants.Rightholder)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
            });

            return assignment;
        }

        private static async Task AssertProblemCode(HttpResponseMessage response, string expectedCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
            Assert.NotNull(problemDetails);
            Assert.Equal(expectedCode, problemDetails.Extensions["code"].ToString());
        }

        /// <summary>
        /// Counts the rules in the delegation policy written to the given path. The XACML parser refuses a policy
        /// without rules, so the rules are counted from the XML directly.
        /// </summary>
        private int GetWrittenPolicyRuleCount(string policyPath)
        {
            var policyFactory = Fixture.Server.Services.GetRequiredService<IPolicyFactory>() as PolicyFactoryMock;
            Assert.NotNull(policyFactory);
            Assert.True(policyFactory.WrittenPolicies.TryGetValue(policyPath, out byte[] content), $"No policy written at {policyPath}");

            XDocument policy = XDocument.Load(new MemoryStream(content));
            return policy.Descendants().Count(element => element.Name.LocalName == "Rule");
        }

        /// <summary>
        /// The service owner owning the resource gets the right keys defined in the resource policy.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsResourceOwner_Returns200WithRightKeys()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/resources/rights?resource={Resource}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rights = await response.Content.ReadFromJsonAsync<List<RightDto>>(TestContext.Current.CancellationToken);
            Assert.NotNull(rights);
            Assert.NotEmpty(rights);
            Assert.All(rights, right => Assert.False(string.IsNullOrEmpty(right.Key)));
        }

        /// <summary>
        /// A service owner that does not own the resource is denied.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsOtherServiceOwner_Returns403ResourceDelegationNotAuthorized()
        {
            var client = CreateClient(TestData.BakerJohnsen.Entity.OrganizationIdentifier);

            var response = await client.GetAsync($"{Route}/resources/rights?resource={Resource}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertProblemCode(response, "AM-00048");
        }

        /// <summary>
        /// An unknown resource identifier gives an invalid resource problem.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_WithUnknownResource_Returns400InvalidResource()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/resources/rights?resource=nonexistent_resource", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertProblemCode(response, "AM-00027");
        }

        /// <summary>
        /// The service owner delegates the resource between two organizations. The rightholder assignment is created,
        /// the assignment resource is stored with the service owner as the one who changed it, and a delegation policy is written.
        /// </summary>
        [Fact]
        public async Task AddResource_BetweenOrganizations_Returns200AndCreatesAssignmentResource()
        {
            List<string> rightKeys = await GetAvailableRightKeys();
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), Organization(TestData.RegnskapNorge), rightKeys);

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            var result = await response.Content.ReadFromJsonAsync<AssignmentResourceDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.Equal(TestData.SiriusSkattemelding.Id, result.ResourceId);

            Assignment assignment = await GetRightholderAssignment(TestData.FredriksonsFabrikk.Id, TestData.RegnskapNorge.Id);
            Assert.NotNull(assignment);
            Assert.Equal(TestData.StorMektigTenesteeier.Id, assignment.Audit_ChangedBy);

            AssignmentResource assignmentResource = await GetAssignmentResource(TestData.FredriksonsFabrikk.Id, TestData.RegnskapNorge.Id);
            Assert.NotNull(assignmentResource);
            Assert.Equal(result.Id, assignmentResource.Id);
            Assert.Equal(assignment.Id, assignmentResource.AssignmentId);
            Assert.Equal(TestData.StorMektigTenesteeier.Id, assignmentResource.Audit_ChangedBy);

            Assert.Equal(rightKeys.Count, GetWrittenPolicyRuleCount(assignmentResource.PolicyPath));
        }

        /// <summary>
        /// The service owner delegates the resource between two persons.
        /// </summary>
        [Fact]
        public async Task AddResource_BetweenPersons_Returns200AndCreatesAssignmentResource()
        {
            var request = CreateRequest(Person(TestData.VegardSolberg), Person(TestData.IngerNygard), await GetAvailableRightKeys());

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            AssignmentResource assignmentResource = await GetAssignmentResource(TestData.VegardSolberg.Id, TestData.IngerNygard.Id);
            Assert.NotNull(assignmentResource);
        }

        /// <summary>
        /// Right keys must be given explicitly in the delegation body.
        /// </summary>
        [Fact]
        public async Task AddResource_WithoutRightKeys_Returns400MissingRightKey()
        {
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), Organization(TestData.SvendsenAutomobil));

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertProblemCode(response, "AM-00033");
            Assert.Null(await GetRightholderAssignment(TestData.FredriksonsFabrikk.Id, TestData.SvendsenAutomobil.Id));
        }

        /// <summary>
        /// A right key that is not in the resource policy is rejected before anything is written.
        /// </summary>
        [Fact]
        public async Task AddResource_WithUnknownRightKey_Returns400InvalidRightKey()
        {
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), Organization(TestData.SvendsenAutomobil), ["some-fake-right-key"]);

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertProblemCode(response, "AM-00036");
            Assert.Null(await GetRightholderAssignment(TestData.FredriksonsFabrikk.Id, TestData.SvendsenAutomobil.Id));
        }

        /// <summary>
        /// A service owner that does not own the resource cannot delegate it.
        /// </summary>
        [Fact]
        public async Task AddResource_AsOtherServiceOwner_Returns403ResourceDelegationNotAuthorized()
        {
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), Organization(TestData.SvendsenAutomobil), await GetAvailableRightKeys());

            var response = await CreateClient(TestData.BakerJohnsen.Entity.OrganizationIdentifier).PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertProblemCode(response, "AM-00048");
        }

        /// <summary>
        /// Parties must be given as a person identifier or an organization identifier of an existing party.
        /// </summary>
        [Fact]
        public async Task AddResource_WithUnknownParty_Returns400ConnectionEntitiesDoNotExist()
        {
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), ServiceOwnerConnectionPartyUrn.PartyUuid.Create(Guid.NewGuid()), await GetAvailableRightKeys());

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertProblemCode(response, "AM-00021");
        }

        [Fact]
        public async Task AddResource_WithoutAuthentication_Returns401()
        {
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), Organization(TestData.SvendsenAutomobil), ["read"]);

            var response = await Fixture.Server.CreateClient().PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// The package delegation scope does not give access to the resource delegation endpoints.
        /// </summary>
        [Fact]
        public async Task AddResource_WithPackageDelegationScope_Returns403()
        {
            var request = CreateRequest(Organization(TestData.FredriksonsFabrikk), Organization(TestData.SvendsenAutomobil), ["read"]);

            var response = await CreateClient(scope: AuthzConstants.SCOPE_SERVICEOWNER_PACKAGE_DELEGATION_WRITE).PostAsJsonAsync($"{Route}/resources", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Revoking the only access on an assignment created by the service owner removes the assignment resource,
        /// clears the delegation policy and removes the assignment.
        /// </summary>
        [Fact]
        public async Task RevokeResource_WhereAssignmentWasCreatedByServiceOwner_Returns204AndRemovesAssignment()
        {
            var request = await AddResource(Person(TestData.SiljeHaugen), Person(TestData.EinarBerg));
            AssignmentResource assignmentResource = await GetAssignmentResource(TestData.SiljeHaugen.Id, TestData.EinarBerg.Id);
            Assert.NotNull(assignmentResource);

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources/revoke", request, TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {content}");

            Assert.Null(await GetAssignmentResource(TestData.SiljeHaugen.Id, TestData.EinarBerg.Id));
            Assert.Null(await GetRightholderAssignment(TestData.SiljeHaugen.Id, TestData.EinarBerg.Id));

            Assert.Equal(0, GetWrittenPolicyRuleCount(assignmentResource.PolicyPath));
        }

        /// <summary>
        /// Revoking the resource keeps the assignment when the assignment still carries other access.
        /// </summary>
        [Fact]
        public async Task RevokeResource_WhereAssignmentHasOtherAccess_Returns204AndKeepsAssignment()
        {
            var request = await AddResource(Person(TestData.ToneKvam), Person(TestData.ArneLund));
            Assignment assignment = await GetRightholderAssignment(TestData.ToneKvam.Id, TestData.ArneLund.Id);
            Assert.NotNull(assignment);

            await Fixture.QueryDb(async db =>
            {
                db.AssignmentPackages.Add(new AssignmentPackage()
                {
                    AssignmentId = assignment.Id,
                    PackageId = PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id,
                });
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources/revoke", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GetAssignmentResource(TestData.ToneKvam.Id, TestData.ArneLund.Id));
            Assert.NotNull(await GetRightholderAssignment(TestData.ToneKvam.Id, TestData.ArneLund.Id));
        }

        /// <summary>
        /// Revoking the resource keeps the assignment when the assignment was not created by the service owner,
        /// even if the resource was the only access on it.
        /// </summary>
        [Fact]
        public async Task RevokeResource_WhereAssignmentWasCreatedByOthers_Returns204AndKeepsAssignment()
        {
            await Fixture.QueryDb(async db =>
            {
                db.Assignments.Add(new Assignment()
                {
                    FromId = TestData.GeirPedersen.Id,
                    ToId = TestData.MaritEriksen.Id,
                    RoleId = RoleConstants.Rightholder,
                });
                await db.SaveChangesAsync(new AuditValues(TestData.BakerJohnsen.Id), TestContext.Current.CancellationToken);
            });

            var request = await AddResource(Person(TestData.GeirPedersen), Person(TestData.MaritEriksen));
            Assert.NotNull(await GetAssignmentResource(TestData.GeirPedersen.Id, TestData.MaritEriksen.Id));

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources/revoke", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GetAssignmentResource(TestData.GeirPedersen.Id, TestData.MaritEriksen.Id));
            Assert.NotNull(await GetRightholderAssignment(TestData.GeirPedersen.Id, TestData.MaritEriksen.Id));
        }

        /// <summary>
        /// The service owner can only revoke resource delegations it made itself.
        /// </summary>
        [Fact]
        public async Task RevokeResource_WhereResourceWasNotDelegatedByServiceOwner_Returns400ResourceNotRevocableFromAssignment()
        {
            await Fixture.QueryDb(async db =>
            {
                var assignment = new Assignment()
                {
                    FromId = TestData.RandiLie.Id,
                    ToId = TestData.KnutVik.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(assignment);
                db.AssignmentResources.Add(new AssignmentResource()
                {
                    AssignmentId = assignment.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = "skd/sirius-skattemelding-v1/50200011/p50200003/delegationpolicy.xml",
                    PolicyVersion = "1.0",
                });
                await db.SaveChangesAsync(new AuditValues(TestData.BakerJohnsen.Id), TestContext.Current.CancellationToken);
            });

            var request = CreateRequest(Person(TestData.RandiLie), Person(TestData.KnutVik));

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources/revoke", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertProblemCode(response, "AM-00049");
            Assert.NotNull(await GetAssignmentResource(TestData.RandiLie.Id, TestData.KnutVik.Id));
        }

        /// <summary>
        /// Revoking a delegation that does not exist is a no-op.
        /// </summary>
        [Fact]
        public async Task RevokeResource_WhereAssignmentDoesNotExist_Returns204NoContent()
        {
            var request = CreateRequest(Person(TestData.OddHalvorsen), Person(TestData.LivKristiansen));

            var response = await CreateClient().PostAsJsonAsync($"{Route}/resources/revoke", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// A service owner that does not own the resource cannot revoke delegations of it.
        /// </summary>
        [Fact]
        public async Task RevokeResource_AsOtherServiceOwner_Returns403ResourceDelegationNotAuthorized()
        {
            var request = CreateRequest(Person(TestData.OddHalvorsen), Person(TestData.LivKristiansen));

            var response = await CreateClient(TestData.BakerJohnsen.Entity.OrganizationIdentifier).PostAsJsonAsync($"{Route}/resources/revoke", request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertProblemCode(response, "AM-00048");
        }
    }

    #endregion
}

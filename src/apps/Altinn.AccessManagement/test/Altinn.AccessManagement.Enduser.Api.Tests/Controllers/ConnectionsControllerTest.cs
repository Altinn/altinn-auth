using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.ProblemDetails;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// <see cref="ConnectionsControllerTest"/>
/// </summary>
public class ConnectionsControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/connections";

    #region GET accessmanagement/api/v1/enduser/connections

    /// <summary>
    /// <see cref="ConnectionsController.GetConnections(AccessManagement.Api.Enduser.Models.ConnectionInput, AccessManagement.Api.Enduser.Models.PagingInput, bool, bool, CancellationToken)"
    /// </summary>
    public class GetConnections : IClassFixture<ApiFixture>
    {
        public GetConnections(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };

                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };
                var agentFromVerdiqToOrjan = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonOrjan,
                    RoleId = RoleConstants.Agent,
                };
                var agentFromNordisToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                var assignmentPackageFromNordisToVerdiq = new AssignmentPackage()
                {
                    AssignmentId = rightholderFromNordisToVerdiq.Id,
                    PackageId = PackageConstants.DocumentBasedSupervision,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);
                db.Assignments.Add(agentFromNordisToPaula);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient(params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }
        
        [Fact]
        public async Task ListConnections_WithDirectionFromOthersUsingFromOthersScope_ReturnsOk()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ, "some:other/scope.read");

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&to={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ConnectionDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ListConnections_WithDirectionFromOthersUsingToOthersScope_ReturnsForbidden()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&to={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingToOthersScope_ReturnsOk()
        {
            var client = CreateClient("some:other/scope.read", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ConnectionDto>>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingFromOthersScope_ReturnsForbidden()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ListConnections_WithDirectionToOthersUsingToOtherScopeWithPartyNotEqualFromOrTo_ReturnsForbidden()
        {
            var client = CreateClient(AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.PersonPaula.Id}&to={TestEntities.PersonOrjan.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region GET accessmanagement/api/v1/enduser/connections/resources/delegationcheck

    /// <summary>
    /// <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/>
    /// </summary>
    public class CheckResource : IClassFixture<ApiFixture>
    {
        public CheckResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                var resourceType = db.ResourceTypes.FirstOrDefault(t => t.Id == Guid.Parse("0195efb8-7c80-7f26-817a-50893176320d"));
                if (resourceType is null)
                {
                    resourceType = new ResourceType() { Id = Guid.Parse("0195efb8-7c80-7f26-817a-50893176320d"), Name = "Test" };
                    db.ResourceTypes.Add(resourceType);
                }

                Resource navSykepengerResource = new Resource()
                {
                    Name = "Dialogs for sickness benefits",
                    Description = "The service is used to send and receive dialogues in Dialogporten about new sick leaves, submitted applications, requests for income reports, and receipts for submitted income reports.",
                    RefId = "nav_sykepenger_dialog",
                    TypeId = resourceType.Id,
                    ProviderId = ProviderConstants.Altinn3.Id,
                };

                db.Resources.Add(navSykepengerResource);

                Resource diheOmsetningsoppgaveAlkhol = new Resource()
                {
                    Name = "Omsetningsoppgave for alkohol",
                    Description = "Omsetningsoppgave for alkohol",
                    RefId = "app_dihe_omsetningsoppgave-for-alkohol",
                    TypeId = resourceType.Id,
                    ProviderId = ProviderConstants.Altinn3.Id,
                };

                db.Resources.Add(diheOmsetningsoppgaveAlkhol);

                var rightholderFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

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

        /// <summary>
        /// Tests that a managing director can successfully check delegation rights for a resource when using the correct authorization scope.
        /// </summary>
        /// <remarks>
        /// This test verifies the delegation check functionality for the resource "Dialogs for sickness benefits" (nav_sykepenger_dialog).
        /// <para>
        /// Test Scenario:
        /// - Actor: Malin Emilie (managing director of Dumbo Adventures)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Resource: nav_sykepenger_dialog (Dialogs for sickness benefits)
        /// - Party: Dumbo Adventures organization
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is OK (200)
        /// - Response contains a valid ResourceCheckDto object
        /// - Response includes exactly 3 rights: "read", "access", and "subscribe"
        /// - All rights have Result = true
        /// - Each right contains ReasonCodes with both RoleAccess and PackageAccess
        /// </para>
        /// </remarks>
        [Fact]
        public async Task CheckResource_NavSykemeldingDialog_FullAccess_RolesAndPackages_ReturnsOK()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=nav_sykepenger_dialog", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == false);
            Assert.Equal(3, result.Rights.Count());

            // Check for the "read" right
            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            // Check for the "access" right
            RightCheckDto accessRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("access", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(accessRight);
            Assert.True(accessRight.Result, "The 'access' right should have Result = true");
            Assert.NotEmpty(accessRight.ReasonCodes);
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            // Check for the "subscribe" right
            RightCheckDto subscribeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("subscribe", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(subscribeRight);
            Assert.True(subscribeRight.Result, "The 'subscribe' right should have Result = true");
            Assert.NotEmpty(subscribeRight.ReasonCodes);
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));
        }

        /// <summary>
        /// Tests that a user with package-based access receives full access when checking delegation rights for a resource,
        /// where all rights are granted through packages but not roles.
        /// </summary>
        /// <remarks>
        /// This test verifies the delegation check functionality for the resource "Dialogs for sickness benefits" (nav_sykepenger_dialog).
        /// <para>
        /// Test Scenario:
        /// - Actor: Thea (user of Dumbo Adventures with package-based access only)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Resource: nav_sykepenger_dialog (Dialogs for sickness benefits)
        /// - Party: Dumbo Adventures organization
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is OK (200)
        /// - Response contains a valid ResourceCheckDto object
        /// - Response includes exactly 3 rights: "read", "access", and "subscribe"
        /// - All rights have Result = true
        /// - Each right contains ReasonCodes with PackageAccess only (no RoleAccess)
        /// </para>
        /// </remarks>
        [Fact]
        public async Task CheckResource_NavSykemeldingDialog_FullAccess_Packages_ReturnsOK()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=nav_sykepenger_dialog", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == false);
            Assert.Equal(3, result.Rights.Count());

            // Check for the "read" right
            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            // Check for the "access" right
            RightCheckDto accessRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("access", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(accessRight);
            Assert.True(accessRight.Result, "The 'access' right should have Result = true");
            Assert.NotEmpty(accessRight.ReasonCodes);
            Assert.DoesNotContain(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            // Check for the "subscribe" right
            RightCheckDto subscribeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("subscribe", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(subscribeRight);
            Assert.True(subscribeRight.Result, "The 'subscribe' right should have Result = true");
            Assert.NotEmpty(subscribeRight.ReasonCodes);
            Assert.DoesNotContain(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));
        }

        /// <summary>
        /// Tests that a managing director receives partial access when checking delegation rights for a resource
        /// where only some rights are granted through roles but not packages.
        /// </summary>
        /// <remarks>
        /// This test verifies the delegation check functionality for the resource "Omsetningsoppgave for alkohol" (app_dihe_omsetningsoppgave-for-alkohol).
        /// <para>
        /// Test Scenario:
        /// - Actor: Malin Emilie (managing director of Dumbo Adventures)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Resource: app_dihe_omsetningsoppgave-for-alkohol (Omsetningsoppgave for alkohol)
        /// - Party: Dumbo Adventures organization
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is OK (200)
        /// - Response contains a valid ResourceCheckDto object
        /// - Response includes exactly 17 rights with a mix of granted and denied results
        /// - The "read" right has Result = true with RoleAccess only (no PackageAccess)
        /// - The "Write (Task_1)" right has Result = true with RoleAccess only (no PackageAccess)
        /// - The "sign" right has Result = false with MissingRoleAccess and MissingDelegationAccess reason codes
        /// </para>
        /// </remarks>
        [Fact]
        public async Task CheckResource_DiheOmsettningsoppgave_PartialAccess_RolesAndPackages_ReturnsOK()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_dihe_omsetningsoppgave-for-alkohol", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.Contains(result.Rights, r => r.Result == false);
            Assert.Contains(result.Rights, r => r.Result == true);
            Assert.Equal(17, result.Rights.Count());

            // Check for the "read" right
            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            // Check for the "access" right
            RightCheckDto writeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("Write (Task_1)", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(writeRight);
            Assert.True(writeRight.Result, "The 'Write' right should have Result = true");
            Assert.NotEmpty(writeRight.ReasonCodes);
            Assert.Contains(writeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.DoesNotContain(writeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            // Check for the "subscribe" right
            RightCheckDto signRight = result.Rights.FirstOrDefault(r => r.Right.Name.Contains("sign", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(signRight);
            Assert.False(signRight.Result, "The 'sign' right should have Result = false");
            Assert.NotEmpty(signRight.ReasonCodes);
            Assert.Contains(signRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingRoleAccess));
            Assert.Contains(signRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingDelegationAccess));
        }

        /// <summary>
        /// Tests that requesting a delegation check with a read-only scope (SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ)
        /// instead of the required to-others write scope returns HTTP 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithReadScope_ReturnsForbidden()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=test-delegation-check-resource", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Tests that requesting a delegation check with the from-others write scope (SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE)
        /// instead of the required to-others write scope returns HTTP 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithFromOthersWriteScope_ReturnsForbidden()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            var response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=test-delegation-check-resource", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region GET accessmanagement/api/v1/enduser/connections/users

    /// <summary>
    /// <see cref="ConnectionsController.GetAvailableUsers(Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// </summary>
    public class GetAvailableUsers : IClassFixture<ApiFixture>
    {
        public GetAvailableUsers(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
        }

        public ApiFixture Fixture { get; }

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

        /// <summary>
        /// Tests that available users for Dumbo Adventures includes Thea when authenticated as Malin Emilie (managing director).
        /// </summary>
        /// <remarks>
        /// This test verifies that the GetAvailableUsers endpoint returns the expected list of available users
        /// for a given organization when the authenticated user has the required write scope.
        /// <para>
        /// Test Scenario:
        /// - Actor: Malin Emilie (person, managing director of Dumbo Adventures AS)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Party: Dumbo Adventures AS (organization)
        /// - Endpoint: GET {Route}/users?party={DumboAdventures.Id}
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is OK (200)
        /// - Response deserializes to a valid PaginatedResult of SimplifiedConnectionDto
        /// - The flattened list of all parties (including nested connections) contains Thea BFF
        /// </para>
        /// </remarks>
        [Fact]
        public async Task GetAvailableUsers_AsMalinForDumbo_ContainsThea()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync($"{Route}/users?party={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            PaginatedResult<SimplifiedConnectionDto> result = JsonSerializer.Deserialize<PaginatedResult<SimplifiedConnectionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            List<SimplifiedPartyDto> allParties = [.. result.Items
                .SelectMany(c => new[] { c }.Concat(c.Connections ?? []))
                .Select(c => c.Party)];

            Assert.Contains(allParties, p => p.Id == TestData.Thea.Id);
        }

        /// <summary>
        /// Tests that requesting available users with a read-only scope (SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ)
        /// instead of the required write scope returns HTTP 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetAvailableUsers_WithReadScope_ReturnsForbidden()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}/users?party={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion

    #region POST accessmanagement/api/v1/enduser/connections

    /// <summary>
    /// <see cref="ConnectionsController.AddRightholder(Guid, Guid, AccessManagement.Api.Enduser.Models.PersonInput, CancellationToken)"/>
    /// </summary>
    public class AddRightholder : IClassFixture<ApiFixture>
    {
        public AddRightholder(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

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

        /// <summary>
        /// Tests that attempting to add a rightholder for a person with no existing connection
        /// to the party returns a BadRequest with the EntityNotExists validation error.
        /// </summary>
        /// <remarks>
        /// This test verifies the AddRightholder endpoint returns a validation problem when Josephine Yvonnesdottir
        /// has no existing connection to Dumbo Adventures.
        /// <para>
        /// Test Scenario:
        /// - Actor: Malin Emilie (managing director of Dumbo Adventures)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Party: Dumbo Adventures organization
        /// - To: Josephine Yvonnesdottir (person with no existing connection to Dumbo Adventures)
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is BadRequest (400)
        /// - Response contains a valid AltinnValidationProblemDetails object
        /// - The outer ErrorCode is "STD-00000" (standard validation error wrapper)
        /// - The validationErrors array contains a single error with code matching ValidationErrors.EntityNotExists
        /// - The validation error path is "QUERY/to"
        /// </para>
        /// </remarks>
        [Fact]
        public async Task AddRightholder_AsMalinForDumboWithJosephine_ReturnsProblem()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(problemDetails);
            Assert.Equal("STD-00000", problemDetails.ErrorCode.ToString());
            Assert.Single(problemDetails.Errors, e => e.ErrorCode == ValidationErrors.EntityNotExists.ErrorCode);
            Assert.Single(problemDetails.Errors, e => e.Paths.Contains("QUERY/to"));
        }

        /// <summary>
        /// Tests that a managing director can successfully add an organization as a rightholder
        /// using the "to" query parameter.
        /// </summary>
        /// <remarks>
        /// This test verifies the AddRightholder endpoint for adding Mille Hundefrisør (an organization)
        /// as a rightholder to Dumbo Adventures. Unlike persons, organizations do not require an existing
        /// connection to pass validation.
        /// <para>
        /// Test Scenario:
        /// - Actor: Malin Emilie (managing director of Dumbo Adventures)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Party: Dumbo Adventures organization
        /// - To: Mille Hundefrisør (organization with no existing connection to Dumbo Adventures)
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is OK (200)
        /// - Response contains a valid AssignmentDto object
        /// - The assignment's FromId matches Dumbo Adventures
        /// - The assignment's ToId matches Mille Hundefrisør
        /// - The assignment's RoleId matches the Rightholder role
        /// </para>
        /// </remarks>
        [Fact]
        public async Task AddRightholder_AsMalinForDumboWithMilleHundefrisor_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}", null, TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            AssignmentDto result = JsonSerializer.Deserialize<AssignmentDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestData.DumboAdventures.Id, result.FromId);
            Assert.Equal(TestData.MilleHundefrisor.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
        }

        /// <summary>
        /// Tests that a managing director can successfully add a new rightholder using PersonInput
        /// with personal number and last name.
        /// </summary>
        /// <remarks>
        /// This test verifies the AddRightholder endpoint using the PersonInput body to look up
        /// Bodil Farmor by her personal number and last name, bypassing the "to" query parameter.
        /// <para>
        /// Test Scenario:
        /// - Actor: Malin Emilie (managing director of Dumbo Adventures)
        /// - Authorization Scope: SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE
        /// - Party: Dumbo Adventures organization
        /// - PersonInput: PersonIdentifier = Bodil's personal number, LastName = "Farmor"
        /// </para>
        /// <para>
        /// Assertions:
        /// - HTTP response status is OK (200)
        /// - Response contains a valid AssignmentDto object
        /// - The assignment's FromId matches Dumbo Adventures
        /// - The assignment's ToId matches Bodil Farmor
        /// - The assignment's RoleId matches the Rightholder role
        /// </para>
        /// </remarks>
        [Fact]
        public async Task AddRightholder_AsMalinForDumboWithBodilViaPersonInput_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "Farmor" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}", content, TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");
            AssignmentDto result = JsonSerializer.Deserialize<AssignmentDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestData.DumboAdventures.Id, result.FromId);
            Assert.Equal(TestData.BodilFarmor.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
        }

        /// <summary>
        /// Tests that requesting to add a rightholder with a read-only scope (SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ)
        /// instead of the required to-others write scope returns HTTP 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddRightholder_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Tests that requesting to add a rightholder with the from-others write scope (SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE)
        /// instead of the required to-others write scope returns HTTP 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddRightholder_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion
}

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Regression coverage for the ADOS subunit resource delegation bug (issue 4029): a person that holds an
/// Altinn 2 (A2) role on an ADOS mainunit must be able to delegate a resource which is protected by that A2
/// role, from the ADOS subunit to a third party.
/// <para>
/// The <c>mainUnit</c> CTE in <c>ResourceDelegationCheckRoleQuery</c> resolves the mainunit through an
/// assignment with the <c>administrativ-enhet-offentlig-sektor</c> (ADOS) role, so the A2 role held on the
/// mainunit surfaces as delegable for the ADOS subunit via the "Direct-AssignmentRole-MainUnit" reason.
/// </para>
/// <para>
/// The <c>app_dihe_omsetningsoppgave-for-alkohol</c> policy permits the <c>A0239</c>
/// (Accountant with signing rights / A2) rolecode, matching the seeded A2 assignment.
/// </para>
/// </summary>
public partial class ConnectionsControllerTest
{
    [IntegrationTest]
    public class CheckResourceAdosSubunit : IClassFixture<ApiFixture>
    {
        private static readonly Guid AdosMainUnitId = Guid.Parse("0196b170-0000-7000-8000-000000000001");
        private static readonly Guid AdosSubUnitId = Guid.Parse("0196b170-0000-7000-8000-000000000002");
        private static readonly Guid AccessManagerPersonId = Guid.Parse("0196b170-0000-7000-8000-000000000003");

        public CheckResourceAdosSubunit(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.AdosSubunitInheritance);
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce<CheckResourceAdosSubunit>(db =>
            {
                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = AdosMainUnitId,
                        Name = "ADOS Mainunit ResourceCheck",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.ORGL,
                        OrganizationIdentifier = "399970001",
                        RefId = "399970001",
                        PartyId = 50970001,
                    },
                    new Entity()
                    {
                        Id = AdosSubUnitId,
                        Name = "ADOS Subunit ResourceCheck",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.ADOS,
                        OrganizationIdentifier = "399970002",
                        RefId = "399970002",
                        ParentId = AdosMainUnitId,
                        PartyId = 50970002,
                    },
                    new Entity()
                    {
                        Id = AccessManagerPersonId,
                        Name = "Anders Tilgangsstyrer",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "26019099952",
                        RefId = "26019099952",
                        PartyId = 50970003,
                        UserId = 50970003,
                        DateOfBirth = new DateOnly(1990, 1, 26),
                    });
                db.SaveChanges();

                // The ADOS subunit points to its mainunit via the ADOS (administrativ-enhet-offentlig-sektor) role.
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosSubUnitId,
                    ToId = AdosMainUnitId,
                    RoleId = RoleConstants.AdministrativeUnitPublicSector,
                });

                // Anders is AccessManager and holds the A2 role (A0239 - Accountant with signing rights) on the ADOS mainunit.
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosMainUnitId,
                    ToId = AccessManagerPersonId,
                    RoleId = RoleConstants.AccessManager,
                });
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosMainUnitId,
                    ToId = AccessManagerPersonId,
                    RoleId = RoleConstants.AccountantWithSigningRights,
                });

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
        /// Anders (AccessManager holding the A0239/A2 role on the ADOS mainunit) runs a resource delegation
        /// check for a resource protected by that A2 role, on behalf of the ADOS subunit. The read right must be
        /// granted through the inherited mainunit role.
        /// </summary>
        [Fact]
        public async Task CheckResource_AsAccessManagerWithA2RoleOfAdosMainUnit_ReturnsRightGrantedByRoleFromAdosSubunit()
        {
            HttpClient client = CreateClient(AccessManagerPersonId, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={AdosSubUnitId}&resource=app_dihe_omsetningsoppgave-for-alkohol",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.NotEmpty(result.Rights);

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true (delegable via the inherited A2 role from the ADOS mainunit)");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
        }
    }

    /// <summary>
    /// Feature-off counterpart of <see cref="CheckResourceAdosSubunit"/>: when the
    /// <c>AccessManagement.Subunit.AdosInheritance</c> feature flag is disabled, the ADOS main-unit
    /// relationship must not be treated as a main-unit branch in <c>ResourceDelegationCheckRoleQuery</c>, so the
    /// A2 role held on the mainunit must NOT grant the resource right from the ADOS subunit.
    /// </summary>
    [IntegrationTest]
    public class CheckResourceAdosSubunitFeatureDisabled : IClassFixture<ApiFixture>
    {
        private static readonly Guid AdosMainUnitId = Guid.Parse("0196b171-0000-7000-8000-000000000001");
        private static readonly Guid AdosSubUnitId = Guid.Parse("0196b171-0000-7000-8000-000000000002");
        private static readonly Guid AccessManagerPersonId = Guid.Parse("0196b171-0000-7000-8000-000000000003");

        public CheckResourceAdosSubunitFeatureDisabled(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.AdosSubunitInheritance);
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce<CheckResourceAdosSubunitFeatureDisabled>(db =>
            {
                db.Entities.AddRange(
                    new Entity()
                    {
                        Id = AdosMainUnitId,
                        Name = "ADOS Mainunit ResourceCheck Disabled",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.ORGL,
                        OrganizationIdentifier = "399971001",
                        RefId = "399971001",
                        PartyId = 50971001,
                    },
                    new Entity()
                    {
                        Id = AdosSubUnitId,
                        Name = "ADOS Subunit ResourceCheck Disabled",
                        TypeId = EntityTypeConstants.Organization,
                        VariantId = EntityVariantConstants.ADOS,
                        OrganizationIdentifier = "399971002",
                        RefId = "399971002",
                        ParentId = AdosMainUnitId,
                        PartyId = 50971002,
                    },
                    new Entity()
                    {
                        Id = AccessManagerPersonId,
                        Name = "Anders Tilgangsstyrer Disabled",
                        TypeId = EntityTypeConstants.Person,
                        VariantId = EntityVariantConstants.Person,
                        PersonIdentifier = "26019099953",
                        RefId = "26019099953",
                        PartyId = 50971003,
                        UserId = 50971003,
                        DateOfBirth = new DateOnly(1990, 1, 26),
                    });
                db.SaveChanges();

                // The ADOS subunit points to its mainunit via the ADOS (administrativ-enhet-offentlig-sektor) role.
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosSubUnitId,
                    ToId = AdosMainUnitId,
                    RoleId = RoleConstants.AdministrativeUnitPublicSector,
                });

                // Anders is AccessManager and holds the A2 role (A0239 - Accountant with signing rights) on the ADOS mainunit.
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosMainUnitId,
                    ToId = AccessManagerPersonId,
                    RoleId = RoleConstants.AccessManager,
                });
                db.Assignments.Add(new Assignment()
                {
                    FromId = AdosMainUnitId,
                    ToId = AccessManagerPersonId,
                    RoleId = RoleConstants.AccountantWithSigningRights,
                });

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
        /// With ADOS inheritance disabled, the resource delegation check on behalf of the ADOS subunit must not
        /// grant the read right via the A2 role inherited from the mainunit.
        /// </summary>
        [Fact]
        public async Task CheckResource_AsAccessManagerWithA2RoleOfAdosMainUnit_FeatureDisabled_ReturnsRightNotGrantedFromAdosSubunit()
        {
            HttpClient client = CreateClient(AccessManagerPersonId, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={AdosSubUnitId}&resource=app_dihe_omsetningsoppgave-for-alkohol",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.False(readRight.Result, "The 'read' right must NOT be delegable when ADOS inheritance is disabled");
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
        }
    }
}

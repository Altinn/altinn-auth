using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on who last changed an access and when
/// (<see cref="PermissionDto.AuditInfo"/>) on the
/// access package, resource and instance endpoints.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="PermissionDto.AuditInfo"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Kari Endringsdottir: recipient with a direct Rightholder assignment from Dumbo Adventures carrying the
    ///   SalarySpecialCategory package, the Skattemelding resource and one SiriusSkattemelding instance
    /// - Havbris Regnskap AS: organization with a Rightholder assignment from Dumbo Adventures carrying the
    ///   SalarySpecialCategory package
    /// - Ola Nøkkelrolle: managing director of Havbris Regnskap AS, so he receives Havbris' access through a key role
    /// - Fjordlys Holding AS: main unit with a Rightholder assignment to Kari carrying the SalarySpecialCategory package,
    ///   and its subunit Fjordlys Holding AS avd. Bergen, which inherits that access
    /// - Siri Slemdal: person who performed the package, resource and instance changes above, at <see cref="ChangedAt"/>
    /// </para>
    /// <para>
    /// The default seed is saved by the StaticDataIngest system entity, which covers the system performer case.
    /// </para>
    /// <para>
    /// Runs on its own fixture, not the shared read-only collection, because the feature flag
    /// <see cref="AccessMgmtFeatureFlags.EnableEnduserConnectionsChangeInfo"/> must be set before the host is built.
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class ChangeInfo : IClassFixture<ApiFixture>
    {
        internal static readonly Guid Recipient = Guid.Parse("0196c3a0-0000-7000-8000-000000000001");
        internal static readonly Guid KeyRoleOrganization = Guid.Parse("0196c3a0-0000-7000-8000-000000000002");
        internal static readonly Guid KeyRolePerson = Guid.Parse("0196c3a0-0000-7000-8000-000000000003");
        internal static readonly Guid Performer = Guid.Parse("0196c3a0-0000-7000-8000-000000000004");
        internal static readonly Guid MainUnit = Guid.Parse("0196c3a0-0000-7000-8000-000000000005");
        internal static readonly Guid SubUnit = Guid.Parse("0196c3a0-0000-7000-8000-000000000006");

        internal const string PerformerPersonIdentifier = "02029099911";
        internal const string InstanceId = "urn:altinn:instance-id:50960001/c0ffee00-0000-4000-8000-000000000001";

        internal static readonly DateOnly PerformerDateOfBirth = new(1990, 2, 2);
        internal static readonly DateTimeOffset ChangedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        internal static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ChangeInfo(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserConnectionsChangeInfo);
            Fixture.EnsureSeedOnce<ChangeInfo>(Seed);
        }

        /// <summary>
        /// Seeds the parties and the access described on <see cref="ChangeInfo"/>.
        /// </summary>
        internal static void Seed(AppDbContext db)
        {
            db.Entities.AddRange(
                new Entity()
                {
                    Id = Recipient,
                    Name = "Kari Endringsdottir",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "01019099922",
                    RefId = "01019099922",
                    PartyId = 50960001,
                    UserId = 50960001,
                    DateOfBirth = new DateOnly(1990, 1, 1),
                },
                new Entity()
                {
                    Id = KeyRoleOrganization,
                    Name = "Havbris Regnskap AS",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399960002",
                    RefId = "399960002",
                    PartyId = 50960002,
                },
                new Entity()
                {
                    Id = KeyRolePerson,
                    Name = "Ola Nøkkelrolle",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = "03039099933",
                    RefId = "03039099933",
                    PartyId = 50960003,
                    UserId = 50960003,
                    DateOfBirth = new DateOnly(1990, 3, 3),
                },
                new Entity()
                {
                    Id = Performer,
                    Name = "Siri Slemdal",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                    PersonIdentifier = PerformerPersonIdentifier,
                    RefId = PerformerPersonIdentifier,
                    PartyId = 50960004,
                    UserId = 50960004,
                    Username = "sirislemdal",
                    DateOfBirth = PerformerDateOfBirth,
                },
                new Entity()
                {
                    Id = MainUnit,
                    Name = "Fjordlys Holding AS",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.AS,
                    OrganizationIdentifier = "399960005",
                    RefId = "399960005",
                    PartyId = 50960005,
                });

            db.SaveChanges();

            db.Entities.Add(new Entity()
            {
                Id = SubUnit,
                Name = "Fjordlys Holding AS avd. Bergen",
                TypeId = EntityTypeConstants.Organization,
                VariantId = EntityVariantConstants.BEDR,
                OrganizationIdentifier = "399960006",
                RefId = "399960006",
                PartyId = 50960006,
                ParentId = MainUnit,
            });

            db.SaveChanges();

            var dumboToRecipient = new Assignment()
            {
                FromId = TestData.DumboAdventures.Id,
                ToId = Recipient,
                RoleId = RoleConstants.Rightholder,
            };

            var dumboToKeyRoleOrganization = new Assignment()
            {
                FromId = TestData.DumboAdventures.Id,
                ToId = KeyRoleOrganization,
                RoleId = RoleConstants.Rightholder,
            };

            var mainUnitToRecipient = new Assignment()
            {
                FromId = MainUnit,
                ToId = Recipient,
                RoleId = RoleConstants.Rightholder,
            };

            db.Assignments.AddRange(
                dumboToRecipient,
                dumboToKeyRoleOrganization,
                mainUnitToRecipient,
                new Assignment()
                {
                    FromId = KeyRoleOrganization,
                    ToId = KeyRolePerson,
                    RoleId = RoleConstants.ManagingDirector,
                });

            db.SaveChanges();

            db.AssignmentPackages.AddRange(
                new AssignmentPackage()
                {
                    AssignmentId = dumboToRecipient.Id,
                    PackageId = PackageConstants.SalarySpecialCategory.Id,
                },
                new AssignmentPackage()
                {
                    AssignmentId = dumboToKeyRoleOrganization.Id,
                    PackageId = PackageConstants.SalarySpecialCategory.Id,
                },
                new AssignmentPackage()
                {
                    AssignmentId = mainUnitToRecipient.Id,
                    PackageId = PackageConstants.SalarySpecialCategory.Id,
                });

            db.AssignmentResources.Add(new AssignmentResource()
            {
                AssignmentId = dumboToRecipient.Id,
                ResourceId = TestData.SkattResource.Id,
            });

            db.AssignmentInstances.Add(new AssignmentInstance()
            {
                AssignmentId = dumboToRecipient.Id,
                ResourceId = TestData.SiriusSkattemelding.Id,
                InstanceId = InstanceId,
                PolicyPath = "sirius-skattemelding-v1/50960001/changeinfo/delegationpolicy.xml",
                PolicyVersion = "1.0",
            });

            // Saved with an explicit audit so audit_changedby and audit_validfrom are known.
            db.SaveChanges(new AuditValues(Performer, Performer, "changeinfo-test-seed", ChangedAt));
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

        internal static async Task<(T Result, string Body)> GetOk<T>(HttpClient client, string url)
        {
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {body}");

            T result = JsonSerializer.Deserialize<T>(body, JsonOptions);
            Assert.NotNull(result);
            return (result, body);
        }

        private static void AssertChangedBySeedPerformer(PermissionDto permission, string body)
        {
            Assert.NotNull(permission.AuditInfo);
            Assert.Equal(ChangedAt, permission.AuditInfo.LastModified);
            var lastModifiedBy = permission.AuditInfo.LastModifiedBy;
            Assert.NotNull(lastModifiedBy);
            Assert.Equal(Performer, lastModifiedBy.Id);
            Assert.Equal("Siri Slemdal", lastModifiedBy.Name);
            Assert.Equal(50960004, lastModifiedBy.PartyId);
            Assert.Equal(50960004, lastModifiedBy.UserId);
            Assert.Equal("sirislemdal", lastModifiedBy.Username);
            Assert.False(string.IsNullOrEmpty(lastModifiedBy.Type));
            AssertNoPersonalData(body);
        }

        private static void AssertNoPersonalData(string body)
        {
            Assert.DoesNotContain(PerformerPersonIdentifier, body);
            Assert.DoesNotContain(PerformerDateOfBirth.ToString("yyyy-MM-dd"), body);
        }

        /// <summary>
        /// Malin (MD of Dumbo) lists the packages Dumbo has given to Kari. The direct assignment
        /// carries who gave it and when, without the performer's personal identifiers.
        /// </summary>
        [Fact]
        public async Task GetPackages_DirectAssignment_ToOthers_ReturnsAuditInfo()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var (result, body) = await GetOk<PaginatedResult<PackagePermissionDto>>(
                client,
                $"{Route}/accesspackages?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={Recipient}");

            var package = Assert.Single(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            AssertChangedBySeedPerformer(Assert.Single(package.Permissions), body);
        }

        /// <summary>
        /// Kari lists the packages she has received from Dumbo. Who gave the access is returned in
        /// this direction too, still without the performer's personal identifiers.
        /// </summary>
        [Fact]
        public async Task GetPackages_DirectAssignment_FromOthers_ReturnsAuditInfo()
        {
            HttpClient client = CreateClient(Recipient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var (result, body) = await GetOk<PaginatedResult<PackagePermissionDto>>(
                client,
                $"{Route}/accesspackages?party={Recipient}&to={Recipient}&from={TestData.DumboAdventures.Id}");

            var package = Assert.Single(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            AssertChangedBySeedPerformer(Assert.Single(package.Permissions), body);
        }

        /// <summary>
        /// Ola is managing director of Havbris, which has the package from Dumbo. The access he
        /// gets through the key role carries the change info of Havbris' assignment.
        /// </summary>
        [Fact]
        public async Task GetPackages_InheritedThroughKeyRole_ReturnsChangeInfoOfUnderlyingAssignment()
        {
            HttpClient client = CreateClient(KeyRolePerson, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var (result, body) = await GetOk<PaginatedResult<PackagePermissionDto>>(
                client,
                $"{Route}/accesspackages?party={KeyRolePerson}&to={KeyRolePerson}&from={TestData.DumboAdventures.Id}");

            var package = Assert.Single(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            var permission = Assert.Single(package.Permissions, p => p.Via?.Id == KeyRoleOrganization);
            AssertChangedBySeedPerformer(permission, body);
        }

        /// <summary>
        /// Fjordlys Holding has given Kari the package, and its subunit inherits it. Kari lists her packages
        /// without a <c>from</c> filter, so the subunit records are added after the packages are loaded.
        /// They carry the change info of the main unit's assignment.
        /// </summary>
        [Fact]
        public async Task GetPackages_InheritedBySubunit_ReturnsChangeInfoOfMainUnitAssignment()
        {
            HttpClient client = CreateClient(Recipient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var (result, body) = await GetOk<PaginatedResult<PackagePermissionDto>>(
                client,
                $"{Route}/accesspackages?party={Recipient}&to={Recipient}");

            var package = Assert.Single(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            AssertChangedBySeedPerformer(Assert.Single(package.Permissions, p => p.From?.Id == MainUnit), body);

            var subunitPermission = Assert.Single(package.Permissions, p => p.From?.Id == SubUnit);
            Assert.NotNull(subunitPermission.Via);
            Assert.Equal(MainUnit, subunitPermission.Via.Id);
            AssertChangedBySeedPerformer(subunitPermission, body);
        }

        /// <summary>
        /// Malin is managing director of Dumbo, and the packages she has through that role come
        /// from the role, not from anyone giving them, so the fields are empty.
        /// </summary>
        [Fact]
        public async Task GetPackages_RolePackage_ReturnsNullChangeInfo()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var (result, _) = await GetOk<PaginatedResult<PackagePermissionDto>>(
                client,
                $"{Route}/accesspackages?party={TestData.MalinEmilie.Id}&to={TestData.MalinEmilie.Id}&from={TestData.DumboAdventures.Id}");

            var rolePermissions = result.Items
                .SelectMany(p => p.Permissions)
                .Where(p => p.Role?.Id == RoleConstants.ManagingDirector.Id)
                .ToList();

            Assert.NotEmpty(rolePermissions);
            Assert.All(rolePermissions, p =>
            {
                Assert.Null(p.AuditInfo);
            });
        }

        /// <summary>
        /// The default seed is written by the StaticDataIngest system entity. It is returned as it is,
        /// so the frontend can decide how to show it.
        /// </summary>
        [Fact]
        public async Task GetPackages_SystemPerformer_ReturnsSystemEntity()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var (result, _) = await GetOk<PaginatedResult<PackagePermissionDto>>(
                client,
                $"{Route}/accesspackages?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}");

            var package = Assert.Single(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            var permission = Assert.Single(package.Permissions);
            Assert.NotNull(permission.AuditInfo);
            var lastModifiedBy = permission.AuditInfo.LastModifiedBy;
            Assert.NotNull(lastModifiedBy);
            Assert.Equal(SystemEntityConstants.StaticDataIngest.Id, lastModifiedBy.Id);
            Assert.Equal(EntityTypeConstants.Internal.Entity.Name, lastModifiedBy.Type);
        }

        /// <summary>
        /// Malin lists the resources Dumbo has given to Kari, which carry the change info of the assignment resource.
        /// </summary>
        [Fact]
        public async Task GetResources_DirectAssignment_ToOthers_ReturnsAuditInfo()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var (result, body) = await GetOk<List<ResourcePermissionDto>>(
                client,
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={Recipient}");

            var resource = Assert.Single(result, r => r.Resource.Id == TestData.SkattResource.Id);
            AssertChangedBySeedPerformer(Assert.Single(resource.Permissions), body);
        }

        /// <summary>
        /// Kari lists the resources she has received from Dumbo, with who gave them and when.
        /// </summary>
        [Fact]
        public async Task GetResources_DirectAssignment_FromOthers_ReturnsAuditInfo()
        {
            HttpClient client = CreateClient(Recipient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var (result, body) = await GetOk<List<ResourcePermissionDto>>(
                client,
                $"{Route}/resources?party={Recipient}&to={Recipient}&from={TestData.DumboAdventures.Id}");

            var resource = Assert.Single(result, r => r.Resource.Id == TestData.SkattResource.Id);
            AssertChangedBySeedPerformer(Assert.Single(resource.Permissions), body);
        }

        /// <summary>
        /// Malin lists the instances Dumbo has given to Kari, which carry the change info of the assignment instance.
        /// </summary>
        [Fact]
        public async Task GetInstances_DirectAssignment_ToOthers_ReturnsAuditInfo()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            var (result, body) = await GetOk<PaginatedResult<InstancePermissionDto>>(
                client,
                $"{Route}/resources/instances?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={Recipient}");

            var instance = Assert.Single(result.Items, i => i.Instance.RefId == InstanceId);
            AssertChangedBySeedPerformer(Assert.Single(instance.Permissions), body);
        }

        /// <summary>
        /// Kari lists the instances she has received from Dumbo, with who gave them and when.
        /// </summary>
        [Fact]
        public async Task GetInstances_DirectAssignment_FromOthers_ReturnsAuditInfo()
        {
            HttpClient client = CreateClient(Recipient, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var (result, body) = await GetOk<PaginatedResult<InstancePermissionDto>>(
                client,
                $"{Route}/resources/instances?party={Recipient}&to={Recipient}&from={TestData.DumboAdventures.Id}");

            var instance = Assert.Single(result.Items, i => i.Instance.RefId == InstanceId);
            AssertChangedBySeedPerformer(Assert.Single(instance.Permissions), body);
        }
    }

    /// <summary>
    /// With <see cref="AccessMgmtFeatureFlags.EnableEnduserConnectionsChangeInfo"/> turned off, the same access as in
    /// <see cref="ChangeInfo"/> is returned without <see cref="PermissionDto.AuditInfo"/>.
    /// </summary>
    [IntegrationTest]
    public class ChangeInfoDisabled : IClassFixture<ApiFixture>
    {
        public ChangeInfoDisabled(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserConnectionsChangeInfo);
            Fixture.EnsureSeedOnce<ChangeInfoDisabled>(ChangeInfo.Seed);
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, ChangeInfo.Recipient.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        private static void AssertNoChangeInfo(PermissionDto permission)
        {
            Assert.Null(permission.AuditInfo);
        }

        [Fact]
        public async Task GetPackages_FeatureOff_ReturnsNullChangeInfo()
        {
            var (result, _) = await ChangeInfo.GetOk<PaginatedResult<PackagePermissionDto>>(
                CreateClient(),
                $"{Route}/accesspackages?party={ChangeInfo.Recipient}&to={ChangeInfo.Recipient}&from={TestData.DumboAdventures.Id}");

            var package = Assert.Single(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            AssertNoChangeInfo(Assert.Single(package.Permissions));
        }

        [Fact]
        public async Task GetResources_FeatureOff_ReturnsNullChangeInfo()
        {
            var (result, _) = await ChangeInfo.GetOk<List<ResourcePermissionDto>>(
                CreateClient(),
                $"{Route}/resources?party={ChangeInfo.Recipient}&to={ChangeInfo.Recipient}&from={TestData.DumboAdventures.Id}");

            var resource = Assert.Single(result, r => r.Resource.Id == TestData.SkattResource.Id);
            AssertNoChangeInfo(Assert.Single(resource.Permissions));
        }

        [Fact]
        public async Task GetInstances_FeatureOff_ReturnsNullChangeInfo()
        {
            var (result, _) = await ChangeInfo.GetOk<PaginatedResult<InstancePermissionDto>>(
                CreateClient(),
                $"{Route}/resources/instances?party={ChangeInfo.Recipient}&to={ChangeInfo.Recipient}&from={TestData.DumboAdventures.Id}");

            var instance = Assert.Single(result.Items, i => i.Instance.RefId == ChangeInfo.InstanceId);
            AssertNoChangeInfo(Assert.Single(instance.Permissions));
        }
    }
}

using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.Tests.Integration.Services;

/// <summary>
/// Verifies that Altinn 2 roles are excluded from connection lookups and resource delegation checks
/// when the <c>AccessManagement.Roles.DisableAltinn2</c> feature toggle is enabled, and that behavior
/// is unchanged when it is disabled.
/// </summary>
[IntegrationTest]
public class ConnectionQueryAltinn2RolesTests : IClassFixture<EfDatabaseFixture>, IAsyncLifetime
{
    private static readonly Guid A2OrgId = Guid.Parse("0195efb8-7c80-7a02-8002-000000000001");
    private static readonly Guid A2OrgSubunitId = Guid.Parse("0195efb8-7c80-7a02-8002-000000000002");
    private static readonly Guid DaglOrgId = Guid.Parse("0195efb8-7c80-7a02-8002-000000000003");
    private static readonly Guid A2OnlyPersonId = Guid.Parse("0195efb8-7c80-7a02-8002-000000000010");
    private static readonly Guid DaglPersonId = Guid.Parse("0195efb8-7c80-7a02-8002-000000000011");
    private static readonly Guid MixedPersonId = Guid.Parse("0195efb8-7c80-7a02-8002-000000000012");

    private readonly EfDatabaseFixture _fixture;
    private readonly AppDbContext _db;

    public ConnectionQueryAltinn2RolesTests(EfDatabaseFixture fixture)
    {
        _fixture = fixture;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Db.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await _fixture.EnsureSeedOnceAsync(SeedTestData);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task SeedTestData()
    {
        _db.Entities.AddRange(
            new Entity() { Id = A2OrgId, Name = "A2 Org", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "A2-ORG-01", RefId = "A2-ORG-01" },
            new Entity() { Id = A2OrgSubunitId, Name = "A2 Org - Bedr", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "A2-ORG-01-01", ParentId = A2OrgId, RefId = "A2-ORG-01-01" },
            new Entity() { Id = DaglOrgId, Name = "Dagl Org", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "A2-ORG-02", RefId = "A2-ORG-02" },
            new Entity() { Id = A2OnlyPersonId, Name = "A2Only", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "11018412345", RefId = "11018412345", DateOfBirth = DateOnly.Parse("1984-01-11") },
            new Entity() { Id = DaglPersonId, Name = "A2Dagl", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "12018412345", RefId = "12018412345", DateOfBirth = DateOnly.Parse("1984-01-12") },
            new Entity() { Id = MixedPersonId, Name = "A2Mixed", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "13018412345", RefId = "13018412345", DateOfBirth = DateOnly.Parse("1984-01-13") });

        _db.Assignments.AddRange(
            new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a02-8002-000000000020"), FromId = A2OrgId, ToId = A2OnlyPersonId, RoleId = RoleConstants.PrimaryIndustryAndFoodstuff },
            new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a02-8002-000000000021"), FromId = DaglOrgId, ToId = DaglPersonId, RoleId = RoleConstants.ManagingDirector },
            new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a02-8002-000000000022"), FromId = A2OrgId, ToId = MixedPersonId, RoleId = RoleConstants.PrimaryIndustryAndFoodstuff },
            new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a02-8002-000000000023"), FromId = A2OrgId, ToId = MixedPersonId, RoleId = RoleConstants.ManagingDirector });

        await _db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));
    }

    private static ConnectionQueryFilter FromOthersFilter(Guid toId) => new()
    {
        ToIds = [toId],
        IncludeKeyRole = true,
        IncludeDelegation = true,
        IncludeMainUnitConnections = true,
        IncludeSubConnections = true,
        EnrichEntities = false,
        ExcludeDeleted = false,
    };

    [Fact]
    public async Task GetConnectionsFromOthers_DirectA2Assignment_ToggleOff_IncludesA2Role()
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: false);

        var result = await query.GetConnectionsFromOthersAsync(FromOthersFilter(A2OnlyPersonId), TestContext.Current.CancellationToken);

        Assert.Contains(result, r => r.FromId == A2OrgId && r.RoleId == RoleConstants.PrimaryIndustryAndFoodstuff.Id);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_DirectA2Assignment_ToggleOn_ExcludesPartyEntirely()
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: true);

        var result = await query.GetConnectionsFromOthersAsync(FromOthersFilter(A2OnlyPersonId), TestContext.Current.CancellationToken);

        Assert.DoesNotContain(result, r => r.FromId == A2OrgId);
        Assert.DoesNotContain(result, r => RoleConstants.Altinn2RoleIds.Contains(r.RoleId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetConnectionsFromOthers_MixedRoles_KeepsPartyWithOnlyNonA2RolesWhenToggleOn(bool excludeAltinn2Roles)
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: excludeAltinn2Roles);

        var result = await query.GetConnectionsFromOthersAsync(FromOthersFilter(MixedPersonId), TestContext.Current.CancellationToken);

        Assert.Contains(result, r => r.FromId == A2OrgId && r.RoleId == RoleConstants.ManagingDirector.Id);
        Assert.Equal(!excludeAltinn2Roles, result.Any(r => r.FromId == A2OrgId && r.RoleId == RoleConstants.PrimaryIndustryAndFoodstuff.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorizedParties_A2OnlyParty_ExcludedOnlyWhenToggleOn(bool excludeAltinn2Roles)
    {
        var repo = CreateAuthorizedPartyRepo(excludeAltinn2Roles);

        var a2Only = await repo.GetConnectionsFromOthers(A2OnlyPersonId, ct: TestContext.Current.CancellationToken);
        var mixed = await repo.GetConnectionsFromOthers(MixedPersonId, ct: TestContext.Current.CancellationToken);

        Assert.Equal(!excludeAltinn2Roles, a2Only.Any(r => r.FromId == A2OrgId));
        Assert.Contains(mixed, r => r.FromId == A2OrgId && r.RoleId == RoleConstants.ManagingDirector.Id);
        Assert.Equal(!excludeAltinn2Roles, mixed.Any(r => RoleConstants.Altinn2RoleIds.Contains(r.RoleId)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pip_A2OnlyParty_ExcludedOnlyWhenToggleOn(bool excludeAltinn2Roles)
    {
        var repo = CreateAuthorizedPartyRepo(excludeAltinn2Roles);

        var a2Only = await repo.GetPipConnectionsFromOthers(A2OnlyPersonId, ct: TestContext.Current.CancellationToken);
        var dagl = await repo.GetPipConnectionsFromOthers(DaglPersonId, ct: TestContext.Current.CancellationToken);

        Assert.Equal(!excludeAltinn2Roles, a2Only.Any(r => r.FromId == A2OrgId));
        Assert.Contains(dagl, r => r.FromId == DaglOrgId && r.RoleId == RoleConstants.ManagingDirector.Id);
        Assert.Equal(!excludeAltinn2Roles, dagl.Any(r => RoleConstants.Altinn2RoleIds.Contains(r.RoleId)));
    }

    private AuthorizedPartyRepoServiceEf CreateAuthorizedPartyRepo(bool excludeAltinn2Roles) =>
        new(_db, new ConnectionQuery(_db, excludeAltinn2Roles: excludeAltinn2Roles), new DisabledFeatureManager());

    private sealed class DisabledFeatureManager : IFeatureManager
    {
        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<bool> IsEnabledAsync(string feature) => Task.FromResult(false);

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) => Task.FromResult(false);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_RoleMap_ToggleOff_IncludesRoleMapA2Roles()
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: false);

        var result = await query.GetConnectionsFromOthersAsync(FromOthersFilter(DaglPersonId), TestContext.Current.CancellationToken);

        Assert.Contains(result, r => r.FromId == DaglOrgId && r.RoleId == RoleConstants.ManagingDirector.Id);
        Assert.Contains(result, r => r.FromId == DaglOrgId && r.IsRoleMap && RoleConstants.Altinn2RoleIds.Contains(r.RoleId));
    }

    [Fact]
    public async Task GetConnectionsFromOthers_RoleMap_ToggleOn_KeepsDaglButExcludesA2Roles()
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: true);

        var result = await query.GetConnectionsFromOthersAsync(FromOthersFilter(DaglPersonId), TestContext.Current.CancellationToken);

        Assert.Contains(result, r => r.FromId == DaglOrgId && r.RoleId == RoleConstants.ManagingDirector.Id);
        Assert.DoesNotContain(result, r => r.IsRoleMap);
        Assert.DoesNotContain(result, r => RoleConstants.Altinn2RoleIds.Contains(r.RoleId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetConnectionsToOthers_ExcludesA2RolesOnlyWhenToggleOn(bool excludeAltinn2Roles)
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: excludeAltinn2Roles);
        var filter = new ConnectionQueryFilter
        {
            FromIds = [A2OrgId],
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            EnrichEntities = false,
            ExcludeDeleted = false,
        };

        var result = await query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Equal(!excludeAltinn2Roles, result.Any(r => r.ToId == A2OnlyPersonId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetConnectionsToOthers_RoleMap_ExcludedOnlyWhenToggleOn(bool excludeAltinn2Roles)
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: excludeAltinn2Roles);
        var filter = new ConnectionQueryFilter
        {
            FromIds = [DaglOrgId],
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            EnrichEntities = false,
            ExcludeDeleted = false,
        };

        var result = await query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(result, r => r.ToId == DaglPersonId && r.RoleId == RoleConstants.ManagingDirector.Id);
        Assert.Equal(!excludeAltinn2Roles, result.Any(r => r.IsRoleMap && RoleConstants.Altinn2RoleIds.Contains(r.RoleId)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HasConnection_A2Assignment_OnlyWhenToggleOff(bool excludeAltinn2Roles)
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: excludeAltinn2Roles);

        var (direct, _) = await query.HasConnection(A2OrgId, A2OnlyPersonId, [ConnectionReason.Assignment]);
        var (hierarchy, _) = await query.HasConnection(A2OrgSubunitId, A2OnlyPersonId, [ConnectionReason.Hierarchy]);

        Assert.Equal(!excludeAltinn2Roles, direct);
        Assert.Equal(!excludeAltinn2Roles, hierarchy);
    }

    [Fact]
    public async Task HasConnection_NonA2Assignment_ToggleOn_ReturnsTrue()
    {
        var query = new ConnectionQuery(_db, excludeAltinn2Roles: true);

        var (result, reason) = await query.HasConnection(DaglOrgId, DaglPersonId, [ConnectionReason.Assignment]);

        Assert.True(result);
        Assert.Equal(ConnectionReason.Assignment, reason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RoleDelegationCheck_DirectA2Assignment_OnlyWhenToggleOff(bool excludeAltinn2Roles)
    {
        var result = await _db.GetRolesForResourceDelegationCheck(
            A2OrgId,
            A2OnlyPersonId,
            excludeAltinn2Roles: excludeAltinn2Roles,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(!excludeAltinn2Roles, result.Any(r => r.Role.Id == RoleConstants.PrimaryIndustryAndFoodstuff.Id && r.Result));
        if (excludeAltinn2Roles)
        {
            Assert.DoesNotContain(result, r => RoleConstants.Altinn2RoleIds.Contains(r.Role.Id));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RoleDelegationCheck_RoleMap_OnlyWhenToggleOff(bool excludeAltinn2Roles)
    {
        var result = await _db.GetRolesForResourceDelegationCheck(
            DaglOrgId,
            DaglPersonId,
            excludeAltinn2Roles: excludeAltinn2Roles,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(!excludeAltinn2Roles, result.Any(r => RoleConstants.Altinn2RoleIds.Contains(r.Role.Id) && r.Result));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RoleDelegationCheck_MainAdministrator_OnlyWhenToggleOff(bool excludeAltinn2Roles)
    {
        var result = await _db.GetRolesForResourceDelegationCheck(
            A2OrgId,
            DaglPersonId,
            toIsMainAdminForFrom: true,
            excludeAltinn2Roles: excludeAltinn2Roles,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(!excludeAltinn2Roles, result.Any(r => r.Reason.Description == "MainAdministratorRole" && r.Result && RoleConstants.Altinn2RoleIds.Contains(r.Role.Id)));
        Assert.True(excludeAltinn2Roles == false || result.All(r => !RoleConstants.Altinn2RoleIds.Contains(r.Role.Id)));
    }
}

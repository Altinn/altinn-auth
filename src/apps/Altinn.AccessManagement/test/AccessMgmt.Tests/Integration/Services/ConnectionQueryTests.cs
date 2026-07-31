using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;
using DelegationPackage = Altinn.AccessMgmt.PersistenceEF.Models.DelegationPackage;
using DelegationResource = Altinn.AccessMgmt.PersistenceEF.Models.DelegationResource;

namespace Altinn.AccessManagement.Tests.Integration.Services;

[IntegrationTest]
public class ConnectionQueryTests : IClassFixture<EfDatabaseFixture>, IAsyncLifetime
{
    private readonly EfDatabaseFixture _fixture;
    private readonly AppDbContext _db;
    private readonly ConnectionQuery _query;

    public ConnectionQueryTests(EfDatabaseFixture fixture)
    {
        _fixture = fixture;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Db.Admin.ToString())
            .Options;

        _db = new AppDbContext(options);
        _query = new ConnectionQuery(_db);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await _fixture.EnsureSeedOnceAsync(() => SeedTestData(_db));

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task SeedTestData(AppDbContext db)
    {
        db.Set<ResourceType>().AddRange(TestDataSet.ResourceTypes);
        await db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));

        db.Set<Resource>().AddRange(TestDataSet.Resources);
        await db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));

        db.Entities.AddRange(TestDataSet.Entities);
        db.Assignments.AddRange(TestDataSet.Assignments);
        db.Delegations.AddRange(TestDataSet.Delegations);
        db.AssignmentPackages.AddRange(TestDataSet.AssignmentPackages);
        db.DelegationPackages.AddRange(TestDataSet.DelegationPackages);
        db.Set<AssignmentResource>().AddRange(TestDataSet.AssignmentResources);
        db.Set<AssignmentInstance>().AddRange(TestDataSet.AssignmentInstances);
        db.Set<DelegationResource>().AddRange(TestDataSet.DelegationResources);

        await db.SaveChangesAsync(new Altinn.AccessMgmt.PersistenceEF.Extensions.AuditValues(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest));
    }

    [Fact]
    public async Task GetConnectionsFromOthers_Petter_ReturnsBakerJohnsenWithSubUnits()
    {
        var orgId = TestDataSet.GetEntity("Regnskaperne").Id;
        var personId = TestDataSet.GetEntity("Petter").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);

        var baker = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Oslo").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Bergen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Kristiansand").Id);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_Gunnar_ReturnsBakerJohnsenWithSubUnits()
    {
        var orgId = TestDataSet.GetEntity("Regnskaperne").Id;
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);

        var baker = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Oslo").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Bergen").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Baker Johnsen - Kristiansand").Id);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_Nina_ReturnsSkrikFrisorOnly()
    {
        var orgId = TestDataSet.GetEntity("Skrik Frisør").Id;
        var personId = TestDataSet.GetEntity("Nina").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_William_ReturnsRegnskaperneWithOsloSubUnit()
    {
        var orgId = TestDataSet.GetEntity("Revi").Id;
        var personId = TestDataSet.GetEntity("William").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);

        var main = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);
        Assert.Contains(main.Connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne - Oslo").Id);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_Terje_ReturnsRegnskaperneWithOsloSubUnit()
    {
        var orgId = TestDataSet.GetEntity("Revi").Id;
        var personId = TestDataSet.GetEntity("Terje").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            EnrichEntities = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            ExcludeDeleted = false,
            EnrichPackageResources = false
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);
        var connections = DtoMapper.ConvertFromOthers(dbResult, false);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == orgId);

        Assert.Single<ConnectionDto>(connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);

        var baker = connections.Single(t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne").Id);
        Assert.Contains(baker.Connections, t => t.Party.Id == TestDataSet.GetEntity("Regnskaperne - Oslo").Id);
    }

    [Fact]
    public async Task GetConnections_KeyRoleEnabled_ReturnsBakerConnectionForPetter()
    {
        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { TestDataSet.GetEntity("Baker Johnsen").Id },
            ToIds = new[] { TestDataSet.GetEntity("Petter").Id },
            IncludeKeyRole = true
        };

        var result = await _query.GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.Any(), "Expected a connections, but none were found.");
    }

    [Fact]
    public async Task GetConnections_KeyRoleDisabled_ReturnsNoBakerConnectionForPetter()
    {
        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { TestDataSet.GetEntity("Baker Johnsen").Id },
            ToIds = new[] { TestDataSet.GetEntity("Petter").Id },
            IncludeKeyRole = false
        };

        var result = await _query.GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, ct: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.Any(), "Expected no connections, but some were found.");
    }

    [Fact]
    public async Task SeededEntities_QueryByRefId_ReturnsBakerJohnsen()
    {
        var petter = await _db.Entities.AsNoTracking().SingleAsync(t => t.RefId == "ORG-01", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(petter);
        Assert.Equal("Baker Johnsen", petter.Name);
    }

    [Fact]
    public async Task SeededEntityTypes_QueryOrganization_ReturnsOrganisasjon()
    {
        var orgType = await _db.EntityTypes.AsNoTracking().SingleAsync(t => t.Id == EntityTypeConstants.Organization, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(orgType);
        Assert.Equal("Organisasjon", orgType.Name);
    }

    [Fact]
    public async Task KeyRole_IksWithDeltakerDeltAnsvar_IsNotIncluded()
    {
        var personId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;
        var iksId = TestDataSet.GetEntity("IKS Selskap 1").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(dbResult, r =>
            r.FromId == iksId);
    }

    [Fact]
    public async Task KeyRole_NonIksWithDeltakerDeltAnsvar_IsIncluded()
    {
        var personId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;
        var nonIksId = TestDataSet.GetEntity("Non-IKS Selskap").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(dbResult, r =>
            r.FromId == nonIksId &&
            r.RoleId == RoleConstants.ParticipantSharedResponsibility.Id &&
            r.ViaRoleId == RoleConstants.ManagingDirector.Id &&
            r.Reason == ConnectionReason.KeyRole);
    }

    [Fact]
    public async Task KeyRole_IksWithDifferentKeyRole_IsIncluded()
    {
        var personId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;
        var iksId = TestDataSet.GetEntity("IKS Selskap 2").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(dbResult, r =>
            r.FromId == iksId &&
            r.RoleId == RoleConstants.Accountant.Id &&
            r.ViaRoleId == RoleConstants.ManagingDirector.Id &&
            r.Reason == ConnectionReason.KeyRole);
    }

    [Fact]
    public async Task HasConnection_IksWithDeltakerDeltAnsvarRole_ReturnsFalse()
    {
        var fromId = TestDataSet.GetEntity("IKS Selskap 1").Id;
        var toId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.KeyRole]);

        Assert.False(result);
        Assert.Null(reason);
    }

    [Fact]
    public async Task HasConnection_NonIksWithDeltakerDeltAnsvarRole_ReturnsKeyRole()
    {
        var fromId = TestDataSet.GetEntity("Non-IKS Selskap").Id;
        var toId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.KeyRole]);

        Assert.True(result);
        Assert.Equal(ConnectionReason.KeyRole, reason);
    }

    [Fact]
    public async Task HasConnection_IksWithDifferentRole_ReturnsKeyRole()
    {
        var fromId = TestDataSet.GetEntity("IKS Selskap 2").Id;
        var toId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.KeyRole]);

        Assert.True(result);
        Assert.Equal(ConnectionReason.KeyRole, reason);
    }

    [Fact]
    public async Task KeyRole_ToOthers_IksWithDeltakerDeltAnsvar_IsNotIncluded()
    {
        var iksId = TestDataSet.GetEntity("IKS Selskap 1").Id;
        var personId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { iksId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(dbResult, r =>
            r.ToId == personId &&
            r.RoleId == RoleConstants.ParticipantSharedResponsibility.Id &&
            r.Reason == ConnectionReason.KeyRole);
    }

    [Fact]
    public async Task KeyRole_ToOthers_ViaIksWithDeltakerDeltAnsvar_IsNotIncluded()
    {
        var iksId = TestDataSet.GetEntity("ABC IKS").Id;
        var orgId = TestDataSet.GetEntity("Kommune 2").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { iksId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(dbResult, r =>
            r.ToId == orgId &&
            r.RoleId == RoleConstants.Auditor.Id &&
            r.Reason == ConnectionReason.KeyRole);  
    }

    [Fact]
    public async Task KeyRole_FromOthers_ViaIksWithDeltakerDeltAnsvar_IsNotIncluded()
    {
        var orgId = TestDataSet.GetEntity("Kommune 2").Id;
        var iksId = TestDataSet.GetEntity("ABC IKS").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { orgId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(dbResult, r =>
            r.FromId == iksId &&
            r.RoleId == RoleConstants.Auditor.Id);
    }

    [Fact]
    public async Task HasConnection_ViaIksWithDeltakerDeltAnsvarRole_ReturnsFalse()
    {
        var fromId = TestDataSet.GetEntity("ABC IKS").Id;
        var toId = TestDataSet.GetEntity("Kommune 2").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.KeyRole]);

        Assert.False(result);
        Assert.Null(reason);
    }

    [Fact]
    public async Task KeyRole_ToOthers_NonIksWithDeltakerDeltAnsvar_IsIncluded()
    {
        var nonIksId = TestDataSet.GetEntity("Non-IKS Selskap").Id;
        var personId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { nonIksId },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(dbResult, r =>
            r.ToId == personId &&
            r.RoleId == RoleConstants.ParticipantSharedResponsibility.Id &&
            r.Reason == ConnectionReason.KeyRole);
    }    

    [Fact]
    public async Task KeyRole_ToOthers_IksWithDifferentRole_IsIncluded()
    {
        var iksId2 = TestDataSet.GetEntity("IKS Selskap 2").Id;
        var personId = TestDataSet.GetEntity("Rådmann Kommune 1").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { iksId2 },
            IncludeKeyRole = true,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(dbResult, r =>
            r.ToId == personId &&
            r.RoleId == RoleConstants.Accountant.Id &&
            r.Reason == ConnectionReason.KeyRole);
    }

    #region HasConnection - Assignment and Delegation

    [Fact]
    public async Task HasConnection_DirectAssignment_ReturnsAssignment()
    {
        var fromId = TestDataSet.GetEntity("Regnskaperne").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.Assignment]);

        Assert.True(result);
        Assert.Equal(ConnectionReason.Assignment, reason);
    }

    [Fact]
    public async Task HasConnection_NoAssignment_ReturnsFalse()
    {
        var fromId = TestDataSet.GetEntity("Baker Johnsen").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.Assignment]);

        Assert.False(result);
        Assert.Null(reason);
    }

    [Fact]
    public async Task HasConnection_Delegation_ReturnsDelegation()
    {
        var fromId = TestDataSet.GetEntity("Baker Johnsen").Id;
        var toId = TestDataSet.GetEntity("Gunnar").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.Delegation]);

        Assert.True(result);
        Assert.Equal(ConnectionReason.Delegation, reason);
    }

    [Fact]
    public async Task HasConnection_NoDelegation_ReturnsFalse()
    {
        var fromId = TestDataSet.GetEntity("Skrik Frisør").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.Delegation]);

        Assert.False(result);
        Assert.Null(reason);
    }

    #endregion

    #region HasConnection - Hierarchy

    [Fact]
    public async Task HasConnection_Hierarchy_SubunitToPersonViaMainUnit_ReturnsHierarchy()
    {
        // Regnskaperne - Oslo is a subunit of Regnskaperne, which has an assignment to Petter
        // Hierarchy: assignment from Regnskaperne to Petter, sub-entity Regnskaperne - Oslo (Id) has ParentId = Regnskaperne
        var fromId = TestDataSet.GetEntity("Regnskaperne - Oslo").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.Hierarchy]);

        Assert.True(result);
        Assert.Equal(ConnectionReason.Hierarchy, reason);
    }

    [Fact]
    public async Task HasConnection_Hierarchy_NoRelation_ReturnsFalse()
    {
        var fromId = TestDataSet.GetEntity("Skrik Frisør - Byporten").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, [ConnectionReason.Hierarchy]);

        Assert.False(result);
        Assert.Null(reason);
    }

    #endregion

    #region AuthorizedParties - FromOthers with packages

    [Fact]
    public async Task GetConnectionsFromOthers_WithPackages_ReturnsPackagesForDelegation()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludePackages = true,
            EnrichEntities = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Gunnar has delegation from Baker Johnsen via Regnskaperne with 3 packages
        var bakerConnection = dbResult.Where(r => r.FromId == TestDataSet.GetEntity("Baker Johnsen").Id && r.Reason == ConnectionReason.Delegation).ToList();
        Assert.NotEmpty(bakerConnection);

        var packagesOnDelegation = bakerConnection.SelectMany(c => c.Packages ?? []).ToList();
        Assert.Contains(packagesOnDelegation, p => p.Id == PackageConstants.AccountantWithSigningRights.Id);
        Assert.Contains(packagesOnDelegation, p => p.Id == PackageConstants.AccountantSalary.Id);
        Assert.Contains(packagesOnDelegation, p => p.Id == PackageConstants.AccountantWithoutSigningRights.Id);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_WithPackages_ReturnsAssignmentPackage()
    {
        var personId = TestDataSet.GetEntity("Nina").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludePackages = true,
            EnrichEntities = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Nina has assignment package AOrderSystem from Skrik Frisør (Rightholder role)
        var skrikConnection = dbResult.Where(r => r.FromId == TestDataSet.GetEntity("Skrik Frisør").Id && r.Reason == ConnectionReason.Assignment).ToList();
        Assert.NotEmpty(skrikConnection);

        var packagesOnAssignment = skrikConnection.SelectMany(c => c.Packages ?? []).ToList();
        Assert.Contains(packagesOnAssignment, p => p.Id == PackageConstants.AOrderSystem.Id);
    }

    #endregion

    #region PIP - FromOthers with packages, no entity enrichment

    [Fact]
    public async Task GetConnectionsFromOthers_PipScenario_NoEntityEnrichment_StillReturnsPackages()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludePackages = true,
            EnrichEntities = false,
            EnrichPackageResources = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Should still return connections and packages even without entity enrichment
        Assert.NotEmpty(dbResult);
        var bakerConnection = dbResult.Where(r => r.FromId == TestDataSet.GetEntity("Baker Johnsen").Id && r.Reason == ConnectionReason.Delegation).ToList();
        Assert.NotEmpty(bakerConnection);

        var packagesOnDelegation = bakerConnection.SelectMany(c => c.Packages ?? []).ToList();
        Assert.NotEmpty(packagesOnDelegation);
    }

    #endregion

    #region ToOthers direction

    [Fact]
    public async Task GetConnectionsToOthers_Regnskaperne_ReturnsPetterAndGunnar()
    {
        var orgId = TestDataSet.GetEntity("Regnskaperne").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { orgId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            EnrichEntities = true,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(dbResult, r => r.ToId == TestDataSet.GetEntity("Petter").Id);
        Assert.Contains(dbResult, r => r.ToId == TestDataSet.GetEntity("Gunnar").Id);
    }

    #endregion

    #region ENK/Innehaver

    [Fact]
    public async Task GetConnectionsFromOthers_EnkInnehaver_ReturnsInnehaverAsConnection()
    {
        // ENK Frisør has Innehaver -> Ole, and Accountant role to Regnskaperne
        // So when querying FromOthers for Petter (who is DagligLeder of Regnskaperne),
        // the ENK/Innehaver logic should include Ole as a connection via ENK Frisør
        var personId = TestDataSet.GetEntity("Petter").Id;
        var oleId = TestDataSet.GetEntity("Ole ENK Innehaver").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            EnrichEntities = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        Assert.Contains(dbResult, r => r.FromId == oleId);
    }

    #endregion

    #region RoleMap connections

    [Fact]
    public async Task GetConnectionsFromOthers_RoleMapConnectionsAppear()
    {
        // Petter has ManagingDirector from Regnskaperne. ManagingDirector has RoleMaps in static data (e.g. to A0212, A0236, etc.)
        var personId = TestDataSet.GetEntity("Petter").Id;
        var regnskaperneId = TestDataSet.GetEntity("Regnskaperne").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Should have direct assignment AND rolemap results from Regnskaperne
        var directAssignment = dbResult.Where(r => r.FromId == regnskaperneId && r.Reason == ConnectionReason.Assignment).ToList();
        Assert.NotEmpty(directAssignment);

        var roleMapConnections = dbResult.Where(r => r.FromId == regnskaperneId && r.Reason == ConnectionReason.RoleMap).ToList();
        Assert.NotEmpty(roleMapConnections);
        Assert.All(roleMapConnections, r => Assert.True(r.IsRoleMap));
    }

    #endregion

    #region IncludeResources

    [Fact]
    public async Task GetConnectionsFromOthers_IncludeResources_ReturnsResources()
    {
        var personId = TestDataSet.GetEntity("Nina").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeResources = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Nina has a Rightholder assignment from Skrik Frisør with an AssignmentResource
        var skrikRightholder = dbResult.Where(r =>
            r.FromId == TestDataSet.GetEntity("Skrik Frisør").Id &&
            r.RoleId == RoleConstants.Rightholder.Id).ToList();
        Assert.NotEmpty(skrikRightholder);

        var resources = skrikRightholder.SelectMany(c => c.Resources ?? []).ToList();
        Assert.Contains(resources, r => r.Id == TestDataSet.TestResourceId);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_WithoutIncludeResources_DoesNotReturnResources()
    {
        var personId = TestDataSet.GetEntity("Nina").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeResources = false,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        var skrikRightholder = dbResult.Where(r =>
            r.FromId == TestDataSet.GetEntity("Skrik Frisør").Id &&
            r.RoleId == RoleConstants.Rightholder.Id).ToList();
        Assert.NotEmpty(skrikRightholder);

        var resources = skrikRightholder.SelectMany(c => c.Resources ?? []).ToList();
        Assert.Empty(resources);
    }

    #endregion

    #region IncludeDelegationResources

    [Fact]
    public async Task GetConnectionsFromOthers_IncludeDelegationResources_ReturnsDelegationResources()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = true,
            IncludeResources = true,
            IncludeDelegationResources = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Gunnar has a delegation from Baker Johnsen via Regnskaperne with a DelegationResource
        var delegationConnections = dbResult.Where(r => r.DelegationId.HasValue).ToList();
        Assert.NotEmpty(delegationConnections);

        var resources = delegationConnections.SelectMany(c => c.Resources ?? []).ToList();
        Assert.Contains(resources, r => r.Id == TestDataSet.DelegationTestResourceId);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_IncludeDelegationFalse_DoesNotReturnDelegationConnections()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeResources = true,
            IncludeDelegationResources = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // With IncludeDelegation = false, no delegation connections should appear
        var delegationConnections = dbResult.Where(r => r.DelegationId.HasValue).ToList();
        Assert.Empty(delegationConnections);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_IncludeDelegationResourcesFalse_DoesNotReturnDelegationResources()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = true,
            IncludeResources = true,
            IncludeDelegationResources = false,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Delegation connections exist but should not have resources loaded
        var delegationConnections = dbResult.Where(r => r.DelegationId.HasValue).ToList();
        Assert.NotEmpty(delegationConnections);

        var resources = delegationConnections.SelectMany(c => c.Resources ?? []).ToList();
        Assert.Empty(resources);
    }

    #endregion

    #region IncludeInstances

    [Fact]
    public async Task GetConnectionsFromOthers_IncludeInstances_ReturnsInstances()
    {
        var personId = TestDataSet.GetEntity("Nina").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeInstances = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        var skrikRightholder = dbResult.Where(r =>
            r.FromId == TestDataSet.GetEntity("Skrik Frisør").Id &&
            r.RoleId == RoleConstants.Rightholder.Id &&
            r.Reason == ConnectionReason.Assignment).ToList();
        Assert.NotEmpty(skrikRightholder);

        var instances = skrikRightholder.SelectMany(c => c.Instances ?? []).ToList();
        Assert.Contains(instances, i => i.InstanceId == "instance-001");
    }

    #endregion

    #region PackageIds filtering

    [Fact]
    public async Task GetConnectionsFromOthers_PackageIdsFilter_OnlyMatchingPackagesReturned()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludePackages = true,
            PackageIds = new[] { PackageConstants.AccountantSalary.Id },
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Only connections with AccountantSalary package should remain
        var delegationConnections = dbResult.Where(r => r.Reason == ConnectionReason.Delegation).ToList();
        Assert.NotEmpty(delegationConnections);

        var allPackages = delegationConnections.SelectMany(c => c.Packages ?? []).ToList();
        Assert.All(allPackages, p => Assert.Equal(PackageConstants.AccountantSalary.Id, p.Id));
    }

    [Fact]
    public async Task GetConnectionsFromOthers_PackageIdsFilter_RemovesConnectionsWithNoMatchingPackages()
    {
        var personId = TestDataSet.GetEntity("Gunnar").Id;
        var nonExistentPackageId = Guid.NewGuid();

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = true,
            IncludePackages = true,
            PackageIds = new[] { nonExistentPackageId },
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Delegation connections should be removed since no packages matched
        var delegationConnections = dbResult.Where(r => r.Reason == ConnectionReason.Delegation).ToList();
        Assert.Empty(delegationConnections);
    }

    #endregion

    #region Supplier role exclusion

    [Fact]
    public async Task GetConnectionsFromOthers_SupplierRoleAlwaysExcluded()
    {
        var consumerId = TestDataSet.GetEntity("Consumer Corp").Id;
        var supplierId = TestDataSet.GetEntity("Supplier Corp").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { supplierId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Consumer Corp has added Supplier Corp as their supplier, but the connection shoul not be included in ConnectionQuery results
        Assert.DoesNotContain(dbResult, r => r.FromId == consumerId && r.RoleId == RoleConstants.Supplier.Id);
    }

    [Fact]
    public async Task GetConnectionsToOthers_SupplierRoleAlwaysExcluded()   
    {
        var consumerId = TestDataSet.GetEntity("Consumer Corp").Id;
        var supplierId = TestDataSet.GetEntity("Supplier Corp").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { consumerId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        // Consumer Corp has added Supplier Corp as their supplier, but the connection shoul not be included in ConnectionQuery results
        Assert.DoesNotContain(dbResult, r => r.ToId == supplierId && r.RoleId == RoleConstants.Supplier.Id);
    }

    #endregion

    #region ToOthers from subunit

    [Fact]
    public async Task GetConnectionsToOthers_FromSubunit_IncludesMainUnitConnections()
    {
        // Baker Johnsen - Oslo is a subunit of Baker Johnsen
        // Baker Johnsen has assignment to Regnskaperne (Accountant)
        // When querying ToOthers from Oslo with IncludeMainUnitConnections=true, 
        // we should see Regnskaperne as a connection via the mainunit
        var osloId = TestDataSet.GetEntity("Baker Johnsen - Oslo").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { osloId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        // Should have hierarchy connections from the mainunit
        var hierarchyConnections = dbResult.Where(r => r.Reason == ConnectionReason.Hierarchy).ToList();
        Assert.NotEmpty(hierarchyConnections);
        Assert.Contains(hierarchyConnections, r => r.ToId == TestDataSet.GetEntity("Regnskaperne").Id);
    }

    #endregion

    #region ToOthers with delegations

    [Fact]
    public async Task GetConnectionsToOthers_WithDelegation_ReturnsClientDelegations()
    {
        // Baker Johnsen has delegation via Regnskaperne to Gunnar (Agent)
        var bakerId = TestDataSet.GetEntity("Baker Johnsen").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { bakerId },
            IncludeKeyRole = false,
            IncludeDelegation = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        var delegations = dbResult.Where(r => r.Reason == ConnectionReason.Delegation).ToList();
        Assert.NotEmpty(delegations);
        Assert.Contains(delegations, r => r.ToId == TestDataSet.GetEntity("Gunnar").Id);
    }

    [Fact]
    public async Task GetConnectionsToOthers_DelegationDisabled_NoDelegationResults()
    {
        var bakerId = TestDataSet.GetEntity("Baker Johnsen").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { bakerId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        var delegations = dbResult.Where(r => r.Reason == ConnectionReason.Delegation).ToList();
        Assert.Empty(delegations);
    }

    #endregion

    #region IncludeSubConnections = false

    [Fact]
    public async Task GetConnectionsFromOthers_IncludeSubConnectionsFalse_NoHierarchyChildren()
    {
        var personId = TestDataSet.GetEntity("Petter").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludeSubConnections = false,
            EnrichEntities = true,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // Baker Johnsen should appear but without sub-entities (Oslo, Bergen, Kristiansand)
        // When IncludeSubConnections=false, child nesting is skipped AND Innehaver connections are skipped
        var hierarchyResults = dbResult.Where(r => r.Reason == ConnectionReason.Hierarchy).ToList();
        Assert.Empty(hierarchyResults);
    }

    #endregion

    #region IncludeMainUnitConnections = false (ToOthers)

    [Fact]
    public async Task GetConnectionsToOthers_IncludeMainUnitConnectionsFalse_NoMainUnitAssignments()
    {
        var osloId = TestDataSet.GetEntity("Baker Johnsen - Oslo").Id;

        var filter = new ConnectionQueryFilter
        {
            FromIds = new[] { osloId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeMainUnitConnections = false,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsToOthersAsync(filter, TestContext.Current.CancellationToken);

        // Oslo subunit has no direct assignments, so with mainunit connections disabled, expect empty
        var hierarchyConnections = dbResult.Where(r => r.Reason == ConnectionReason.Hierarchy).ToList();
        Assert.Empty(hierarchyConnections);
    }

    #endregion

    #region HasConnection priority ordering

    [Fact]
    public async Task HasConnection_MultipleReasons_ReturnsHighestPriority()
    {
        // Regnskaperne has a direct assignment to Petter (ManagingDirector)
        // So checking with both Assignment and KeyRole should return Assignment (higher priority)
        var fromId = TestDataSet.GetEntity("Regnskaperne").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId);

        Assert.True(result);
        Assert.Equal(ConnectionReason.Assignment, reason);
    }

    [Fact]
    public async Task HasConnection_EmptyReasons_ReturnsFalse()
    {
        var fromId = TestDataSet.GetEntity("Regnskaperne").Id;
        var toId = TestDataSet.GetEntity("Petter").Id;

        var (result, reason) = await _query.HasConnection(fromId, toId, []);

        Assert.False(result);
        Assert.Null(reason);
    }

    #endregion

    #region AppControlledRightholder exclusion

    [Fact]
    public async Task GetConnectionsFromOthers_AppControlledRightholder_ExcludedByDefault()
    {
        var personId = TestDataSet.GetEntity("Nina").Id;
        var skrikId = TestDataSet.GetEntity("Skrik Frisør").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeAppControlledInstances = false,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // AppControlledRightholder role should be excluded by default
        Assert.DoesNotContain(dbResult, r => r.FromId == skrikId && r.RoleId == RoleConstants.AppControlledRightholder.Id);
    }

    [Fact]
    public async Task GetConnectionsFromOthers_AppControlledRightholder_IncludedWhenRequested()
    {
        var personId = TestDataSet.GetEntity("Nina").Id;
        var skrikId = TestDataSet.GetEntity("Skrik Frisør").Id;

        var filter = new ConnectionQueryFilter
        {
            ToIds = new[] { personId },
            IncludeKeyRole = false,
            IncludeDelegation = false,
            IncludeAppControlledInstances = true,
            EnrichEntities = false,
        };

        var dbResult = await _query.GetConnectionsFromOthersAsync(filter, TestContext.Current.CancellationToken);

        // AppControlledRightholder role should be included when explicitly requested
        Assert.Contains(dbResult, r => r.FromId == skrikId && r.RoleId == RoleConstants.AppControlledRightholder.Id);
    }

    #endregion

    [Theory]
    [MemberData(nameof(GetFilterCombinations))]
    public async Task GetConnectionsAsync_AllFilterFlagCombinations_DoesNotThrow(bool[] flags, bool useSingle)
    {
        var filter = new ConnectionQueryFilter
        {
            FromIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            ToIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            RoleIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            PackageIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            ResourceIds = useSingle ? new[] { Guid.NewGuid() } : new[] { Guid.NewGuid(), Guid.NewGuid() },
            IncludeDelegation = flags[0],
            IncludeKeyRole = flags[1],
            IncludePackages = flags[2],
            IncludeResources = flags[3],
            EnrichEntities = flags[4],
            EnrichPackageResources = flags[5],
            ExcludeDeleted = flags[6]
        };

        // Every flag combination must produce a valid query that runs against the database
        // without the query builder throwing. Any combination that faults is a regression.
        var exception = await Record.ExceptionAsync(() => _query.GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, ct: TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    public static IEnumerable<object[]> GetFilterCombinations()
    {
        var combinations = new List<object[]>();
        var total = 1 << 7;

        foreach (var useSingle in new[] { true, false })
        {
            for (int i = 0; i < total; i++)
            {
                var flags = new bool[7];
                for (int j = 0; j < 7; j++)
                {
                    flags[j] = (i & (1 << j)) != 0;
                }

                combinations.Add(new object[] { flags, useSingle });
            }
        }

        return combinations;
    }
}

internal static class TestDataSet
{
    internal static Entity GetEntity(string name)
    {
        return Entities.First(x => x.Name == name);
    }

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable SA1401 // Fields should be private
    internal static List<Entity> Entities = new()
    #pragma warning restore SA1401 // Fields should be private
    #pragma warning restore IDE0028 // Simplify collection initialization
    {
        new Entity() { Id = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), Name = "Baker Johnsen", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-01", ParentId = null, RefId = "ORG-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7006-8da4-55c17e07f4d6"), Name = "Baker Johnsen - Oslo", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-01-01", ParentId = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), RefId = "ORG-01-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7fb8-915b-c13b5c1c5264"), Name = "Baker Johnsen - Bergen", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-01-02", ParentId = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), RefId = "ORG-01-02" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-72da-afe2-daa4fe116965"), Name = "Baker Johnsen - Kristiansand", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-01-03", ParentId = Guid.Parse("0195efb8-7c80-773a-ba5c-d81b5345f4fa"), RefId = "ORG-01-03" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-71fa-9a7e-624ca11ebaf7"), Name = "Regnskaperne", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-02", ParentId = null, RefId = "ORG-02" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7127-8101-095fd6ffe7bf"), Name = "Regnskaperne - Oslo", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-02-01", ParentId = Guid.Parse("0195efb8-7c80-71fa-9a7e-624ca11ebaf7"), RefId = "ORG-02-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7fac-9944-8a51f26c2633"), Name = "Skrik Frisør", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-03", ParentId = null, RefId = "ORG-03" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7225-bf3e-0aab8e9f59c3"), Name = "Skrik Frisør - Byporten", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-03-01", ParentId = Guid.Parse("0195efb8-7c80-7fac-9944-8a51f26c2633"), RefId = "ORG-03-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7b40-8415-0c0fe952dd2f"), Name = "Skrik Frisør - CC Vest", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-03-02", ParentId = Guid.Parse("0195efb8-7c80-7fac-9944-8a51f26c2633"), RefId = "ORG-03-02" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7c46-96b3-b2b1b0b51895"), Name = "Revi", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-04", ParentId = null, RefId = "ORG-04" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-713c-b504-84984779378c"), Name = "Revi - Stavanger", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.BEDR, OrganizationIdentifier = "ORG-04-01", ParentId = Guid.Parse("0195efb8-7c80-7c46-96b3-b2b1b0b51895"), RefId = "ORG-04-01" },

        new Entity() { Id = Guid.Parse("0195efb8-7c80-7d22-b320-2eed10bc8a84"), Name = "Petter", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "01018412345", RefId = "01018412345", DateOfBirth = DateOnly.Parse("1984-01-01") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7414-87e1-3b3b9799161d"), Name = "Gunnar", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "02018412345", RefId = "02018412345", DateOfBirth = DateOnly.Parse("1984-01-02") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-77d3-b35b-4bbf7d207dc2"), Name = "Nina", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "03018412345", RefId = "03018412345", DateOfBirth = DateOnly.Parse("1984-01-03") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7d2c-b030-1d1c205d5400"), Name = "Kari", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "04018412345", RefId = "04018412345", DateOfBirth = DateOnly.Parse("1984-01-04") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7911-bf6c-67713e9fe4f8"), Name = "William", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "05018412345", RefId = "05018412345", DateOfBirth = DateOnly.Parse("1984-01-05") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-706b-9e0e-87f73c5b3ed0"), Name = "Terje", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "06018412345", RefId = "06018412345", DateOfBirth = DateOnly.Parse("1984-01-06") },

        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000001"), Name = "NUF International Corp", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.NUF, OrganizationIdentifier = "ORG-05", ParentId = null, RefId = "ORG-05" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000002"), Name = "Non-NUF Client AS", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-06", ParentId = null, RefId = "ORG-06" },

        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000003"), Name = "Siri", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "07018412345", RefId = "07018412345", DateOfBirth = DateOnly.Parse("1984-01-07") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000004"), Name = "Lars", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "08018412345", RefId = "08018412345", DateOfBirth = DateOnly.Parse("1984-01-08") },

        // ENK/Innehaver test entities
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000020"), Name = "ENK Frisør", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.ENK, OrganizationIdentifier = "ORG-ENK-01", ParentId = null, RefId = "ORG-ENK-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000021"), Name = "Ole ENK Innehaver", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "10018412345", RefId = "10018412345", DateOfBirth = DateOnly.Parse("1984-01-10") },

        // Supplier/Consumer test entities
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000040"), Name = "Supplier Corp", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-SUPP-01", ParentId = null, RefId = "ORG-SUPP-01" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000041"), Name = "Consumer Corp", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-CONS-01", ParentId = null, RefId = "ORG-CONS-01" },

        // Deleted entity for ExcludeDeleted tests
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000030"), Name = "Deleted Org", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "ORG-DEL-01", ParentId = null, RefId = "ORG-DEL-01", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow.AddDays(-10) },

        // IKS keyrole test entities
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7839-8b2a-8794bf7a929d"), Name = "Rådmann Kommune 1", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "09018412345", RefId = "09018412345", DateOfBirth = DateOnly.Parse("1984-01-09") },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7e40-9ea1-4362ea2aefa5"), Name = "Kommune 1", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.KOMM, OrganizationIdentifier = "605001750", ParentId = null, RefId = "605001750" },
        new Entity() { Id = Guid.Parse("019f64dc-314f-714f-9443-b2ab79b52309"), Name = "Kommune 2", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.KOMM, OrganizationIdentifier = "605001807", ParentId = null, RefId = "605001807" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-76ee-a7d9-f2d76ac7187b"), Name = "IKS Selskap 1", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.IKS, OrganizationIdentifier = "605001769", ParentId = null, RefId = "605001769" },
        new Entity() { Id = Guid.Parse("019f21f7-aa45-75ce-a26f-a4a06f238604"), Name = "IKS Selskap 2", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.IKS, OrganizationIdentifier = "605001785", ParentId = null, RefId = "605001785" },
        new Entity() { Id = Guid.Parse("019f64ec-6579-7579-b961-3277e49293cf"), Name = "ABC IKS", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.IKS, OrganizationIdentifier = "605001815", ParentId = null, RefId = "605001815" },
        new Entity() { Id = Guid.Parse("019f64e1-2ad1-7ad1-b839-9a4265b79b5a"), Name = "DEF IKS", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.IKS, OrganizationIdentifier = "605001793", ParentId = null, RefId = "605001793" },
        new Entity() { Id = Guid.Parse("0195efb8-7c80-7a77-8203-e7ca159053d0"), Name = "Non-IKS Selskap", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.AS, OrganizationIdentifier = "605001777", ParentId = null, RefId = "605001777" },
    };

#pragma warning disable SA1401 // Fields should be private
    internal static List<Assignment> Assignments = new()
    #pragma warning restore SA1401 // Fields should be private
    {
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7f47-9046-14e33e571f55"), FromId = Entities.First(t => t.Name == "Baker Johnsen").Id, ToId = Entities.First(t => t.Name == "Regnskaperne").Id, RoleId = RoleConstants.Accountant }, // Regnskapsfører
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7c9d-9191-9e6c04c4683c"), FromId = Entities.First(t => t.Name == "Regnskaperne").Id, ToId = Entities.First(t => t.Name == "Petter").Id, RoleId = RoleConstants.ManagingDirector }, // Daglig leder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7f16-b2b7-b539522eeddb"), FromId = Entities.First(t => t.Name == "Regnskaperne").Id, ToId = Entities.First(t => t.Name == "Gunnar").Id, RoleId = RoleConstants.Agent }, // Agent
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7129-b367-cab290cc6e11"), FromId = Entities.First(t => t.Name == "Skrik Frisør").Id, ToId = Entities.First(t => t.Name == "Nina").Id, RoleId = RoleConstants.ManagingDirector }, // Daglig leder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a3c-8f8b-f97e069f6aa0"), FromId = Entities.First(t => t.Name == "Skrik Frisør").Id, ToId = Entities.First(t => t.Name == "Nina").Id, RoleId = RoleConstants.Rightholder }, // Rettighetsholder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-717a-85a5-0e127816ed69"), FromId = Entities.First(t => t.Name == "Regnskaperne").Id, ToId = Entities.First(t => t.Name == "Revi").Id, RoleId = RoleConstants.Auditor }, // Revisor 
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-708d-beec-13141fd0fae3"), FromId = Entities.First(t => t.Name == "Revi").Id, ToId = Entities.First(t => t.Name == "William").Id, RoleId = RoleConstants.ManagingDirector }, // Daglig leder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-759a-8a59-ce8abd161c1b"), FromId = Entities.First(t => t.Name == "Revi").Id, ToId = Entities.First(t => t.Name == "Terje").Id, RoleId = RoleConstants.ChairOfTheBoard }, // Styreleder
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7311-a2df-21bd3352ba24"), FromId = Entities.First(t => t.Name == "Terje").Id, ToId = Entities.First(t => t.Name == "Terje").Id, RoleId = RoleConstants.PrivatePerson }, // Privatperson (self)
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000010"), FromId = Entities.First(t => t.Name == "NUF International Corp").Id, ToId = Entities.First(t => t.Name == "Regnskaperne").Id, RoleId = RoleConstants.BusinessManager }, // Forretningsfører from NUF client
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000011"), FromId = Entities.First(t => t.Name == "Non-NUF Client AS").Id, ToId = Entities.First(t => t.Name == "Regnskaperne").Id, RoleId = RoleConstants.BusinessManager }, // Forretningsfører from non-NUF client
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000012"), FromId = Entities.First(t => t.Name == "NUF International Corp").Id, ToId = Entities.First(t => t.Name == "Siri").Id, RoleId = RoleConstants.ContactPersonNUF }, // Kontaktperson NUF
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000013"), FromId = Entities.First(t => t.Name == "NUF International Corp").Id, ToId = Entities.First(t => t.Name == "Lars").Id, RoleId = RoleConstants.NorwegianRepresentativeForeignEntity }, // Norsk representant for utenlandsk foretak

        // Supplier role assignment (should always be excluded from results)
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000041"), FromId = Entities.First(t => t.Name == "Consumer Corp").Id, ToId = Entities.First(t => t.Name == "Supplier Corp").Id, RoleId = RoleConstants.Supplier }, // Supplier role

        // AppControlledRightholder assignment for Nina from Skrik Frisør
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000042"), FromId = Entities.First(t => t.Name == "Skrik Frisør").Id, ToId = Entities.First(t => t.Name == "Nina").Id, RoleId = RoleConstants.AppControlledRightholder }, // App-controlled rightholder

        // ENK/Innehaver test assignments
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000022"), FromId = Entities.First(t => t.Name == "ENK Frisør").Id, ToId = Entities.First(t => t.Name == "Ole ENK Innehaver").Id, RoleId = RoleConstants.Innehaver }, // Innehaver
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000023"), FromId = Entities.First(t => t.Name == "ENK Frisør").Id, ToId = Entities.First(t => t.Name == "Regnskaperne").Id, RoleId = RoleConstants.Accountant }, // Regnskapsfører

        // Deleted org assignment
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000031"), FromId = Entities.First(t => t.Name == "Deleted Org").Id, ToId = Entities.First(t => t.Name == "Petter").Id, RoleId = RoleConstants.ManagingDirector }, // Direct assignment from deleted org

        // IKS keyrole test assignments
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7604-8518-2ddb45c89645"), FromId = Entities.First(t => t.Name == "Kommune 1").Id, ToId = Entities.First(t => t.Name == "Rådmann Kommune 1").Id, RoleId = RoleConstants.ManagingDirector }, // daglig-leder (different keyrole)
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7532-9427-433385e63908"), FromId = Entities.First(t => t.Name == "IKS Selskap 1").Id, ToId = Entities.First(t => t.Name == "Kommune 1").Id, RoleId = RoleConstants.ParticipantSharedResponsibility }, // deltaker-delt-ansvar (keyrole) for IKS Selskap
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7b15-bfc8-4d596bc18d01"), FromId = Entities.First(t => t.Name == "IKS Selskap 2").Id, ToId = Entities.First(t => t.Name == "Kommune 1").Id, RoleId = RoleConstants.Accountant }, // Regnskapsfører  (not deltaker-delt-ansvar) for IKS Selskap
        new Assignment() { Id = Guid.Parse("0195efb8-7c80-7d79-87ce-f1dcdf1bba79"), FromId = Entities.First(t => t.Name == "Non-IKS Selskap").Id, ToId = Entities.First(t => t.Name == "Kommune 1").Id, RoleId = RoleConstants.ParticipantSharedResponsibility }, // deltaker-delt-ansvar (keyrole) for Non IKS Selskap
        
        new Assignment() { Id = Guid.Parse("019f64f7-f575-7575-98f8-474361196943"), FromId = Entities.First(t => t.Name == "ABC IKS").Id, ToId = Entities.First(t => t.Name == "Kommune 1").Id, RoleId = RoleConstants.ParticipantSharedResponsibility }, // deltaker-delt-ansvar (keyrole) for IKS ABC to Kommune 1
        new Assignment() { Id = Guid.Parse("019f64f8-1c28-7c28-9a1c-daba52081006"), FromId = Entities.First(t => t.Name == "ABC IKS").Id, ToId = Entities.First(t => t.Name == "Kommune 2").Id, RoleId = RoleConstants.ParticipantSharedResponsibility }, // deltaker-delt-ansvar (keyrole) for IKS ABC to Kommune 2
        new Assignment() { Id = Guid.Parse("019f64f8-39fc-79fc-b40f-65f25fa98b92"), FromId = Entities.First(t => t.Name == "ABC IKS").Id, ToId = Entities.First(t => t.Name == "DEF IKS").Id, RoleId = RoleConstants.Auditor }, // Revisor for IKS ABC to DEF IKS
        new Assignment() { Id = Guid.Parse("019f64f8-5c9c-7c9c-9a6e-f2e4f28e439a"), FromId = Entities.First(t => t.Name == "DEF IKS").Id, ToId = Entities.First(t => t.Name == "Kommune 2").Id, RoleId = RoleConstants.ParticipantSharedResponsibility }, // deltaker-delt-ansvar (keyrole) for IKS DEF to Kommune 2
    };

    internal static Assignment GetAssignment(string fromName, string toName, Guid roleId)
    {
        var fromEntity = Entities.First(t => t.Name == fromName);
        var toEntity = Entities.First(t => t.Name == toName);

        return Assignments.First(t => t.FromId == fromEntity.Id && t.ToId == toEntity.Id);
    }

#pragma warning disable SA1401 // Fields should be private
    internal static List<Delegation> Delegations = new()
    #pragma warning restore SA1401 // Fields should be private
    {
        new Delegation() { Id = Guid.Parse("0195efb8-7c80-7bda-9f72-4ef5c897f619"), FromId = GetAssignment("Baker Johnsen", "Regnskaperne", RoleConstants.Accountant).Id, ToId = GetAssignment("Regnskaperne", "Gunnar", RoleConstants.Agent).Id, FacilitatorId = GetEntity("Regnskaperne").Id },
        new Delegation() { Id = Guid.Parse("0195efb8-7c80-7743-b054-e946a946c44a"), FromId = GetAssignment("Baker Johnsen", "Regnskaperne", RoleConstants.Accountant).Id, ToId = GetAssignment("Regnskaperne", "Revi", RoleConstants.Auditor).Id, FacilitatorId = GetEntity("Regnskaperne").Id },
    };

    internal static Delegation GetDelegation(string fromEntityName, string toEntityName)
    {
        var fromEntity = Entities.First(t => t.Name == fromEntityName);
        var toEntity = Entities.First(t => t.Name == toEntityName);

        return Delegations.First(t =>
            Assignments.Any(fa => fa.Id == t.FromId && fa.FromId == fromEntity.Id) &&
            Assignments.Any(ta => ta.Id == t.ToId && ta.ToId == toEntity.Id));
    }

#pragma warning disable SA1401 // Fields should be private
    internal static List<AssignmentPackage> AssignmentPackages = new()
    #pragma warning restore SA1401 // Fields should be private
    {
        // AOrderSystem package assigned to Nina's Rightholder assignment from Skrik Frisør
        new AssignmentPackage() { AssignmentId = Assignments.First(t => t.FromId == GetEntity("Skrik Frisør").Id && t.ToId == GetEntity("Nina").Id && t.RoleId == RoleConstants.Rightholder).Id, PackageId = PackageConstants.AOrderSystem.Id },
    };

#pragma warning disable SA1401 // Fields should be private
    internal static List<DelegationPackage> DelegationPackages = new()
    #pragma warning restore SA1401 // Fields should be private
    {
        // Accountant packages delegated from Baker Johnsen to Gunnar via Regnskaperne
        new DelegationPackage() { DelegationId = Guid.Parse("0195efb8-7c80-7bda-9f72-4ef5c897f619"), PackageId = PackageConstants.AccountantWithSigningRights.Id },
        new DelegationPackage() { DelegationId = Guid.Parse("0195efb8-7c80-7bda-9f72-4ef5c897f619"), PackageId = PackageConstants.AccountantSalary.Id },
        new DelegationPackage() { DelegationId = Guid.Parse("0195efb8-7c80-7bda-9f72-4ef5c897f619"), PackageId = PackageConstants.AccountantWithoutSigningRights.Id },
    };

    internal static readonly Guid TestResourceTypeId = Guid.Parse("0195efb8-7c80-7a01-8001-000000000050");
    internal static readonly Guid TestResourceId = Guid.Parse("0195efb8-7c80-7a01-8001-000000000051");
    internal static readonly Guid DelegationTestResourceId = Guid.Parse("0195efb8-7c80-7a01-8001-000000000060");

#pragma warning disable SA1401 // Fields should be private
    internal static List<ResourceType> ResourceTypes = new()
#pragma warning restore SA1401 // Fields should be private
    {
        new ResourceType() { Id = TestResourceTypeId, Name = "TestResourceType" },
    };

#pragma warning disable SA1401 // Fields should be private
    internal static List<Resource> Resources = new()
#pragma warning restore SA1401 // Fields should be private
    {
        new Resource() { Id = TestResourceId, Name = "Test Bakery Resource", Description = "Test resource", RefId = "test-bakery-resource", TypeId = TestResourceTypeId, ProviderId = ProviderConstants.Altinn3.Id },
        new Resource() { Id = DelegationTestResourceId, Name = "Delegation Test Resource", Description = "Test delegation resource", RefId = "delegation-test-resource", TypeId = TestResourceTypeId, ProviderId = ProviderConstants.Altinn3.Id },
    };

#pragma warning disable SA1401 // Fields should be private
    internal static List<DelegationResource> DelegationResources = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Resource delegated via Baker Johnsen -> Gunnar delegation
        new DelegationResource() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000061"), DelegationId = Guid.Parse("0195efb8-7c80-7bda-9f72-4ef5c897f619"), ResourceId = DelegationTestResourceId, AssignmentResourceId = Guid.Parse("0195efb8-7c80-7a01-8001-000000000052") },
    };

#pragma warning disable SA1401 // Fields should be private
    internal static List<AssignmentResource> AssignmentResources = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Resource assigned to Nina's Rightholder assignment from Skrik Frisør
        new AssignmentResource() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000052"), AssignmentId = Assignments.First(t => t.FromId == GetEntity("Skrik Frisør").Id && t.ToId == GetEntity("Nina").Id && t.RoleId == RoleConstants.Rightholder).Id, ResourceId = TestResourceId },
    };

#pragma warning disable SA1401 // Fields should be private
    internal static List<AssignmentInstance> AssignmentInstances = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Instance assigned to Nina's Rightholder assignment from Skrik Frisør
        new AssignmentInstance() { Id = Guid.Parse("0195efb8-7c80-7a01-8001-000000000053"), AssignmentId = Assignments.First(t => t.FromId == GetEntity("Skrik Frisør").Id && t.ToId == GetEntity("Nina").Id && t.RoleId == RoleConstants.Rightholder).Id, ResourceId = TestResourceId, InstanceId = "instance-001", InstanceSourceTypeId = InstanceSourceTypeConstants.EndUser.Id },
    };
}

/*



0195efb8-7c80-7bda-9f72-4ef5c897f619
0195efb8-7c80-7743-b054-e946a946c44a
0195efb8-7c80-7311-a2df-21bd3352ba24
0195efb8-7c80-7839-8b2a-8794bf7a929d
0195efb8-7c80-7e40-9ea1-4362ea2aefa5
0195efb8-7c80-76ee-a7d9-f2d76ac7187b
0195efb8-7c80-7a77-8203-e7ca159053d0
0195efb8-7c80-7b15-bfc8-4d596bc18d01
0195efb8-7c80-7604-8518-2ddb45c89645
0195efb8-7c80-7532-9427-433385e63908
0195efb8-7c80-7d79-87ce-f1dcdf1bba79
0195efb8-7c80-73a9-a8e4-78615f974d92
0195efb8-7c80-7d62-920b-051135c76e45
0195efb8-7c80-7fbc-aa99-25524087073b
0195efb8-7c80-7293-b522-68cf3998f2ee
0195efb8-7c80-79b4-ae6e-32ffcf783b5e
0195efb8-7c80-7eca-85f2-434a235c966f
0195efb8-7c80-7f61-9aa2-4fbe5e208795
0195efb8-7c80-71b9-a6ea-d4bc8f68d681
0195efb8-7c80-7fd5-9676-2eb0d1b62c8e
0195efb8-7c80-7dfa-aaab-2c1023643bba
0195efb8-7c80-71b4-8ddb-cb457780038a
0195efb8-7c80-79ea-8db3-56ef39cbef4f
0195efb8-7c80-7c54-b2e9-5e219227c565
0195efb8-7c80-70b0-8731-671de4b2eeef
*/

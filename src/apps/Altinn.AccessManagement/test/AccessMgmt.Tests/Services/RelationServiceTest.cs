using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RelationService"/> — pure Moq, no database.
/// </summary>
public class RelationServiceTest
{
    private static (RelationService svc, Mock<IRelationRepository> compactRepo, Mock<IRelationPermissionRepository> fullRepo) MakeSut()
    {
        var compactRepo = new Mock<IRelationRepository>();
        var fullRepo = new Mock<IRelationPermissionRepository>();
        compactRepo.Setup(r => r.CreateFilterBuilder()).Returns(new GenericFilterBuilder<CompactRelation>());
        fullRepo.Setup(r => r.CreateFilterBuilder()).Returns(new GenericFilterBuilder<Relation>());
        return (new RelationService(compactRepo.Object, fullRepo.Object), compactRepo, fullRepo);
    }

    private static QueryResponse<T> Resp<T>(params T[] items) => new() { Data = items };

    private static CompactEntity Party(Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Name = "Party" };

    private static CompactRole Role(Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Code = "role" };

    private static CompactPackage Package(Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Urn = "urn:pkg" };

    private static CompactResource Resource(Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Value = "res" };

    private static ExtRelation FullRelation(CompactEntity from, CompactEntity to, string reason = "Direct", CompactPackage package = null, CompactResource resource = null) =>
        new() { From = from, To = to, Role = Role(), Reason = reason, Package = package, Resource = resource };

    private static ExtCompactRelation CompactRelation(CompactEntity from, CompactEntity to, string reason = "Direct") =>
        new() { From = from, To = to, Role = Role(), Reason = reason };

    private static void SetupFullGetExtended(Mock<IRelationPermissionRepository> repo, QueryResponse<ExtRelation> result)
    {
        repo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    private static void SetupCompactGetExtended(Mock<IRelationRepository> repo, QueryResponse<ExtCompactRelation> result)
    {
        repo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    #region GetConnectionsToOthers — full (ExtRelation)

    [Fact]
    public async Task GetConnectionsToOthers_Full_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, _, fullRepo) = MakeSut();
        SetupFullGetExtended(fullRepo, Resp<ExtRelation>());

        var result = await svc.GetConnectionsToOthers(Guid.NewGuid(), packageId: null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectionsToOthers_Full_DirectRelation_GroupsByToParty()
    {
        var (svc, _, fullRepo) = MakeSut();
        var from = Party();
        var to = Party();
        var relation = FullRelation(from, to, "Direct");
        SetupFullGetExtended(fullRepo, Resp(relation));

        var result = await svc.GetConnectionsToOthers(from.Id, packageId: null);

        result.Should().ContainSingle(r => r.Party.Id == to.Id);
    }

    [Fact]
    public async Task GetConnectionsToOthers_Full_NonDirectRelation_IsExcludedFromDirectResults()
    {
        var (svc, _, fullRepo) = MakeSut();
        var from = Party();
        var to = Party();
        var relation = FullRelation(from, to, "Delegated");
        SetupFullGetExtended(fullRepo, Resp(relation));

        var result = await svc.GetConnectionsToOthers(from.Id, packageId: null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectionsToOthers_Full_TwoRelationsSameTo_CollectsDistinctRoles()
    {
        var (svc, _, fullRepo) = MakeSut();
        var from = Party();
        var to = Party();
        var roleId = Guid.NewGuid();
        var r1 = new ExtRelation { From = from, To = to, Role = new CompactRole { Id = roleId }, Reason = "Direct" };
        var r2 = new ExtRelation { From = from, To = to, Role = new CompactRole { Id = roleId }, Reason = "Direct" };
        SetupFullGetExtended(fullRepo, Resp(r1, r2));

        var result = await svc.GetConnectionsToOthers(from.Id, packageId: null);

        result.Should().ContainSingle().Which.Roles.Should().ContainSingle();
    }

    #endregion

    #region GetConnectionsFromOthers — full (ExtRelation)

    [Fact]
    public async Task GetConnectionsFromOthers_Full_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, _, fullRepo) = MakeSut();
        SetupFullGetExtended(fullRepo, Resp<ExtRelation>());

        var result = await svc.GetConnectionsFromOthers(Guid.NewGuid(), packageId: null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectionsFromOthers_Full_WithRelation_GroupsByFromParty()
    {
        var (svc, _, fullRepo) = MakeSut();
        var from = Party();
        var to = Party();
        var relation = FullRelation(from, to, "Direct");
        SetupFullGetExtended(fullRepo, Resp(relation));

        var result = await svc.GetConnectionsFromOthers(to.Id, packageId: null);

        result.Should().ContainSingle(r => r.Party.Id == from.Id);
    }

    #endregion

    #region GetConnectionsToOthers — compact (ExtCompactRelation)

    [Fact]
    public async Task GetConnectionsToOthers_Compact_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, compactRepo, _) = MakeSut();
        SetupCompactGetExtended(compactRepo, Resp<ExtCompactRelation>());

        var result = await svc.GetConnectionsToOthers(Guid.NewGuid(), null, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectionsToOthers_Compact_DirectRelation_GroupsByToParty()
    {
        var (svc, compactRepo, _) = MakeSut();
        var from = Party();
        var to = Party();
        SetupCompactGetExtended(compactRepo, Resp(CompactRelation(from, to, "Direct")));

        var result = await svc.GetConnectionsToOthers(from.Id, null, null, CancellationToken.None);

        result.Should().ContainSingle(r => r.Party.Id == to.Id);
    }

    [Fact]
    public async Task GetConnectionsToOthers_Compact_NonDirectRelation_IsExcluded()
    {
        var (svc, compactRepo, _) = MakeSut();
        var from = Party();
        var to = Party();
        SetupCompactGetExtended(compactRepo, Resp(CompactRelation(from, to, "Delegated")));

        var result = await svc.GetConnectionsToOthers(from.Id, null, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    #endregion

    #region GetConnectionsFromOthers — compact (ExtCompactRelation)

    [Fact]
    public async Task GetConnectionsFromOthers_Compact_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, compactRepo, _) = MakeSut();
        SetupCompactGetExtended(compactRepo, Resp<ExtCompactRelation>());

        var result = await svc.GetConnectionsFromOthers(Guid.NewGuid(), null, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectionsFromOthers_Compact_WithRelation_GroupsByFromParty()
    {
        var (svc, compactRepo, _) = MakeSut();
        var from = Party();
        var to = Party();
        SetupCompactGetExtended(compactRepo, Resp(CompactRelation(from, to)));

        var result = await svc.GetConnectionsFromOthers(to.Id, null, null, CancellationToken.None);

        result.Should().ContainSingle(r => r.Party.Id == from.Id);
    }

    #endregion

    #region GetPackagePermissionsFromOthers

    [Fact]
    public async Task GetPackagePermissionsFromOthers_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, _, fullRepo) = MakeSut();
        SetupFullGetExtended(fullRepo, Resp<ExtRelation>());

        var result = await svc.GetPackagePermissionsFromOthers(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPackagePermissionsFromOthers_RelationWithNullPackage_ReturnsEmpty()
    {
        var (svc, _, fullRepo) = MakeSut();
        var relation = FullRelation(Party(), Party(), "Direct", package: null);
        SetupFullGetExtended(fullRepo, Resp(relation));

        var result = await svc.GetPackagePermissionsFromOthers(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPackagePermissionsFromOthers_WithPackage_GroupsByPackageId()
    {
        var (svc, _, fullRepo) = MakeSut();
        var pkg = Package();
        var r1 = FullRelation(Party(), Party(), "Direct", package: pkg);
        var r2 = FullRelation(Party(), Party(), "Direct", package: pkg);
        SetupFullGetExtended(fullRepo, Resp(r1, r2));

        var result = await svc.GetPackagePermissionsFromOthers(Guid.NewGuid());

        result.Should().ContainSingle(p => p.Package.Id == pkg.Id)
            .Which.Permissions.Should().HaveCount(2);
    }

    #endregion

    #region GetPackagePermissionsToOthers

    [Fact]
    public async Task GetPackagePermissionsToOthers_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, _, fullRepo) = MakeSut();
        SetupFullGetExtended(fullRepo, Resp<ExtRelation>());

        var result = await svc.GetPackagePermissionsToOthers(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPackagePermissionsToOthers_WithPackage_GroupsByPackageId()
    {
        var (svc, _, fullRepo) = MakeSut();
        var pkg = Package();
        SetupFullGetExtended(fullRepo, Resp(FullRelation(Party(), Party(), "Direct", package: pkg)));

        var result = await svc.GetPackagePermissionsToOthers(Guid.NewGuid());

        result.Should().ContainSingle(p => p.Package.Id == pkg.Id);
    }

    #endregion

    #region GetResourcePermissionsFromOthers

    [Fact]
    public async Task GetResourcePermissionsFromOthers_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, _, fullRepo) = MakeSut();
        SetupFullGetExtended(fullRepo, Resp<ExtRelation>());

        var result = await svc.GetResourcePermissionsFromOthers(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourcePermissionsFromOthers_NullPackageOrResource_IsExcluded()
    {
        var (svc, _, fullRepo) = MakeSut();
        // resource non-null but package null → excluded by `r.Resource is { } && r.Package is { }`
        var relation = FullRelation(Party(), Party(), "Direct", package: null, resource: Resource());
        SetupFullGetExtended(fullRepo, Resp(relation));

        var result = await svc.GetResourcePermissionsFromOthers(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourcePermissionsFromOthers_WithResourceAndPackage_GroupsByResourceId()
    {
        var (svc, _, fullRepo) = MakeSut();
        var pkg = Package();
        var res = Resource();
        SetupFullGetExtended(fullRepo, Resp(FullRelation(Party(), Party(), "Direct", package: pkg, resource: res)));

        var result = await svc.GetResourcePermissionsFromOthers(Guid.NewGuid());

        result.Should().ContainSingle(r => r.Resource.Id == res.Id);
    }

    #endregion

    #region GetResourcePermissionsToOthers

    [Fact]
    public async Task GetResourcePermissionsToOthers_EmptyResult_ReturnsEmptySequence()
    {
        var (svc, _, fullRepo) = MakeSut();
        SetupFullGetExtended(fullRepo, Resp<ExtRelation>());

        var result = await svc.GetResourcePermissionsToOthers(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResourcePermissionsToOthers_WithResourceAndPackage_GroupsByResourceId()
    {
        var (svc, _, fullRepo) = MakeSut();
        var pkg = Package();
        var res = Resource();
        SetupFullGetExtended(fullRepo, Resp(FullRelation(Party(), Party(), "Direct", package: pkg, resource: res)));

        var result = await svc.GetResourcePermissionsToOthers(Guid.NewGuid());

        result.Should().ContainSingle(r => r.Resource.Id == res.Id);
    }

    #endregion

    #region GetAssignablePackagePermissions

    [Fact]
    public async Task GetAssignablePackagePermissions_DelegatesToRepository()
    {
        var (svc, _, fullRepo) = MakeSut();
        var partyId = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var expectedResult = new[] { new PackageDelegationCheck() };
        fullRepo.Setup(r => r.GetAssignableAccessPackages(fromId, partyId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await svc.GetAssignablePackagePermissions(partyId, fromId);

        result.Should().BeEquivalentTo(expectedResult);
    }

    #endregion
}

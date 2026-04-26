using System.Linq.Expressions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RoleService"/> — pure Moq, no database.
/// </summary>
public class RoleServiceTest
{
    private static (RoleService svc, Mock<IRoleRepository> roleRepo, Mock<IRoleLookupRepository> lookupRepo, Mock<IRolePackageRepository> pkgRepo) MakeSut()
    {
        var roleRepo = new Mock<IRoleRepository>();
        var lookupRepo = new Mock<IRoleLookupRepository>();
        var pkgRepo = new Mock<IRolePackageRepository>();
        lookupRepo.Setup(r => r.CreateFilterBuilder()).Returns(new GenericFilterBuilder<RoleLookup>());
        return (new RoleService(roleRepo.Object, lookupRepo.Object, pkgRepo.Object), roleRepo, lookupRepo, pkgRepo);
    }

    private static ExtRole MakeRole(Guid? id = null, string code = "test-code", string name = "Test Role") =>
        new() { Id = id ?? Guid.NewGuid(), Code = code, Name = name, Urn = $"urn:altinn:role:{code}", Description = "desc", IsKeyRole = false };

    private static ExtRoleLookup MakeLookup(Guid roleId, string key, string value) =>
        new() { RoleId = roleId, Key = key, Value = value, Role = new Role { Id = roleId } };

    private static QueryResponse<T> Resp<T>(params T[] items) => new() { Data = items };

    private static void SetupLookupByFilter(Mock<IRoleLookupRepository> repo, QueryResponse<ExtRoleLookup> result)
    {
        repo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    private static void SetupLookupGetAll(Mock<IRoleLookupRepository> repo, QueryResponse<ExtRoleLookup> result)
    {
        repo.Setup(r => r.GetExtended(
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    #region GetById

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var (svc, roleRepo, _, _) = MakeSut();
        roleRepo.Setup(r => r.GetExtended(It.IsAny<Guid>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync((ExtRole)null);

        var result = await svc.GetById(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetById_Found_NoLegacyCode_ReturnsDtoWithNullLegacyFields()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var role = MakeRole();
        roleRepo.Setup(r => r.GetExtended(It.IsAny<Guid>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(role);
        SetupLookupByFilter(lookupRepo, Resp<ExtRoleLookup>());

        var result = await svc.GetById(role.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(role.Id);
        result.Code.Should().Be(role.Code);
        result.LegacyRoleCode.Should().BeNull();
        result.LegacyUrn.Should().BeNull();
    }

    [Fact]
    public async Task GetById_Found_WithLegacyCode_SetsLegacyFields()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var role = MakeRole();
        var lookup = MakeLookup(role.Id, "LegacyCode", "DAGL");
        roleRepo.Setup(r => r.GetExtended(It.IsAny<Guid>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(role);
        SetupLookupByFilter(lookupRepo, Resp(lookup));

        var result = await svc.GetById(role.Id);

        result.LegacyRoleCode.Should().Be("DAGL");
        result.LegacyUrn.Should().Be("urn:altinn:rolecode:DAGL");
    }

    #endregion

    #region GetAll

    [Fact]
    public async Task GetAll_NullResult_ReturnsNull()
    {
        var (svc, roleRepo, _, _) = MakeSut();
        roleRepo.Setup(r => r.GetExtended(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync((QueryResponse<ExtRole>)null);

        var result = await svc.GetAll();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_WithRoles_ReturnsMappedDtos()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var role = MakeRole();
        roleRepo.Setup(r => r.GetExtended(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(Resp(role));
        SetupLookupGetAll(lookupRepo, Resp<ExtRoleLookup>());

        var result = await svc.GetAll();

        result.Should().ContainSingle(r => r.Id == role.Id && r.Code == role.Code);
    }

    [Fact]
    public async Task GetAll_WithLegacyCodeInLookup_SetsLegacyFieldsOnMatchingRole()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var role = MakeRole();
        var lookup = MakeLookup(role.Id, "LegacyCode", "PRIV");
        roleRepo.Setup(r => r.GetExtended(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(Resp(role));
        SetupLookupGetAll(lookupRepo, Resp(lookup));

        var result = await svc.GetAll();

        result.Single().LegacyRoleCode.Should().Be("PRIV");
        result.Single().LegacyUrn.Should().Be("urn:altinn:rolecode:PRIV");
    }

    #endregion

    #region GetByProvider

    [Fact]
    public async Task GetByProvider_NullResult_ReturnsNull()
    {
        var (svc, roleRepo, _, _) = MakeSut();
        roleRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRole, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync((QueryResponse<ExtRole>)null);

        var result = await svc.GetByProvider(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProvider_WithRoles_ReturnsMappedDtos()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var role = MakeRole();
        roleRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRole, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Resp(role));
        SetupLookupGetAll(lookupRepo, Resp<ExtRoleLookup>());

        var result = await svc.GetByProvider(Guid.NewGuid());

        result.Should().ContainSingle(r => r.Id == role.Id);
    }

    #endregion

    #region GetByCode

    [Fact]
    public async Task GetByCode_NullResult_ReturnsNull()
    {
        var (svc, roleRepo, _, _) = MakeSut();
        roleRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRole, string>>>(),
                It.IsAny<string>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync((QueryResponse<ExtRole>)null);

        var result = await svc.GetByCode("daglig-leder");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCode_WithRoles_ReturnsDtosWithMatchingCode()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var role = MakeRole(code: "daglig-leder");
        roleRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRole, string>>>(),
                It.IsAny<string>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Resp(role));
        SetupLookupGetAll(lookupRepo, Resp<ExtRoleLookup>());

        var result = await svc.GetByCode("daglig-leder");

        result.Should().ContainSingle(r => r.Code == "daglig-leder");
    }

    #endregion

    #region GetByKeyValue

    [Fact]
    public async Task GetByKeyValue_EmptyLookupResult_ReturnsNull()
    {
        var (svc, _, lookupRepo, _) = MakeSut();
        SetupLookupByFilter(lookupRepo, Resp<ExtRoleLookup>());

        var result = await svc.GetByKeyValue("LegacyCode", "DAGL");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByKeyValue_Found_ReturnsRoleDtoWithId()
    {
        var (svc, roleRepo, lookupRepo, _) = MakeSut();
        var roleId = Guid.NewGuid();
        var lookup = MakeLookup(roleId, "LegacyCode", "DAGL");
        var role = MakeRole(id: roleId);
        SetupLookupByFilter(lookupRepo, Resp(lookup));
        roleRepo.Setup(r => r.GetExtended(It.IsAny<Guid>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(role);

        var result = await svc.GetByKeyValue("LegacyCode", "DAGL");

        result.Should().NotBeNull();
        result.Id.Should().Be(roleId);
    }

    #endregion

    #region GetLookupKeys

    [Fact]
    public async Task GetLookupKeys_NullResult_ReturnsNull()
    {
        var (svc, _, lookupRepo, _) = MakeSut();
        lookupRepo.Setup(r => r.Get(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync((QueryResponse<RoleLookup>)null);

        var result = await svc.GetLookupKeys();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLookupKeys_WithDuplicateKeys_ReturnsDistinct()
    {
        var (svc, _, lookupRepo, _) = MakeSut();
        lookupRepo.Setup(r => r.Get(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(new QueryResponse<RoleLookup>
            {
                Data = [new RoleLookup { Key = "LegacyCode", Value = "DAGL" }, new RoleLookup { Key = "LegacyCode", Value = "PRIV" }]
            });

        var result = await svc.GetLookupKeys();

        result.Should().ContainSingle().Which.Should().Be("LegacyCode");
    }

    #endregion

    #region GetPackagesForRole

    [Fact]
    public async Task GetPackagesForRole_EmptyAndRoleFound_ReturnsEmptyList()
    {
        var (svc, roleRepo, _, pkgRepo) = MakeSut();
        var roleId = Guid.NewGuid();
        pkgRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRolePackage, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Resp<ExtRolePackage>());
        roleRepo.Setup(r => r.Get(It.IsAny<Guid>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(new Role { Id = roleId });

        var result = await svc.GetPackagesForRole(roleId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPackagesForRole_EmptyAndRoleNotFound_ReturnsNull()
    {
        var (svc, roleRepo, _, pkgRepo) = MakeSut();
        pkgRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRolePackage, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Resp<ExtRolePackage>());
        roleRepo.Setup(r => r.Get(It.IsAny<Guid>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync((Role)null);

        var result = await svc.GetPackagesForRole(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPackagesForRole_WithPackages_ReturnsDtosWithPackage()
    {
        var (svc, _, lookupRepo, pkgRepo) = MakeSut();
        var roleId = Guid.NewGuid();
        var pkgId = Guid.NewGuid();
        var rp = new ExtRolePackage
        {
            RoleId = roleId,
            PackageId = pkgId,
            Role = new Role { Id = roleId, Name = "Test" },
            Package = new Package { Id = pkgId, Name = "TestPkg" }
        };
        pkgRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtRolePackage, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Resp(rp));
        SetupLookupByFilter(lookupRepo, Resp<ExtRoleLookup>());

        var result = await svc.GetPackagesForRole(roleId);

        result.Should().ContainSingle(p => p.Package.Id == pkgId);
    }

    #endregion
}

using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

namespace Altinn.AccessMgmt.Core.Tests.Utils.Mappers;

/// <summary>
/// Pure-logic unit tests for the connection-query overloads of
/// <see cref="DtoMapper"/>: <c>ConvertToOthers</c>, <c>ConvertFromOthers</c>,
/// <c>ConvertSubConnections*</c>, <c>ConvertPackages</c>, <c>ConvertResources</c>,
/// <c>ConvertToAgentDto</c>, and all <c>Extract*</c> instance methods.
/// No database or container required.
/// </summary>
public class DtoMapperConnectionQueryTest
{
    // ── known EntityTypeConstants / EntityVariantConstants ids ────────────────
    private static readonly Guid OrganizationTypeId = new("8c216e2f-afdd-4234-9ba2-691c727bb33d");
    private static readonly Guid UtbgVariantId = new("99a54a28-52d3-4608-9298-94081bb3f3d2");

    // ── helpers ───────────────────────────────────────────────────────────────
    private static Entity MakeEntity(string name = "Corp") => new()
    {
        Id = Guid.NewGuid(),
        TypeId = OrganizationTypeId,
        VariantId = UtbgVariantId,
        Name = name,
    };

    private static Role MakeRole(string code = "dagl") => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Urn = $"altinn:role:{code}",
        LegacyUrn = $"urn:altinn:role:{code}",
    };

    private static ConnectionQueryPackage MakeQueryPackage(string urn = "altinn:pkg:test") => new()
    {
        Id = Guid.NewGuid(),
        Urn = urn,
        AreaId = Guid.NewGuid(),
    };

    private static ConnectionQueryResource MakeQueryResource(string name = "Res") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        RefId = "ref-1",
    };

    private static ConnectionQueryExtendedRecord MakeRecord(
        Entity from,
        Entity to,
        Role role,
        ConnectionReason reason = ConnectionReason.Assignment,
        Guid? viaId = null,
        List<ConnectionQueryPackage>? packages = null,
        List<ConnectionQueryResource>? resources = null) =>
        new()
        {
            FromId = from.Id,
            ToId = to.Id,
            RoleId = role.Id,
            ViaId = viaId,
            Reason = reason,
            From = from,
            To = to,
            Role = role,
            Packages = packages ?? [],
            Resources = resources ?? [],
        };

    /// <summary>Creates a <see cref="Connection"/> (old model, Reason is string).</summary>
    private static Connection MakeConnection(
        Entity from,
        Entity to,
        Role role,
        string reason = "Direct",
        Entity? via = null,
        Package? package = null) => new()
        {
            FromId = from.Id,
            ToId = to.Id,
            RoleId = role.Id,
            ViaId = via?.Id,
            Reason = reason,
            From = from,
            To = to,
            Role = role,
            Via = via,
            Package = package,
        };

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertToOthers
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertToOthers_EmptyInput_ReturnsEmptyList()
    {
        DtoMapper.ConvertToOthers([]).Should().BeEmpty();
    }

    [Fact]
    public void ConvertToOthers_SingleAssignmentRecord_ReturnsMappedConnectionDto()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.Assignment);

        var result = DtoMapper.ConvertToOthers([rec]);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
        result[0].Roles.Should().ContainSingle(r => r.Id == role.Id);
    }

    [Fact]
    public void ConvertToOthers_KeyRoleReason_ExcludedWhenGetSingleFalse()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.KeyRole);

        DtoMapper.ConvertToOthers([rec], getSingle: false).Should().BeEmpty();
    }

    [Fact]
    public void ConvertToOthers_KeyRoleReason_IncludedWhenGetSingleTrue()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.KeyRole);

        var result = DtoMapper.ConvertToOthers([rec], getSingle: true);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
    }

    [Fact]
    public void ConvertToOthers_DelegationReason_ExcludedWhenGetSingleFalse()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.Delegation);

        DtoMapper.ConvertToOthers([rec], getSingle: false).Should().BeEmpty();
    }

    [Fact]
    public void ConvertToOthers_MultipleRecordsSameToId_DeduplicatesRoles()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole("dagl");
        var rec1 = MakeRecord(from, to, role);
        var rec2 = MakeRecord(from, to, role);

        var result = DtoMapper.ConvertToOthers([rec1, rec2]);

        result.Should().ContainSingle();
        result[0].Roles.Should().ContainSingle();
    }

    [Fact]
    public void ConvertToOthers_WithViaId_BuildsSubConnections()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var sub = MakeEntity("Sub");
        var role = MakeRole();
        var direct = MakeRecord(from, to, role, ConnectionReason.Assignment);
        var via = MakeRecord(from, sub, role, ConnectionReason.KeyRole, viaId: to.Id);

        var result = DtoMapper.ConvertToOthers([direct, via]);

        result.Should().ContainSingle();
        result[0].Connections.Should().ContainSingle(c => c.Party!.Id == sub.Id);
    }

    [Fact]
    public void ConvertToOthers_PackagesOnRecord_MappedToDto()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var pkg = MakeQueryPackage("altinn:pkg:test");
        var rec = MakeRecord(from, to, role, packages: [pkg]);

        var result = DtoMapper.ConvertToOthers([rec]);

        result.Should().ContainSingle();
        result[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertFromOthers
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertFromOthers_EmptyInput_ReturnsEmptyList()
    {
        DtoMapper.ConvertFromOthers([]).Should().BeEmpty();
    }

    [Fact]
    public void ConvertFromOthers_SingleRecord_ReturnsMappedConnectionDtoFromFrom()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.Assignment);

        var result = DtoMapper.ConvertFromOthers([rec]);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
        result[0].Roles.Should().ContainSingle(r => r.Id == role.Id);
    }

    [Fact]
    public void ConvertFromOthers_HierarchyReason_ExcludedWhenGetSingleFalse()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.Hierarchy);

        DtoMapper.ConvertFromOthers([rec], getSingle: false).Should().BeEmpty();
    }

    [Fact]
    public void ConvertFromOthers_HierarchyReason_IncludedWhenGetSingleTrue()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var rec = MakeRecord(from, to, role, ConnectionReason.Hierarchy);

        var result = DtoMapper.ConvertFromOthers([rec], getSingle: true);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
    }

    [Fact]
    public void ConvertFromOthers_MultipleRecordsSameFromId_DeduplicatesRoles()
    {
        var from = MakeEntity("Shared From");
        var to1 = MakeEntity("To1");
        var to2 = MakeEntity("To2");
        var role = MakeRole("dagl");
        var rec1 = MakeRecord(from, to1, role);
        var rec2 = MakeRecord(from, to2, role);

        var result = DtoMapper.ConvertFromOthers([rec1, rec2]);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
        result[0].Roles.Should().ContainSingle();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertSubConnectionsToOthers
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertSubConnectionsToOthers_NullInput_ReturnsEmptyList()
    {
        DtoMapper.ConvertSubConnectionsToOthers(null!).Should().BeEmpty();
    }

    [Fact]
    public void ConvertSubConnectionsToOthers_EmptyInput_ReturnsEmptyList()
    {
        DtoMapper.ConvertSubConnectionsToOthers([]).Should().BeEmpty();
    }

    [Fact]
    public void ConvertSubConnectionsToOthers_GroupsByToId_MapsRolesAndPackages()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role1 = MakeRole("dagl");
        var role2 = MakeRole("regn");
        var pkg = MakeQueryPackage();
        var rec1 = MakeRecord(from, to, role1, packages: [pkg]);
        var rec2 = MakeRecord(from, to, role2);

        var result = DtoMapper.ConvertSubConnectionsToOthers([rec1, rec2]);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
        result[0].Roles.Should().HaveCount(2);
        result[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertSubConnectionsFromOthers
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertSubConnectionsFromOthers_EmptyInput_ReturnsEmptyList()
    {
        DtoMapper.ConvertSubConnectionsFromOthers([]).Should().BeEmpty();
    }

    [Fact]
    public void ConvertSubConnectionsFromOthers_GroupsByFromId_MapsRolesAndResources()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role1 = MakeRole("dagl");
        var role2 = MakeRole("regn");
        var res = MakeQueryResource("MyRes");
        var rec1 = MakeRecord(from, to, role1, resources: [res]);
        var rec2 = MakeRecord(from, to, role2);

        var result = DtoMapper.ConvertSubConnectionsFromOthers([rec1, rec2]);

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
        result[0].Roles.Should().HaveCount(2);
        result[0].Resources.Should().ContainSingle(r => r.Id == res.Id);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertPackages
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertPackages_SinglePackage_ReturnsSinglePackagePermissionDtoWithPermission()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var pkg = MakeQueryPackage("altinn:pkg:invoicing");
        var rec = MakeRecord(from, to, role, packages: [pkg]);

        var result = DtoMapper.ConvertPackages([rec]);

        result.Should().ContainSingle();
        result[0].Package!.Id.Should().Be(pkg.Id);
        result[0].Permissions.Should().ContainSingle();
    }

    [Fact]
    public void ConvertPackages_SamePackageTwoRecords_DeduplicatesPackageCreatesMultiplePermissions()
    {
        var from = MakeEntity();
        var to1 = MakeEntity("To1");
        var to2 = MakeEntity("To2");
        var role = MakeRole();
        var pkg = MakeQueryPackage();
        var rec1 = MakeRecord(from, to1, role, packages: [pkg]);
        var rec2 = MakeRecord(from, to2, role, packages: [pkg]);

        var result = DtoMapper.ConvertPackages([rec1, rec2]);

        result.Should().ContainSingle();
        result[0].Permissions.Should().HaveCount(2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertResources (from ConnectionQueryExtendedRecord)
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertResources_FromExtendedRecord_SingleResource_MapsWithPermission()
    {
        var from = MakeEntity();
        var to = MakeEntity();
        var role = MakeRole();
        var res = MakeQueryResource("Invoice Resource");
        var rec = MakeRecord(from, to, role, resources: [res]);

        var result = DtoMapper.ConvertResources([rec]);

        result.Should().ContainSingle();
        result[0].Resource!.Id.Should().Be(res.Id);
        result[0].Permissions.Should().ContainSingle();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ConvertToAgentDto
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ConvertToAgentDto_SingleAgent_MapsPartyRolesAndPackages()
    {
        var from = MakeEntity("Client");
        var to = MakeEntity("Agent");
        var role = MakeRole("dagl");
        var pkg = MakeQueryPackage("altinn:pkg:accounting");
        var rec = MakeRecord(from, to, role, packages: [pkg]);

        var result = DtoMapper.ConvertToAgentDto([rec]);

        result.Should().ContainSingle();
        result[0].Agent!.Id.Should().Be(to.Id);
        result[0].Access.Should().ContainSingle(a => a.Role!.Id == role.Id);
        result[0].Access[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }

    [Fact]
    public void ConvertToAgentDto_TwoAgents_ReturnsTwoDtos()
    {
        var from = MakeEntity("Client");
        var to1 = MakeEntity("Agent1");
        var to2 = MakeEntity("Agent2");
        var role = MakeRole();
        var rec1 = MakeRecord(from, to1, role);
        var rec2 = MakeRecord(from, to2, role);

        var result = DtoMapper.ConvertToAgentDto([rec1, rec2]);

        result.Should().HaveCount(2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Convert(ConnectionQueryPackage) / Convert(ConnectionQueryResource)
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void Convert_ConnectionQueryPackage_MapsIdUrnAndAreaId()
    {
        var pkg = MakeQueryPackage("altinn:pkg:test");

        var dto = DtoMapper.Convert(pkg);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(pkg.Id);
        dto.Urn.Should().Be("altinn:pkg:test");
        dto.AreaId.Should().Be(pkg.AreaId);
    }

    [Fact]
    public void Convert_ConnectionQueryResource_MapsIdNameAndRefId()
    {
        var res = MakeQueryResource("Invoice Resource");

        var dto = DtoMapper.Convert(res);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(res.Id);
        dto.Name.Should().Be("Invoice Resource");
        dto.RefId.Should().Be("ref-1");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Extract* instance methods (DtoMapper.cs — use old Connection model)
    // ══════════════════════════════════════════════════════════════════════════
    [Fact]
    public void ExtractRelationDtoToOthers_OnlyDirectConnectionsReturned()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role = MakeRole();
        var direct = MakeConnection(from, to, role, "Direct");
        var nonDirect = MakeConnection(from, to, role, "Delegated");

        var result = mapper.ExtractRelationDtoToOthers([direct, nonDirect]).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
    }

    [Fact]
    public void ExtractRelationDtoToOthers_IncludeSubConnections_PopulatesConnections()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var sub = MakeEntity("Sub");
        var role = MakeRole();
        var direct = MakeConnection(from, to, role, "Direct");
        var subConn = MakeConnection(from, sub, role, "Delegated", via: to);

        var result = mapper.ExtractRelationDtoToOthers([direct, subConn], includeSubConnections: true).ToList();

        result.Should().ContainSingle();
        result[0].Connections.Should().ContainSingle(c => c.Party!.Id == sub.Id);
    }

    [Fact]
    public void ExtractRelationDtoFromOthers_AllConnectionsGroupedByFromId()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("SharedFrom");
        var to1 = MakeEntity("To1");
        var to2 = MakeEntity("To2");
        var role = MakeRole();
        var conn1 = MakeConnection(from, to1, role);
        var conn2 = MakeConnection(from, to2, role);

        var result = mapper.ExtractRelationDtoFromOthers([conn1, conn2]).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
    }

    [Fact]
    public void ExtractSubRelationDtoToOthers_FiltersByViaPartyAndNonDirect()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var via = MakeEntity("Via");
        var role = MakeRole();
        var subConn = MakeConnection(from, to, role, "Delegated", via: via);

        var result = mapper.ExtractSubRelationDtoToOthers([subConn], via.Id).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
    }

    [Fact]
    public void ExtractSubRelationDtoToOthers_DirectConnectionsExcluded()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity();
        var to = MakeEntity();
        var via = MakeEntity();
        var role = MakeRole();
        var direct = MakeConnection(from, to, role, "Direct", via: via);

        var result = mapper.ExtractSubRelationDtoToOthers([direct], via.Id).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractSubRelationDtoFromOthers_FiltersByViaPartyAndNonDirect()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var via = MakeEntity("Via");
        var role = MakeRole();
        var subConn = MakeConnection(from, to, role, "Delegated", via: via);

        var result = mapper.ExtractSubRelationDtoFromOthers([subConn], via.Id).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
    }

    [Fact]
    public void ExtractRelationPackageDtoToOthers_DirectConnectionsWithPackagesMapped()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role = MakeRole();
        var pkg = new Package { Id = Guid.NewGuid(), Urn = "altinn:pkg:test", Name = "Test" };
        var conn = MakeConnection(from, to, role, "Direct", package: pkg);

        var result = mapper.ExtractRelationPackageDtoToOthers([conn]).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
        result[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }

    [Fact]
    public void ExtractRelationPackageDtoFromOthers_AllConnectionsGroupedByFromIdWithPackages()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var role = MakeRole();
        var pkg = new Package { Id = Guid.NewGuid(), Urn = "altinn:pkg:from-pkg", Name = "From Pkg" };
        var conn = MakeConnection(from, to, role, package: pkg);

        var result = mapper.ExtractRelationPackageDtoFromOthers([conn]).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
        result[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }

    [Fact]
    public void ExtractSubRelationPackageDtoToOthers_FiltersByViaWithPackages()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var via = MakeEntity("Via");
        var role = MakeRole();
        var pkg = new Package { Id = Guid.NewGuid(), Urn = "altinn:pkg:sub", Name = "Sub Pkg" };
        var subConn = MakeConnection(from, to, role, "Delegated", via: via, package: pkg);

        var result = mapper.ExtractSubRelationPackageDtoToOthers([subConn], via.Id).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(to.Id);
        result[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }

    [Fact]
    public void ExtractSubRelationPackageDtoFromOthers_FiltersByViaWithPackages()
    {
        var mapper = new DtoMapper();
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var via = MakeEntity("Via");
        var role = MakeRole();
        var pkg = new Package { Id = Guid.NewGuid(), Urn = "altinn:pkg:subfrom", Name = "Sub From Pkg" };
        var subConn = MakeConnection(from, to, role, "Delegated", via: via, package: pkg);

        var result = mapper.ExtractSubRelationPackageDtoFromOthers([subConn], via.Id).ToList();

        result.Should().ContainSingle();
        result[0].Party!.Id.Should().Be(from.Id);
        result[0].Packages.Should().ContainSingle(p => p.Id == pkg.Id);
    }
}

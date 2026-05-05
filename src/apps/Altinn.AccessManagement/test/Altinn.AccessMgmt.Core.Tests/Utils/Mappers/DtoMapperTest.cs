using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Tests.Utils.Mappers;

/// <summary>
/// Pure-logic unit tests for all <see cref="Altinn.AccessMgmt.Core.Utils.DtoMapper"/> static
/// and instance methods spread across the partial-class files:
///   DtoMapper.cs, DtoMapper.Simplified.cs, DtoMapperAssignmentDto.cs,
///   DtoMapperAssignmentPackageDto.cs, DtoMapperPermissionDto.cs,
///   DtoMapperDelegationDto.cs, CreateDelegationResponseDtoMapper.cs,
///   DtoMapperRolePackage.cs, DtoMapperAccessPackageDto.cs,
///   DtoMapperAuthorizedPartyDto.cs, RequestMapper.cs
/// </summary>
public class DtoMapperTest
{
    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Known valid EntityType id (Organization) from EntityTypeConstants.</summary>
    private static readonly Guid OrganizationTypeId = new("8c216e2f-afdd-4234-9ba2-691c727bb33d");

    /// <summary>Known valid EntityVariant id (UTBG → Organization) from EntityVariantConstants.</summary>
    private static readonly Guid UtbgVariantId = new("99a54a28-52d3-4608-9298-94081bb3f3d2");

    private static Entity MakeEntity(string? name = "Acme AS") => new()
    {
        Id = Guid.NewGuid(),
        TypeId = OrganizationTypeId,
        VariantId = UtbgVariantId,
        Name = name,
        OrganizationIdentifier = "123456789",
        PartyId = 42,
    };

    private static Role MakeRole(string code = "dagl") => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Urn = $"altinn:role:{code}",
        LegacyUrn = $"urn:altinn:role:{code}",
        Name = "Test Role",
        Description = "desc",
        IsKeyRole = false,
    };

    private static Package MakePackage() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Regnskapsfører",
        Urn = "altinn:package:regnskapsforer",
        Description = "desc",
        IsDelegable = true,
        IsAssignable = true,
        IsAvailableForServiceOwners = false,
    };

    private static Area MakeArea() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Skatt",
        Urn = "altinn:area:skatt",
        Description = "Skatteområde",
        IconUrl = "https://example.com/icon.png",
    };

    private static EntityType MakeEntityType() => new()
    {
        Id = Guid.NewGuid(),
        Name = "TestType",
        ProviderId = Guid.NewGuid(),
    };

    private static EntityVariant MakeVariant() => new()
    {
        Id = Guid.NewGuid(),
        Name = "TestVariant",
        Description = "A variant",
        TypeId = Guid.NewGuid(),
    };

    private static AreaGroup MakeAreaGroup() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Finance",
        Urn = "altinn:areagroup:finance",
        Description = "Finance group",
        EntityType = MakeEntityType(),
    };

    // ── DtoMapper.Convert(Entity) ─────────────────────────────────────────────
    [Fact]
    public void Convert_Entity_MapsAllScalarFields()
    {
        var entity = MakeEntity("Acme AS");
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(entity);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be("Acme AS");
        dto.OrganizationIdentifier.Should().Be("123456789");
        dto.PartyId.Should().Be(42);
    }

    [Fact]
    public void Convert_Entity_TypeAndVariantResolvedFromConstants()
    {
        var entity = MakeEntity();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(entity);

        dto.Type.Should().Be("Organisasjon");
        dto.Variant.Should().Be("UTBG");
    }

    [Fact]
    public void Convert_Entity_NullEntity_ReturnsNull()
    {
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((Entity)null!);
        dto.Should().BeNull();
    }

    [Fact]
    public void Convert_Entity_WithParent_ParentIsMapped()
    {
        var parent = MakeEntity("Parent AS");
        var child = MakeEntity("Child AS");
        child.Parent = parent;

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(child);

        dto.Parent.Should().NotBeNull();
        dto.Parent!.Id.Should().Be(parent.Id);
    }

    [Fact]
    public void Convert_Entity_IsConvertingParent_ParentIsNull()
    {
        var parent = MakeEntity("Parent AS");
        var child = MakeEntity("Child AS");
        child.Parent = parent;

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(child, isConvertingParent: true);

        dto.Parent.Should().BeNull();
    }

    // ── DtoMapper.ConvertCompactRole ──────────────────────────────────────────
    [Fact]
    public void ConvertCompactRole_NonNull_MapsFields()
    {
        var role = MakeRole("dagl");
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertCompactRole(role);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(role.Id);
        dto.Code.Should().Be("dagl");
        dto.Urn.Should().Be("altinn:role:dagl");
        dto.LegacyUrn.Should().Be("urn:altinn:role:dagl");
    }

    [Fact]
    public void ConvertCompactRole_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertCompactRole(null).Should().BeNull();
    }

    // ── DtoMapper.ConvertCompactPackage ───────────────────────────────────────
    [Fact]
    public void ConvertCompactPackage_NonNull_MapsFields()
    {
        var pkg = new ConnectionQueryPackage
        {
            Id = Guid.NewGuid(),
            AreaId = Guid.NewGuid(),
            Urn = "altinn:package:test",
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertCompactPackage(pkg);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(pkg.Id);
        dto.AreaId.Should().Be(pkg.AreaId);
        dto.Urn.Should().Be(pkg.Urn);
    }

    [Fact]
    public void ConvertCompactPackage_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertCompactPackage((ConnectionQueryPackage?)null).Should().BeNull();
    }

    // ── DtoMapperAssignmentDto ─────────────────────────────────────────────────
    [Fact]
    public void Convert_Assignment_MapsFields()
    {
        var assignment = new Assignment { RoleId = Guid.NewGuid(), FromId = Guid.NewGuid(), ToId = Guid.NewGuid() };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(assignment);

        dto.Id.Should().Be(assignment.Id);
        dto.RoleId.Should().Be(assignment.RoleId);
        dto.FromId.Should().Be(assignment.FromId);
        dto.ToId.Should().Be(assignment.ToId);
    }

    // ── DtoMapperAssignmentPackageDto ─────────────────────────────────────────
    [Fact]
    public void Convert_AssignmentPackage_MapsFields()
    {
        var obj = new AssignmentPackage { AssignmentId = Guid.NewGuid(), PackageId = Guid.NewGuid() };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(obj);

        dto.Id.Should().Be(obj.Id);
        dto.AssignmentId.Should().Be(obj.AssignmentId);
        dto.PackageId.Should().Be(obj.PackageId);
    }

    [Fact]
    public void Convert_AssignmentResource_MapsFields()
    {
        var obj = new AssignmentResource { AssignmentId = Guid.NewGuid(), ResourceId = Guid.NewGuid() };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(obj);

        dto.Id.Should().Be(obj.Id);
        dto.AssignmentId.Should().Be(obj.AssignmentId);
        dto.ResourceId.Should().Be(obj.ResourceId);
    }

    // ── DtoMapper.Simplified ──────────────────────────────────────────────────
    [Fact]
    public void ToSimplifiedParty_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.ToSimplifiedParty(null).Should().BeNull();
    }

    [Fact]
    public void ToSimplifiedParty_NonNull_MapsFieldsAndExcludesPersonIdentifier()
    {
        var compact = new CompactEntityDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Person",
            Type = "Person",
            Variant = "SAM",
            OrganizationIdentifier = null,
            PersonIdentifier = "12345678901",
            IsDeleted = false,
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ToSimplifiedParty(compact);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(compact.Id);
        dto.Name.Should().Be("Test Person");
        dto.Type.Should().Be("Person");
        dto.Variant.Should().Be("SAM");
    }

    [Fact]
    public void ToSimplifiedConnection_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.ToSimplifiedConnection(null).Should().BeNull();
    }

    [Fact]
    public void ToSimplifiedConnection_NonNull_MapsPartyAndEmptyConnections()
    {
        var entity = new CompactEntityDto { Id = Guid.NewGuid(), Name = "Org", Type = "Organization" };
        var conn = new ConnectionDto { Party = entity, Connections = [] };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ToSimplifiedConnection(conn);

        dto.Should().NotBeNull();
        dto!.Party!.Id.Should().Be(entity.Id);
        dto.Connections.Should().BeEmpty();
    }

    [Fact]
    public void ToSimplifiedConnections_Null_ReturnsEmpty()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.ToSimplifiedConnections(null).Should().BeEmpty();
    }

    [Fact]
    public void ToSimplifiedConnections_NonNull_ReturnsProjected()
    {
        var entity = new CompactEntityDto { Id = Guid.NewGuid(), Name = "Org", Type = "Organization" };
        var connections = new[] { new ConnectionDto { Party = entity, Connections = [] } };

        var result = Altinn.AccessMgmt.Core.Utils.DtoMapper.ToSimplifiedConnections(connections).ToList();

        result.Should().HaveCount(1);
        result[0].Party!.Id.Should().Be(entity.Id);
    }

    // ── DtoMapperPermissionDto ────────────────────────────────────────────────
    [Fact]
    public void ConvertToPermission_Assignment_MapsFromAndToAndRole()
    {
        var from = MakeEntity("A");
        var to = MakeEntity("B");
        var role = MakeRole();
        var assignment = new Assignment { RoleId = role.Id, FromId = from.Id, ToId = to.Id, From = from, To = to, Role = role };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToPermission(assignment);

        dto.From!.Id.Should().Be(from.Id);
        dto.To!.Id.Should().Be(to.Id);
        dto.Role!.Id.Should().Be(role.Id);
    }

    [Fact]
    public void ConvertToPermission_Connection_MapsFromViaTo()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var via = MakeEntity("Via");
        var role = MakeRole("priv");
        var viaRole = MakeRole("dagl");
        var connection = new Connection { From = from, To = to, Via = via, Role = role, ViaRole = viaRole };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToPermission(connection);

        dto.From!.Id.Should().Be(from.Id);
        dto.To!.Id.Should().Be(to.Id);
        dto.Via!.Id.Should().Be(via.Id);
        dto.ViaRole!.Id.Should().Be(viaRole.Id);
        dto.Role!.Id.Should().Be(role.Id);
    }

    // ── DtoMapperDelegationDto ────────────────────────────────────────────────
    [Fact]
    public void ConvertToDelegationDto_Delegation_MapsIds()
    {
        var delegation = new Altinn.AccessMgmt.PersistenceEF.Models.Delegation
        {
            ToId = Guid.NewGuid(),
            FromId = Guid.NewGuid(),
            FacilitatorId = Guid.NewGuid(),
        };
        var packageId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToDelegationDto(delegation, packageId, roleId);

        dto.ToId.Should().Be(delegation.ToId);
        dto.FromId.Should().Be(delegation.FromId);
        dto.ViaId.Should().Be(delegation.FacilitatorId);
        dto.PackageId.Should().Be(packageId);
        dto.RoleId.Should().Be(roleId);
    }

    [Fact]
    public void ConvertToDelegationDto_ExtendedRecord_MapsIds()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var viaId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        var record = new ConnectionQueryExtendedRecord
        {
            FromId = fromId,
            ToId = toId,
            RoleId = roleId,
            ViaId = viaId,
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToDelegationDto(record, packageId, Guid.NewGuid());

        dto.FromId.Should().Be(fromId);
        dto.ToId.Should().Be(toId);
        dto.ViaId.Should().Be(viaId);
        dto.PackageId.Should().Be(packageId);
    }

    // ── CreateDelegationResponseDtoMapper ─────────────────────────────────────
    [Fact]
    public void Convert_Delegation_MapsDelegationIdAndFromEntityId()
    {
        var fromAssignment = new Assignment { FromId = Guid.NewGuid() };
        var delegation = new Altinn.AccessMgmt.PersistenceEF.Models.Delegation { From = fromAssignment };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(delegation);

        dto.DelegationId.Should().Be(delegation.Id);
        dto.FromEntityId.Should().Be(fromAssignment.FromId);
    }

    [Fact]
    public void Convert_DelegationEnumerable_ReturnsOnePerItem()
    {
        var delegations = new Altinn.AccessMgmt.PersistenceEF.Models.Delegation[] { new() { From = new Assignment() }, new() { From = new Assignment() } };
        var result = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(delegations).ToList();
        result.Should().HaveCount(2);
    }

    // ── DtoMapperRolePackage — Package ────────────────────────────────────────
    [Fact]
    public void Convert_Package_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((Package?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_Package_NonNull_MapsScalars()
    {
        var pkg = MakePackage();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(pkg)!;

        dto.Id.Should().Be(pkg.Id);
        dto.Name.Should().Be(pkg.Name);
        dto.Urn.Should().Be(pkg.Urn);
        dto.Description.Should().Be(pkg.Description);
        dto.IsDelegable.Should().Be(pkg.IsDelegable);
        dto.IsAssignable.Should().Be(pkg.IsAssignable);
        dto.IsResourcePolicyAvailable.Should().Be(pkg.IsAvailableForServiceOwners);
    }

    [Fact]
    public void Convert_Package_WithAreaAndResources_MapsAll()
    {
        var pkg = MakePackage();
        var area = MakeArea();
        var resources = new List<Resource> { new() { RefId = "r1" }, new() { RefId = "r2" } };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(pkg, area, resources)!;

        dto.Area!.Id.Should().Be(area.Id);
        dto.Resources.Should().HaveCount(2);
    }

    [Fact]
    public void Convert_Package_WithNullAreaAndResources_EmptyResourcesList()
    {
        var pkg = MakePackage();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(pkg, null, null)!;
        dto.Resources.Should().BeEmpty();
    }

    // ── DtoMapperRolePackage — Role ───────────────────────────────────────────
    [Fact]
    public void Convert_Role_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((Role?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_Role_NonNull_MapsScalars()
    {
        var role = MakeRole("dagl");
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(role)!;

        dto.Id.Should().Be(role.Id);
        dto.Name.Should().Be(role.Name);
        dto.Code.Should().Be("dagl");
        dto.Urn.Should().Be("altinn:role:dagl");
        dto.IsKeyRole.Should().Be(role.IsKeyRole);
        dto.LegacyRoleCode.Should().Be(role.LegacyCode);
    }

    // ── DtoMapperRolePackage — RolePackage ────────────────────────────────────
    [Fact]
    public void Convert_RolePackage_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((RolePackage?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_RolePackage_NonNull_MapsRoleAndPackage()
    {
        var rp = new RolePackage { Role = MakeRole(), Package = MakePackage(), HasAccess = true, CanDelegate = false };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(rp)!;

        dto.Id.Should().Be(rp.Id);
        dto.Role!.Id.Should().Be(rp.Role.Id);
        dto.Package!.Id.Should().Be(rp.Package.Id);
        dto.HasAccess.Should().BeTrue();
        dto.CanDelegate.Should().BeFalse();
    }

    // ── DtoMapperRolePackage — Area ───────────────────────────────────────────
    [Fact]
    public void Convert_Area_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((Area?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_Area_NonNull_MapsFields()
    {
        var area = MakeArea();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(area)!;

        dto.Id.Should().Be(area.Id);
        dto.Name.Should().Be(area.Name);
        dto.Urn.Should().Be(area.Urn);
        dto.Description.Should().Be(area.Description);
        dto.IconUrl.Should().Be(area.IconUrl);
        dto.Packages.Should().BeEmpty();
    }

    [Fact]
    public void Convert_AreaWithPackages_MapsPackageList()
    {
        var area = MakeArea();
        var packages = new List<Package> { MakePackage(), MakePackage() };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(area, packages)!;
        dto.Packages.Should().HaveCount(2);
    }

    [Fact]
    public void Convert_AreaWithNullPackages_EmptyPackageList()
    {
        var area = MakeArea();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(area, null)!;
        dto.Packages.Should().BeEmpty();
    }

    // ── DtoMapperRolePackage — AreaGroup ──────────────────────────────────────
    [Fact]
    public void Convert_AreaGroup_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((AreaGroup?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_AreaGroup_NonNull_MapsFields()
    {
        var ag = MakeAreaGroup();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(ag)!;

        dto.Id.Should().Be(ag.Id);
        dto.Name.Should().Be(ag.Name);
        dto.Urn.Should().Be(ag.Urn);
        dto.Description.Should().Be(ag.Description);
        dto.Areas.Should().BeEmpty();
    }

    [Fact]
    public void Convert_AreaGroupWithAreas_MapsAreaList()
    {
        var ag = MakeAreaGroup();
        var areas = new List<Area> { MakeArea(), MakeArea() };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(ag, areas)!;
        dto.Areas.Should().HaveCount(2);
    }

    [Fact]
    public void Convert_AreaGroupWithNullAreas_EmptyAreaList()
    {
        var ag = MakeAreaGroup();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(ag, null)!;
        dto.Areas.Should().BeEmpty();
    }

    // ── DtoMapperRolePackage — EntityType ─────────────────────────────────────
    [Fact]
    public void Convert_EntityType_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((EntityType?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_EntityType_NonNull_MapsFields()
    {
        var et = MakeEntityType();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(et)!;
        dto.Id.Should().Be(et.Id);
        dto.Name.Should().Be(et.Name);
        dto.ProviderId.Should().Be(et.ProviderId);
    }

    // ── DtoMapperRolePackage — EntityVariant ──────────────────────────────────
    [Fact]
    public void Convert_EntityVariant_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((EntityVariant?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_EntityVariant_NonNull_MapsFields()
    {
        var ev = MakeVariant();
        ev.Type = MakeEntityType();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(ev)!;

        dto.Id.Should().Be(ev.Id);
        dto.Name.Should().Be(ev.Name);
        dto.Description.Should().Be(ev.Description);
        dto.TypeId.Should().Be(ev.TypeId);
        dto.Type.Should().NotBeNull();
    }

    [Fact]
    public void ConvertFlat_EntityVariant_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertFlat((EntityVariant?)null).Should().BeNull();
    }

    [Fact]
    public void ConvertFlat_EntityVariant_NonNull_MapsFields()
    {
        var ev = MakeVariant();
        ev.Type = MakeEntityType();
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertFlat(ev)!;

        dto.Id.Should().Be(ev.Id);
        dto.Name.Should().Be(ev.Name);
    }

    // ── DtoMapperRolePackage — Resource ───────────────────────────────────────
    [Fact]
    public void Convert_Resource_Null_ReturnsNull()
    {
        Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert((Resource?)null).Should().BeNull();
    }

    [Fact]
    public void Convert_Resource_NonNull_MapsFields()
    {
        var resource = new Resource { Name = "Test Resource", RefId = "res1" };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(resource)!;
        dto.Name.Should().Be("Test Resource");
        dto.RefId.Should().Be("res1");
    }

    // ── DtoMapperAccessPackageDto ─────────────────────────────────────────────
    [Fact]
    public void Convert_PackageDto_MapsFields()
    {
        var areaId = Guid.NewGuid();
        var pkgDto = new PackageDto { Id = Guid.NewGuid(), Urn = "altinn:pkg:test", Area = new AreaDto { Id = areaId } };
        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(pkgDto);

        dto.Id.Should().Be(pkgDto.Id);
        dto.Urn.Should().Be(pkgDto.Urn);
        dto.AreaId.Should().Be(areaId);
    }

    // ── DtoMapperAuthorizedPartyDto ───────────────────────────────────────────
    [Fact]
    public void ConvertToAuthorizedPartyDto_MapsScalars()
    {
        var party = new AuthorizedParty
        {
            PartyUuid = Guid.NewGuid(),
            Name = "Test Org",
            OrganizationNumber = "987654321",
            PartyId = 99,
            Type = AuthorizedPartyType.Organization,
            IsDeleted = false,
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToAuthorizedPartyDto(party);

        dto.PartyUuid.Should().Be(party.PartyUuid);
        dto.Name.Should().Be("Test Org");
        dto.OrganizationNumber.Should().Be("987654321");
        dto.PartyId.Should().Be(99);
        dto.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void ConvertToAuthorizedPartyDto_WithSubunits_SubunitsMapped()
    {
        var sub = new AuthorizedParty { PartyUuid = Guid.NewGuid(), Name = "Sub AS" };
        var party = new AuthorizedParty
        {
            PartyUuid = Guid.NewGuid(),
            Name = "Main AS",
            Type = AuthorizedPartyType.Organization,
        };
        party.Subunits.Add(sub);

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToAuthorizedPartyDto(party);

        dto.Subunits.Should().HaveCount(1);
        dto.Subunits[0].PartyUuid.Should().Be(sub.PartyUuid);
    }

    [Fact]
    public void ConvertToAuthorizedPartiesDto_ReturnsOnePerInput()
    {
        var parties = new[]
        {
            new AuthorizedParty { PartyUuid = Guid.NewGuid(), Name = "A" },
            new AuthorizedParty { PartyUuid = Guid.NewGuid(), Name = "B" },
        };

        var result = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToAuthorizedPartiesDto(parties).ToList();

        result.Should().HaveCount(2);
    }

    // ── RequestMapper — RequestAssignmentPackage ───────────────────────────────
    [Fact]
    public void Convert_RequestAssignmentPackage_MapsTypeAndStatus()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var assignment = new RequestAssignment { FromId = from.Id, ToId = to.Id, From = from, To = to };
        var request = new RequestAssignmentPackage
        {
            AssignmentId = Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            Status = RequestStatus.Pending,
            Assignment = assignment,
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(request);

        dto.Id.Should().Be(request.Id);
        dto.Type.Should().Be("package");
        dto.Status.Should().Be(RequestStatus.Pending);
        dto.From!.Id.Should().Be(from.Id);
        dto.To!.Id.Should().Be(to.Id);
        dto.Package!.Id.Should().Be(request.PackageId);
    }

    [Fact]
    public void Convert_RequestAssignmentResource_MapsTypeAndStatus()
    {
        var from = MakeEntity("From");
        var to = MakeEntity("To");
        var assignment = new RequestAssignment { FromId = from.Id, ToId = to.Id, From = from, To = to };
        var request = new RequestAssignmentResource
        {
            AssignmentId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            Status = RequestStatus.Approved,
            Assignment = assignment,
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.Convert(request);

        dto.Type.Should().Be("resource");
        dto.Status.Should().Be(RequestStatus.Approved);
        dto.Resource!.Id.Should().Be(request.ResourceId);
    }

    // ── RequestMapper — ConvertToPartyEntityDto ────────────────────────────────
    [Fact]
    public void ConvertToPartyEntityDto_MapsKnownTypeAndVariant()
    {
        var entity = MakeEntity("Test Org");

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToPartyEntityDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be("Test Org");
        dto.Type.Should().Be("Organisasjon");
        dto.Variant.Should().Be("UTBG");
        dto.OrganizationIdentifier.Should().Be("123456789");
    }

    [Fact]
    public void ConvertToPartyEntityDto_UnknownTypeAndVariant_NullTypeAndVariant()
    {
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Name = "Unknown",
            TypeId = Guid.NewGuid(),  // not in constants
            VariantId = Guid.NewGuid(), // not in constants
        };

        var dto = Altinn.AccessMgmt.Core.Utils.DtoMapper.ConvertToPartyEntityDto(entity);

        dto.Type.Should().BeNull();
        dto.Variant.Should().BeNull();
    }
}

using Altinn.AccessMgmt.Core.HostedServices.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Integration.Platform.Register;

namespace Altinn.AccessMgmt.Core.Tests.Unit.HostedServices;

/// <summary>
/// Unit tests for <see cref="RoleSyncService.ShouldSetParent"/>, which decides whether a
/// streamed external-role assignment should materialize a subunit <c>ParentId</c> relationship.
///
/// Pins the subunit inheritance policy:
/// - BEDR and AAFY registration-unit roles ALWAYS establish the parent relationship.
/// - ADOS (administrative unit - public sector) only establishes the parent relationship when
///   the <c>AccessManagement.Subunit.AdosInheritance</c> feature flag is enabled, so the feature
///   remains fully reversible.
/// - Unrelated roles never establish a parent relationship.
/// </summary>
[UnitTest]
public class RoleSyncServiceShouldSetParentTest
{
    private static ExternalRoleAssignmentEvent Event(string roleIdentifier) => new()
    {
        VersionId = 1,
        Type = ExternalRoleAssignmentEvent.EventType.Added,
        RoleSource = default,
        RoleIdentifier = roleIdentifier,
        ToParty = Guid.NewGuid(),
        FromParty = Guid.NewGuid(),
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldSetParent_Bedr_AlwaysTrue(bool adosEnabled)
    {
        var item = Event(RoleConstants.HasAsRegistrationUnitBEDR.Entity.Code);
        RoleSyncService.ShouldSetParent(item, adosEnabled).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldSetParent_Aafy_AlwaysTrue(bool adosEnabled)
    {
        var item = Event(RoleConstants.HasAsRegistrationUnitAAFY.Entity.Code);
        RoleSyncService.ShouldSetParent(item, adosEnabled).Should().BeTrue();
    }

    [Fact]
    public void ShouldSetParent_Ados_Enabled_ReturnsTrue()
    {
        var item = Event(RoleConstants.AdministrativeUnitPublicSector.Entity.Code);
        RoleSyncService.ShouldSetParent(item, adosSubunitInheritanceEnabled: true).Should().BeTrue();
    }

    [Fact]
    public void ShouldSetParent_Ados_Disabled_ReturnsFalse()
    {
        var item = Event(RoleConstants.AdministrativeUnitPublicSector.Entity.Code);
        RoleSyncService.ShouldSetParent(item, adosSubunitInheritanceEnabled: false).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldSetParent_UnrelatedRole_AlwaysFalse(bool adosEnabled)
    {
        var item = Event(RoleConstants.Agent.Entity.Code);
        RoleSyncService.ShouldSetParent(item, adosEnabled).Should().BeFalse();
    }
}

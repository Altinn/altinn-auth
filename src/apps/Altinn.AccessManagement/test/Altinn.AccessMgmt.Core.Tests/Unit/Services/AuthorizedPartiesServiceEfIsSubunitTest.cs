using Altinn.AccessManagement.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="AuthorizedPartiesServiceEf.IsSubunit(Entity, bool)"/>.
///
/// Pins the AuthorizedParties subunit-nesting policy that mirrors the reversible ADOS feature toggle:
/// - BEDR and AAFY subunits (entities with a ParentId) are ALWAYS nested as subunits.
/// - ADOS (administrative unit - public sector) subunits are only nested when the
///   <c>AccessManagement.Subunit.AdosInheritance</c> feature flag is enabled. When disabled they
///   are surfaced as separate top-level parties (the pre-backfill behavior), even though Entity.ParentId
///   is set.
/// - Entities without a ParentId are never treated as subunits.
/// </summary>
[UnitTest]
public class AuthorizedPartiesServiceEfIsSubunitTest
{
    private static Entity Entity(Guid variantId, Guid? parentId) => new()
    {
        Id = Guid.NewGuid(),
        VariantId = variantId,
        ParentId = parentId,
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_Bedr_WithParent_AlwaysTrue(bool adosAsSubunit)
    {
        var entity = Entity(EntityVariantConstants.BEDR.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity, adosAsSubunit).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_Aafy_WithParent_AlwaysTrue(bool adosAsSubunit)
    {
        var entity = Entity(EntityVariantConstants.AAFY.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity, adosAsSubunit).Should().BeTrue();
    }

    [Fact]
    public void IsSubunit_Ados_Enabled_ReturnsTrue()
    {
        var entity = Entity(EntityVariantConstants.ADOS.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity, adosAsSubunit: true).Should().BeTrue();
    }

    [Fact]
    public void IsSubunit_Ados_Disabled_ReturnsFalse()
    {
        // Even though ParentId is set (backfilled), a disabled flag must keep ADOS as a separate top-level party.
        var entity = Entity(EntityVariantConstants.ADOS.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity, adosAsSubunit: false).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_NoParent_AlwaysFalse(bool adosAsSubunit)
    {
        var entity = Entity(EntityVariantConstants.ORGL.Id, parentId: null);
        AuthorizedPartiesServiceEf.IsSubunit(entity, adosAsSubunit).Should().BeFalse();
    }
}

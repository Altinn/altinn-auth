using Altinn.AccessManagement.Core.Services;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="AuthorizedPartiesServiceEf.IsSubunit(Entity)"/>.
///
/// Pins the AuthorizedParties subunit-nesting policy that mirrors the reversible ADOS feature toggle:
/// - BEDR and AAFY subunits (entities with a ParentId) are ALWAYS nested as subunits.
/// - ADOS (administrative unit - public sector) subunits are only nested when the
///   <c>AccessManagement.Subunit.AdosInheritance</c> feature flag is enabled. When disabled they
///   are surfaced as separate top-level parties (the pre-backfill behavior), even though Entity.ParentId
///   is set.
/// - Entities without a ParentId are never treated as subunits.
///
/// The feature flag is resolved once at startup into a per-host <see cref="AppLifecycleFeatures"/>
/// singleton, which these tests inject directly per case (no shared static state).
/// </summary>
[UnitTest]
public class AuthorizedPartiesServiceEfIsSubunitTest
{
    private static AuthorizedPartiesServiceEf CreateService(bool adosSubunitInheritance) =>
        new(
            contextRetrievalService: null!,
            repoService: null!,
            memoryCache: null!,
            lifecycleFeatures: new AppLifecycleFeatures { AdosSubunitInheritance = adosSubunitInheritance });

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
        var sut = CreateService(adosAsSubunit);
        var entity = Entity(EntityVariantConstants.BEDR.Id, Guid.NewGuid());
        sut.IsSubunit(entity).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_Aafy_WithParent_AlwaysTrue(bool adosAsSubunit)
    {
        var sut = CreateService(adosAsSubunit);
        var entity = Entity(EntityVariantConstants.AAFY.Id, Guid.NewGuid());
        sut.IsSubunit(entity).Should().BeTrue();
    }

    [Fact]
    public void IsSubunit_Ados_Enabled_ReturnsTrue()
    {
        var sut = CreateService(adosSubunitInheritance: true);
        var entity = Entity(EntityVariantConstants.ADOS.Id, Guid.NewGuid());
        sut.IsSubunit(entity).Should().BeTrue();
    }

    [Fact]
    public void IsSubunit_Ados_Disabled_ReturnsFalse()
    {
        // Even though ParentId is set (backfilled), a disabled flag must keep ADOS as a separate top-level party.
        var sut = CreateService(adosSubunitInheritance: false);
        var entity = Entity(EntityVariantConstants.ADOS.Id, Guid.NewGuid());
        sut.IsSubunit(entity).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_NoParent_AlwaysFalse(bool adosAsSubunit)
    {
        var sut = CreateService(adosAsSubunit);
        var entity = Entity(EntityVariantConstants.ORGL.Id, parentId: null);
        sut.IsSubunit(entity).Should().BeFalse();
    }
}

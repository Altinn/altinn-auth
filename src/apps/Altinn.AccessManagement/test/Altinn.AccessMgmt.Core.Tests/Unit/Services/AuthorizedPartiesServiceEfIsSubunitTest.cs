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
/// The feature flag is resolved once at startup into <see cref="AppLifecycleFeatures.AdosSubunitInheritance"/>,
/// which these tests set directly and reset on dispose.
/// </summary>
[UnitTest]
public class AuthorizedPartiesServiceEfIsSubunitTest : IDisposable
{
    private readonly bool _originalAdosSubunitInheritance = AppLifecycleFeatures.AdosSubunitInheritance;

    public void Dispose()
    {
        AppLifecycleFeatures.AdosSubunitInheritance = _originalAdosSubunitInheritance;
        GC.SuppressFinalize(this);
    }

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
        AppLifecycleFeatures.AdosSubunitInheritance = adosAsSubunit;
        var entity = Entity(EntityVariantConstants.BEDR.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_Aafy_WithParent_AlwaysTrue(bool adosAsSubunit)
    {
        AppLifecycleFeatures.AdosSubunitInheritance = adosAsSubunit;
        var entity = Entity(EntityVariantConstants.AAFY.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity).Should().BeTrue();
    }

    [Fact]
    public void IsSubunit_Ados_Enabled_ReturnsTrue()
    {
        AppLifecycleFeatures.AdosSubunitInheritance = true;
        var entity = Entity(EntityVariantConstants.ADOS.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity).Should().BeTrue();
    }

    [Fact]
    public void IsSubunit_Ados_Disabled_ReturnsFalse()
    {
        // Even though ParentId is set (backfilled), a disabled flag must keep ADOS as a separate top-level party.
        AppLifecycleFeatures.AdosSubunitInheritance = false;
        var entity = Entity(EntityVariantConstants.ADOS.Id, Guid.NewGuid());
        AuthorizedPartiesServiceEf.IsSubunit(entity).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubunit_NoParent_AlwaysFalse(bool adosAsSubunit)
    {
        AppLifecycleFeatures.AdosSubunitInheritance = adosAsSubunit;
        var entity = Entity(EntityVariantConstants.ORGL.Id, parentId: null);
        AuthorizedPartiesServiceEf.IsSubunit(entity).Should().BeFalse();
    }
}

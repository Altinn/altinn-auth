using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

// See: overhaul part-2 step 21
namespace Altinn.AccessMgmt.Tests.Queries;

/// <summary>
/// Pure-unit tests for <see cref="ConnectionQueryFilter.HasAny"/> and
/// <see cref="ConnectionQueryFilter.Validate"/>. Pins which collections
/// participate in the "has any filter set" decision — a regression that
/// expanded HasAny to include ViaIds / ViaRoleIds / ExcludeRoleIds (or
/// shrank it to drop e.g. InstanceIds) would either silently let
/// unsafe broad-scan queries through Validate or block legitimate
/// instance-only queries.
/// </summary>
public class ConnectionQueryFilterTest
{
    [Fact]
    public void HasAny_AllCollectionsNullOrEmpty_ReturnsFalse()
    {
        new ConnectionQueryFilter().HasAny.Should().BeFalse();
        new ConnectionQueryFilter
        {
            FromIds = [],
            ToIds = [],
            RoleIds = [],
            PackageIds = [],
            ResourceIds = [],
            InstanceIds = [],
        }.HasAny.Should().BeFalse();
    }

    [Theory]
    [InlineData("FromIds")]
    [InlineData("ToIds")]
    [InlineData("RoleIds")]
    [InlineData("PackageIds")]
    [InlineData("ResourceIds")]
    public void HasAny_OnlyOneGuidCollectionPopulated_ReturnsTrue(string field)
    {
        var guids = new[] { Guid.NewGuid() };
        var filter = field switch
        {
            "FromIds" => new ConnectionQueryFilter { FromIds = guids },
            "ToIds" => new ConnectionQueryFilter { ToIds = guids },
            "RoleIds" => new ConnectionQueryFilter { RoleIds = guids },
            "PackageIds" => new ConnectionQueryFilter { PackageIds = guids },
            "ResourceIds" => new ConnectionQueryFilter { ResourceIds = guids },
            _ => throw new ArgumentException(field),
        };

        filter.HasAny.Should().BeTrue();
    }

    [Fact]
    public void HasAny_OnlyInstanceIdsPopulated_ReturnsTrue()
    {
        new ConnectionQueryFilter { InstanceIds = ["instance-1"] }.HasAny.Should().BeTrue();
    }

    [Fact]
    public void HasAny_OnlyViaIdsPopulated_ReturnsFalse()
    {
        // Pinning the documented exclusion: ViaIds, ViaRoleIds, ExcludeRoleIds
        // do NOT count for HasAny. A regression that expanded HasAny to
        // include them would let queries through Validate that don't actually
        // narrow the scan.
        new ConnectionQueryFilter { ViaIds = [Guid.NewGuid()] }.HasAny.Should().BeFalse();
    }

    [Fact]
    public void HasAny_OnlyViaRoleIdsPopulated_ReturnsFalse()
    {
        new ConnectionQueryFilter { ViaRoleIds = [Guid.NewGuid()] }.HasAny.Should().BeFalse();
    }

    [Fact]
    public void HasAny_OnlyExcludeRoleIdsPopulated_ReturnsFalse()
    {
        new ConnectionQueryFilter { ExcludeRoleIds = [Guid.NewGuid()] }.HasAny.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoFilters_ThrowsArgumentException()
    {
        var filter = new ConnectionQueryFilter();
        Assert.Throws<ArgumentException>(filter.Validate);
    }

    [Fact]
    public void Validate_AnyFilterSet_DoesNotThrow()
    {
        var filter = new ConnectionQueryFilter { FromIds = [Guid.NewGuid()] };
        filter.Validate();
    }

    [Fact]
    public void DefaultFlags_PinnedAtConstruction()
    {
        var filter = new ConnectionQueryFilter();

        // Default flag values are part of the public contract — a regression
        // that flipped any of these would silently change the shape of every
        // unfiltered query.
        filter.OnlyUniqueResults.Should().BeFalse();
        filter.EnrichEntities.Should().BeTrue();
        filter.IncludePackages.Should().BeFalse();
        filter.IncludeResources.Should().BeFalse();
        filter.IncludeInstances.Should().BeFalse();
        filter.EnrichPackageResources.Should().BeFalse();
        filter.ExcludeDeleted.Should().BeFalse();
        filter.IncludeDelegation.Should().BeTrue();
        filter.IncludeKeyRole.Should().BeTrue();
        filter.IncludeSubConnections.Should().BeTrue();
        filter.IncludeMainUnitConnections.Should().BeTrue();
    }
}

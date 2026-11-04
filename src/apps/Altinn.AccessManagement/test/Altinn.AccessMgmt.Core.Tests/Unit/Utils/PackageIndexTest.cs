using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Utils;

/// <summary>
/// Unit tests for <see cref="PackageIndex"/>, the in-memory matching behind the package lookup.
/// </summary>
[UnitTest]
public class PackageIndexTest
{
    private static readonly Package Agriculture = PackageConstants.Agriculture.Entity;
    private static readonly Package Accounting = PackageConstants.AccountingAndEconomicReporting.Entity;

    /// <summary>A package that is not defined in <see cref="PackageConstants"/>, as if added by a script.</summary>
    private static readonly Package DatabaseOnly = new()
    {
        Id = Guid.Parse("d8b1c0a2-4128-4000-8000-000000000001"),
        Urn = "urn:altinn:accesspackage:database-only-lookup",
        Code = "database-only-lookup",
    };

    private static readonly PackageIndex Index = new([Agriculture, Accounting, DatabaseOnly]);

    [Fact]
    public void FindByUrn_AcceptsFullUrnSuffixAndCodeIgnoringCase()
    {
        var code = Agriculture.Urn.Split(':').Last();

        foreach (var value in new[] { Agriculture.Urn, Agriculture.Urn.ToUpperInvariant(), ":" + code, code })
        {
            Assert.True(Index.FindByUrn(value)?.Id == Agriculture.Id, $"'{value}' did not find {Agriculture.Urn}.");
        }
    }

    [Theory]
    [InlineData("urn:altinn:accesspackage:does-not-exist")]
    [InlineData("does-not-exist")]
    [InlineData("")]
    [InlineData(null)]
    public void FindByUrn_UnknownOrEmptyValue_ReturnsNull(string value)
    {
        Assert.Null(Index.FindByUrn(value));
    }

    [Fact]
    public void FindByUrn_DoesNotMatchId()
    {
        Assert.Null(Index.FindByUrn(Agriculture.Id.ToString()));
    }

    [Fact]
    public void Find_FindsByUrnCodeAndId()
    {
        foreach (var value in new[] { Agriculture.Urn, Agriculture.Code, Agriculture.Code.ToUpperInvariant(), Agriculture.Id.ToString() })
        {
            Assert.True(Index.Find(value)?.Id == Agriculture.Id, $"'{value}' did not find {Agriculture.Urn}.");
        }
    }

    [Theory]
    [InlineData("does-not-exist")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    [InlineData(null)]
    public void Find_UnknownOrEmptyValue_ReturnsNull(string value)
    {
        Assert.Null(Index.Find(value));
    }

    [Fact]
    public void Lookups_PackageNotInConstants_AreFound()
    {
        Assert.DoesNotContain(PackageConstants.AllEntities(), p => p.Entity.Urn == DatabaseOnly.Urn);

        Assert.Equal(DatabaseOnly.Id, Index.FindByUrn(DatabaseOnly.Urn)?.Id);
        Assert.Equal(DatabaseOnly.Id, Index.FindByUrn(DatabaseOnly.Code)?.Id);
        Assert.Equal(DatabaseOnly.Id, Index.Find(DatabaseOnly.Code)?.Id);
        Assert.Equal(DatabaseOnly.Id, Index.Find(DatabaseOnly.Id.ToString())?.Id);
    }

    [Fact]
    public void Packages_ReturnsAllPackages()
    {
        Assert.Equal(3, Index.Packages.Count);
    }
}

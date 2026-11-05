using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

// No web host: builds an AppDbContext directly from the EfDatabaseFixture
// database (a clone of the shared migrated + seeded template) and instantiates
// PackageLookupService by hand.
namespace Altinn.AccessManagement.Tests.Integration.Services;

/// <summary>
/// Tests for <see cref="PackageLookupService"/>: which values find a package in the database.
/// </summary>
[IntegrationTest]
public class PackageLookupServiceTests : IClassFixture<EfDatabaseFixture>, IAsyncLifetime
{
    private static readonly AuditValues TestAudit = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest);

    private static readonly Package Agriculture = PackageConstants.Agriculture.Entity;

    /// <summary>A package that is not defined in <see cref="PackageConstants"/>.</summary>
    private static readonly Package DatabaseOnly = new()
    {
        Id = Guid.Parse("d8b1c0a2-4128-4000-8000-000000000003"),
        ProviderId = PackageConstants.AccountingAndEconomicReporting.Entity.ProviderId,
        EntityTypeId = PackageConstants.AccountingAndEconomicReporting.Entity.EntityTypeId,
        AreaId = PackageConstants.AccountingAndEconomicReporting.Entity.AreaId,
        Name = "Database only lookup",
        Description = "Only in the database",
        Urn = "urn:altinn:accesspackage:database-only-lookup",
        Code = "database-only-lookup",
        IsAssignable = true,
        IsDelegable = true,
    };

    private readonly EfDatabaseFixture _fixture;
    private readonly AppDbContext _db;
    private readonly PackageLookupService _lookup;

    public PackageLookupServiceTests(EfDatabaseFixture fixture)
    {
        _fixture = fixture;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Db.Admin.ToString())
            .Options;

        _db = new AppDbContext(options)
        {
            AuditAccessor = new AuditAccessor { AuditValues = TestAudit }
        };

        _lookup = new PackageLookupService(_db);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync() => await _fixture.EnsureSeedOnceAsync(async () =>
    {
        _db.Packages.Add(DatabaseOnly);
        await _db.SaveChangesAsync(TestAudit);
        _db.ChangeTracker.Clear();
    });

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindByUrn_AcceptsFullUrnSuffixAndCodeIgnoringCase()
    {
        foreach (var value in new[] { Agriculture.Urn, Agriculture.Urn.ToUpperInvariant(), ":" + Agriculture.Code, Agriculture.Code, Agriculture.Code.ToUpperInvariant() })
        {
            var package = await _lookup.FindByUrn(value, TestContext.Current.CancellationToken);
            Assert.True(package?.Id == Agriculture.Id, $"'{value}' did not find {Agriculture.Urn}.");
        }
    }

    [Fact]
    public async Task FindByUrn_LoadsEntityType()
    {
        var package = await _lookup.FindByUrn(Agriculture.Urn, TestContext.Current.CancellationToken);

        Assert.NotNull(package?.EntityType);
        Assert.Equal(Agriculture.EntityTypeId, package.EntityType.Id);
    }

    [Theory]
    [InlineData("urn:altinn:accesspackage:does-not-exist")]
    [InlineData("does-not-exist")]
    [InlineData("")]
    [InlineData(null)]
    public async Task FindByUrn_UnknownOrEmptyValue_ReturnsNull(string value)
    {
        Assert.Null(await _lookup.FindByUrn(value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByUrn_DoesNotMatchId()
    {
        Assert.Null(await _lookup.FindByUrn(Agriculture.Id.ToString(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByUrns_KeysMatchesByValueAsGiven()
    {
        var shortForm = ":" + Agriculture.Code;
        var upperCode = DatabaseOnly.Code.ToUpperInvariant();

        var result = await _lookup.FindByUrns([Agriculture.Urn, shortForm, upperCode, "does-not-exist", null, Agriculture.Urn], TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Equal(Agriculture.Id, result[Agriculture.Urn].Id);
        Assert.Equal(Agriculture.Id, result[shortForm].Id);
        Assert.Equal(DatabaseOnly.Id, result[upperCode].Id);
    }

    [Fact]
    public async Task FindByUrns_NoValues_ReturnsEmpty()
    {
        Assert.Empty(await _lookup.FindByUrns([], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Find_FindsByUrnCodeAndId()
    {
        foreach (var value in new[] { Agriculture.Urn, ":" + Agriculture.Code, Agriculture.Code, Agriculture.Code.ToUpperInvariant(), Agriculture.Id.ToString() })
        {
            var package = await _lookup.Find(value, TestContext.Current.CancellationToken);
            Assert.True(package?.Id == Agriculture.Id, $"'{value}' did not find {Agriculture.Urn}.");
        }
    }

    [Theory]
    [InlineData("does-not-exist")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Find_UnknownOrEmptyValue_ReturnsNull(string value)
    {
        Assert.Null(await _lookup.Find(value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Lookups_PackageNotInConstants_AreFound()
    {
        Assert.DoesNotContain(PackageConstants.AllEntities(), p => p.Entity.Urn == DatabaseOnly.Urn);

        var ct = TestContext.Current.CancellationToken;
        var byUrn = await _lookup.FindByUrn(DatabaseOnly.Urn, ct);
        var byUrnCode = await _lookup.FindByUrn(DatabaseOnly.Code, ct);
        var byCode = await _lookup.Find(DatabaseOnly.Code, ct);
        var byId = await _lookup.Find(DatabaseOnly.Id.ToString(), ct);

        Assert.Equal(DatabaseOnly.Id, byUrn?.Id);
        Assert.Equal(DatabaseOnly.Id, byUrnCode?.Id);
        Assert.Equal(DatabaseOnly.Id, byCode?.Id);
        Assert.Equal(DatabaseOnly.Id, byId?.Id);
    }
}

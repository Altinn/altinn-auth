using System.Reflection;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Data;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Data;

/// <summary>
/// Integration tests for how <see cref="StaticDataIngest.IngestAll"/> handles a package marked
/// <see cref="ConstantDefinition{T}.TestOnly"/>.
/// </summary>
[IntegrationTest]
[Collection(nameof(StaticDataIngestIsProdCollection))]
public sealed class StaticDataIngestTestOnlyPackageTest : IClassFixture<ApiFixture>, IDisposable
{
    private static readonly Guid RegularPackage = PackageConstants.TaxBase.Id;

    /// <summary>
    /// A real package the test marks test only for its duration, so the ingest is covered without a package that
    /// exists just for testing. It has translations and role packages, which the ingest must also leave out.
    /// </summary>
    private static readonly ConstantDefinition<Package> MarkedPackage = PackageConstants.TaxBaseExtendedAccess;

    private static readonly PropertyInfo TestOnlyProperty = typeof(ConstantDefinition<Package>).GetProperty(nameof(ConstantDefinition<Package>.TestOnly))!;

    private readonly bool _isProd = ConstantLookup.IsProd;
    private readonly bool _markedPackageTestOnly = MarkedPackage.TestOnly;

    public StaticDataIngestTestOnlyPackageTest(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    public ApiFixture Fixture { get; }

    public void Dispose()
    {
        ConstantLookup.IsProd = _isProd;
        TestOnlyProperty.SetValue(MarkedPackage, _markedPackageTestOnly);
    }

    /// <summary>
    /// Marks <see cref="MarkedPackage"/> test only, and uses it with every package already marked so. The fixture's
    /// database is seeded outside production, so it has them with their translations and role packages. The test
    /// removes them and, as a control, the role packages of a regular package, and ingests as production: only the
    /// regular rows must come back. It does not ingest the packages again outside production, since the ingest adds
    /// the constant's shared entity instance, which already carries navigations from the context that seeded the
    /// template.
    /// </summary>
    [Fact]
    public async Task IngestAll_TestOnlyPackages_AreLeftOutInProdOnly()
    {
        var ct = TestContext.Current.CancellationToken;

        // The first call builds the host, which sets IsProd and seeds the database, so the flags are changed only after it.
        await Fixture.QueryDb(db => Task.CompletedTask);
        TestOnlyProperty.SetValue(MarkedPackage, true);
        var testOnly = PackageConstants.AllEntities().Where(p => p.TestOnly).Select(p => p.Id).ToList();
        Assert.Contains(MarkedPackage.Id, testOnly);

        await Fixture.QueryDb(async db =>
        {
            Assert.Equal(testOnly.Count, await db.Packages.CountAsync(p => testOnly.Contains(p.Id), ct));
            Assert.True(await db.TranslationEntries.AnyAsync(t => testOnly.Contains(t.Id), ct));
            Assert.True(await db.RolePackages.AnyAsync(r => testOnly.Contains(r.PackageId), ct));

            db.RolePackages.RemoveRange(await db.RolePackages.AsTracking().Where(r => testOnly.Contains(r.PackageId) || r.PackageId == RegularPackage).ToListAsync(ct));
            db.TranslationEntries.RemoveRange(await db.TranslationEntries.AsTracking().Where(t => t.Type == nameof(Package) && testOnly.Contains(t.Id)).ToListAsync(ct));
            db.Packages.RemoveRange(await db.Packages.AsTracking().Where(p => testOnly.Contains(p.Id)).ToListAsync(ct));
            await db.SaveChangesAsync(ct);
        });

        ConstantLookup.IsProd = true;
        await Fixture.QueryDb(db => StaticDataIngest.IngestAll(db, ct));
        await Fixture.QueryDb(async db =>
        {
            Assert.False(await db.Packages.AnyAsync(p => testOnly.Contains(p.Id), ct));
            Assert.False(await db.TranslationEntries.AnyAsync(t => testOnly.Contains(t.Id), ct));
            Assert.False(await db.RolePackages.AnyAsync(r => testOnly.Contains(r.PackageId), ct));
            Assert.True(await db.RolePackages.AnyAsync(r => r.PackageId == RegularPackage, ct));
        });
    }
}

/// <summary>
/// Runs the tests that change <see cref="ConstantLookup.IsProd"/> on their own, since the whole process shares it.
/// </summary>
[CollectionDefinition(nameof(StaticDataIngestIsProdCollection), DisableParallelization = true)]
public class StaticDataIngestIsProdCollection
{
}

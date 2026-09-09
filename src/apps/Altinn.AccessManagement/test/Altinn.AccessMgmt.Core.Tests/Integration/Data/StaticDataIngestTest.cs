using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Data;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Data;

/// <summary>
/// Integration tests for <see cref="StaticDataIngest.IngestAll"/>.
/// </summary>
[IntegrationTest]
public class StaticDataIngestTest : IClassFixture<ApiFixture>
{
    public ApiFixture Fixture { get; }

    public StaticDataIngestTest(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    /// <summary>
    /// The fixture's database is cloned from a template that is already migrated and
    /// seeded (the EF <c>UseAsyncSeeding</c> hook runs <see cref="StaticDataIngest.IngestAll"/>
    /// during that migration). Running it again here against that already-seeded clone
    /// reproduces the "second instance starts against an already-seeded database" case
    /// the batched existence checks and the advisory lock need to keep working.
    /// </summary>
    [Fact]
    public async Task IngestAll_CalledAgainOnSeededDatabase_DoesNotDuplicateRowsOrThrow()
    {
        await Fixture.QueryDb(async db =>
        {
            var rolePackageCountBefore = await db.RolePackages.CountAsync(TestContext.Current.CancellationToken);
            var roleMapCountBefore = await db.RoleMaps.CountAsync(TestContext.Current.CancellationToken);
            var entityVariantRoleCountBefore = await db.EntityVariantRoles.CountAsync(TestContext.Current.CancellationToken);

            await StaticDataIngest.IngestAll(db, TestContext.Current.CancellationToken);

            Assert.Equal(rolePackageCountBefore, await db.RolePackages.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(roleMapCountBefore, await db.RoleMaps.CountAsync(TestContext.Current.CancellationToken));
            Assert.Equal(entityVariantRoleCountBefore, await db.EntityVariantRoles.CountAsync(TestContext.Current.CancellationToken));
        });
    }
}

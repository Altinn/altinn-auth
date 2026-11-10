using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Data;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
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

/// <summary>
/// Integration tests for <see cref="StaticDataIngest.RemoveObsoletePackages"/>, run through
/// <see cref="StaticDataIngest.IngestAll"/> as on deploy.
/// </summary>
[IntegrationTest]
public class StaticDataIngestRemoveObsoletePackagesTest : IClassFixture<ApiFixture>
{
    private static readonly Guid ObsoletePackageId = StaticDataIngest.ObsoletePackageIds[0];
    private static readonly Guid Estate = Guid.Parse("b0a7e5c1-0003-4000-8000-000000000001");
    private static readonly Guid Admin = Guid.Parse("b0a7e5c1-0003-4000-8000-000000000002");

    public ApiFixture Fixture { get; }

    public StaticDataIngestRemoveObsoletePackagesTest(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    /// <summary>
    /// Recreates the removed Konkursbo administrator package as it exists in deployed databases: with a
    /// translation, a role package and an assignment package. Expects the ingest to delete all of them,
    /// while the assignment itself is kept, and a second ingest to do nothing.
    /// </summary>
    [Fact]
    public async Task IngestAll_WithObsoletePackage_RemovesPackageAndReferences()
    {
        var assignment = new Assignment { FromId = Estate, ToId = Admin, RoleId = RoleConstants.Rightholder };

        await Fixture.QueryDb(async db =>
        {
            var template = PackageConstants.MainAdministrator.Entity;
            db.Packages.Add(new Package
            {
                Id = ObsoletePackageId,
                ProviderId = template.ProviderId,
                EntityTypeId = template.EntityTypeId,
                AreaId = template.AreaId,
                Name = "Konkursbo administrator",
                Description = "Obsolete",
                Urn = "urn:altinn:accesspackage:konkursbo-tilgangsstyrer",
                Code = "konkursbo-tilgangsstyrer",
            });
            db.TranslationEntries.Add(new TranslationEntry { Id = ObsoletePackageId, Type = nameof(Package), LanguageCode = "eng", FieldName = "Name", Value = "Bankruptcy estate administrator" });
            db.RolePackages.Add(new RolePackage { RoleId = RoleConstants.EstateAdministrator, PackageId = ObsoletePackageId, CanDelegate = true, HasAccess = true });

            db.Entities.AddRange(
                new Entity { Id = Estate, Name = "Konkursbo Obsolete", TypeId = EntityTypeConstants.Organization, VariantId = EntityVariantConstants.KBO, OrganizationIdentifier = "399970301", RefId = "399970301", PartyId = 50970301 },
                new Entity { Id = Admin, Name = "Olav Obsolete", TypeId = EntityTypeConstants.Person, VariantId = EntityVariantConstants.Person, PersonIdentifier = "19017700301", RefId = "19017700301", DateOfBirth = new(1977, 1, 19), PartyId = 50970302, UserId = 20970301 });
            db.Assignments.Add(assignment);
            db.AssignmentPackages.Add(new AssignmentPackage { AssignmentId = assignment.Id, PackageId = ObsoletePackageId });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await Fixture.QueryDb(async db =>
        {
            await StaticDataIngest.IngestAll(db, TestContext.Current.CancellationToken);
            await StaticDataIngest.IngestAll(db, TestContext.Current.CancellationToken);
        });

        await Fixture.QueryDb(async db =>
        {
            var ct = TestContext.Current.CancellationToken;
            Assert.False(await db.Packages.AnyAsync(p => p.Id == ObsoletePackageId, ct));
            Assert.False(await db.TranslationEntries.AnyAsync(t => t.Id == ObsoletePackageId, ct));
            Assert.False(await db.RolePackages.AnyAsync(p => p.PackageId == ObsoletePackageId, ct));
            Assert.False(await db.AssignmentPackages.AnyAsync(p => p.PackageId == ObsoletePackageId, ct));
            Assert.True(await db.Assignments.AnyAsync(a => a.Id == assignment.Id, ct));
        });
    }
}

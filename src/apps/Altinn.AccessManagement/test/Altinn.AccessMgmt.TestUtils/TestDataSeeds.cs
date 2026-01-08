using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.TestUtils.Data;

namespace Altinn.AccessMgmt.TestUtils;

/// <summary>
/// Helper class that seeds deterministic test data into an
/// <see cref="AppDbContext"/> instance. Intended for use during test
/// database initialization so tests run against a known dataset.
/// </summary>
public static class TestDataSeeds
{
    /// <summary>
    /// Applies a minimal set of test entities to the provided <see cref="AppDbContext"/>
    /// and saves the changes. This method is safe to call during test database
    /// bootstrapping and is idempotent if called on a fresh database.
    /// </summary>
    /// <param name="db">The <see cref="AppDbContext"/> to seed.</param>
    public static async Task Exec(AppDbContext db)
    {
        #region Entities
        db.Entities.AddRange([
            TestEntities.PersonPaula,
            TestEntities.PersonOrjan,
            TestEntities.OrganizationNordisAS,
            TestEntities.OrganizationVerdiqAS,
        ]);
        #endregion

        await db.SaveChangesAsync();
    }
}

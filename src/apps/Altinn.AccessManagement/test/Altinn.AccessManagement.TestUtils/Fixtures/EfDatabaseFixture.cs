using Altinn.AccessManagement.TestUtils.Factories;

namespace Altinn.AccessManagement.TestUtils.Fixtures;

/// <summary>
/// Thin database-only fixture for tests that exercise EF services or
/// repositories directly, without standing up a web host.
/// </summary>
/// <remarks>
/// Provides a per-class PostgreSQL database cloned from the shared, migrated and
/// seeded template (see <see cref="EFPostgresFactory"/>). Compared with building
/// a fresh database and running migrations + static-data ingest per fixture, the
/// template clone is fast and reuses the single shared container — so no second
/// PostgreSQL container is needed. Use it as an
/// <see cref="Xunit.IClassFixture{TFixture}"/> and build an
/// <c>AppDbContext</c> from <see cref="Db"/> (e.g.
/// <c>Db.Admin.ToString()</c> — <see cref="PostgresDatabase"/> exposes separate
/// <c>Admin</c> and <c>User</c> connection-string builders).
/// </remarks>
public class EfDatabaseFixture : IAsyncLifetime
{
    /// <summary>
    /// The per-class test database (a clone of the migrated and seeded template).
    /// </summary>
    public PostgresDatabase Db { get; private set; } = null!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        Db = await EFPostgresFactory.Create();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

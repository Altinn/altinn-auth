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
    private readonly SemaphoreSlim _seedGate = new(1, 1);
    private bool _seeded;

    /// <summary>
    /// The per-class test database (a clone of the migrated and seeded template).
    /// </summary>
    public PostgresDatabase Db { get; private set; } = null!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        Db = await EFPostgresFactory.Create();
    }

    /// <summary>
    /// Runs <paramref name="seedAsync"/> exactly once for this fixture (i.e. once
    /// per test class). xUnit constructs the test class once per test method, so a
    /// seed placed in the constructor would otherwise re-run against the same
    /// shared database for every method and collide on unique constraints. Call
    /// this from the constructor instead of seeding directly.
    /// </summary>
    public async Task EnsureSeedOnceAsync(Func<Task> seedAsync)
    {
        ArgumentNullException.ThrowIfNull(seedAsync);

        if (_seeded)
        {
            return;
        }

        await _seedGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_seeded)
            {
                return;
            }

            await seedAsync().ConfigureAwait(false);
            _seeded = true;
        }
        finally
        {
            _seedGate.Release();
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _seedGate.Dispose();
        return ValueTask.CompletedTask;
    }
}

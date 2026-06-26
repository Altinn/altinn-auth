using Microsoft.EntityFrameworkCore;

namespace Altinn.Platform.Authorization.Persistence;

/// <summary>
/// EF Core context that owns schema migrations for the Authorization database.
/// </summary>
/// <remarks>
/// It intentionally declares no <see cref="DbSet{TEntity}"/>s. The <c>delegation</c>
/// schema (its enum type, <c>delegationchanges</c> table, lookup functions and grants)
/// is created by hand-written SQL in the baseline migration. Data access continues to
/// run through the existing raw-Npgsql repositories, so this context exists purely to
/// apply migrations.
/// </remarks>
public sealed class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
}

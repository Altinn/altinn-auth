using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

/// <summary>
/// Provides extension methods for writes that can collide with a concurrent request.
/// </summary>
public static class DbContextConflictExtensions
{
    /// <summary>
    /// Returns true if the exception was caused by another request writing the same rows at the same time:
    /// a unique or foreign key violation, a deadlock, or an optimistic concurrency failure.
    /// </summary>
    public static bool IsConcurrentWriteConflict(this Exception ex) =>
        ex is DbUpdateConcurrencyException
        || (ex as PostgresException ?? ex.InnerException as PostgresException) is
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.DeadlockDetected
        };

    /// <summary>
    /// Runs <paramref name="attempt"/>, which reads the current rows, stages its changes and saves them.
    /// If the save collides with a concurrent request, the change tracker is cleared and the attempt runs
    /// again, so it reads the rows the other request committed instead of inserting them a second time.
    /// The last attempt rethrows the conflict.
    /// </summary>
    /// <param name="db">The context the attempt reads from and saves to.</param>
    /// <param name="attempt">Reads, stages and saves. Must not rely on entities tracked before it runs.</param>
    /// <param name="maxAttempts">How many times the attempt may run in total.</param>
    public static async Task<T> RetryOnConcurrentWriteConflict<T>(
        this DbContext db,
        Func<Task<T>> attempt,
        int maxAttempts = 3)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        for (var attemptNo = 1; ; attemptNo++)
        {
            try
            {
                return await attempt();
            }
            catch (Exception ex) when (attemptNo < maxAttempts && ex.IsConcurrentWriteConflict())
            {
                db.ChangeTracker.Clear();
            }
        }
    }
}

using System.Data;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

/// <summary>
/// Keeps monthly partitions of <c>dbo.activitylog</c> created ahead of time.
/// </summary>
public static class ActivityLogPartitionMaintenance
{
    /// <summary>
    /// How many months of partitions to keep ahead of now. Matches the buffer the migration creates.
    /// </summary>
    public const int DefaultMonthsAhead = 24;

    /// <summary>
    /// Lock timeout in seconds. Attaching a partition takes ACCESS EXCLUSIVE on the default
    /// partition, and while that lock waits behind a long reader, every later query that touches
    /// the default partition queues behind it. Giving up quickly keeps reads flowing; the call
    /// then fails with <c>lock_not_available</c> (55P03) and the next run tries again.
    /// </summary>
    public const int LockTimeoutSeconds = 5;

    /// <summary>
    /// Statement timeout in seconds. Once granted, the lock on the default partition is held while
    /// attaching a partition scans it, and <c>lock_timeout</c> does not cover that. The default
    /// partition is normally empty, so the scan is instant; if it runs long (most likely because
    /// rows have piled up there), the call fails with <c>query_canceled</c> (57014) and rolls back
    /// rather than block reads for longer. Repeated timeouts stop replenishment, which the warning
    /// the init run logs is there to surface.
    /// </summary>
    public const int StatementTimeoutSeconds = 30;

    /// <summary>
    /// Client-side command timeout in seconds: a backstop above the server-side timeouts, so the
    /// server's own error surfaces first.
    /// </summary>
    public const int CommandTimeoutSeconds = StatementTimeoutSeconds + 30;

    /// <summary>
    /// Calls <c>dbo.activitylog_ensure_month_partitions</c>, which creates any missing monthly
    /// partitions up to <paramref name="monthsAhead"/> months from now. Idempotent: once the
    /// partitions exist it creates nothing.
    /// </summary>
    /// <remarks>
    /// Runs in its own transaction, or in the caller's when one is active. The timeouts are set
    /// with <c>SET LOCAL</c>, so in the caller's transaction they last until that one ends, and so
    /// does the lock on the default partition: callers should commit promptly.
    /// </remarks>
    /// <returns>
    /// How many partitions were created, and how many months were skipped because the default
    /// partition already holds rows in their range and they must be moved out by hand first.
    /// </returns>
    public static async Task<ActivityLogPartitionResult> EnsureMonthPartitions(AppDbContext dbContext, int monthsAhead = DefaultMonthsAhead, CancellationToken cancellationToken = default)
    {
        await using var ownTransaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Database.ExecuteSqlRawAsync(
            $"SET LOCAL lock_timeout = '{LockTimeoutSeconds}s'; SET LOCAL statement_timeout = '{StatementTimeoutSeconds}s'",
            cancellationToken);

        ActivityLogPartitionResult result;
        await using (var command = dbContext.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT created, skipped FROM dbo.activitylog_ensure_month_partitions(@monthsAhead)";
            command.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
            command.CommandTimeout = CommandTimeoutSeconds;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "monthsAhead";
            parameter.DbType = DbType.Int32;
            parameter.Value = monthsAhead;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            result = new ActivityLogPartitionResult(reader.GetInt32(0), reader.GetInt32(1));
        }

        if (ownTransaction is not null)
        {
            await ownTransaction.CommitAsync(cancellationToken);
        }

        return result;
    }
}

/// <summary>
/// The outcome of <see cref="ActivityLogPartitionMaintenance.EnsureMonthPartitions"/>.
/// </summary>
/// <param name="Created">Number of partitions created.</param>
/// <param name="Skipped">Number of months that still have no partition.</param>
public sealed record ActivityLogPartitionResult(int Created, int Skipped);

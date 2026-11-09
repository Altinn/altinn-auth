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
    /// Command timeout in seconds. Attaching a partition waits for a SHARE UPDATE EXCLUSIVE lock on
    /// <c>dbo.activitylog</c> and scans the default partition, which can outlast the driver's
    /// 30-second default; the bound still keeps a stuck lock from holding a deploy indefinitely.
    /// </summary>
    public const int CommandTimeoutSeconds = 300;

    /// <summary>
    /// Calls <c>dbo.activitylog_ensure_month_partitions</c>, which creates any missing monthly
    /// partitions up to <paramref name="monthsAhead"/> months from now. Idempotent: once the
    /// partitions exist it creates nothing.
    /// </summary>
    /// <returns>
    /// How many partitions were created, and how many months were skipped because the default
    /// partition already holds rows in their range and they must be moved out by hand first.
    /// </returns>
    public static async Task<ActivityLogPartitionResult> EnsureMonthPartitions(AppDbContext dbContext, int monthsAhead = DefaultMonthsAhead, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT created, skipped FROM dbo.activitylog_ensure_month_partitions(@monthsAhead)";
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandTimeout = CommandTimeoutSeconds;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "monthsAhead";
            parameter.DbType = DbType.Int32;
            parameter.Value = monthsAhead;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new ActivityLogPartitionResult(reader.GetInt32(0), reader.GetInt32(1));
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}

/// <summary>
/// The outcome of <see cref="ActivityLogPartitionMaintenance.EnsureMonthPartitions"/>.
/// </summary>
/// <param name="Created">Number of partitions created.</param>
/// <param name="Skipped">Number of months that still have no partition.</param>
public sealed record ActivityLogPartitionResult(int Created, int Skipped);

using Altinn.AccessMgmt.FFB.Jobs.Models;
using Dapper;

namespace Altinn.AccessMgmt.FFB.Jobs;

/// <summary>
/// Options for a partition maintenance run.
/// </summary>
/// <param name="MonthsAhead">How many months of monthly partitions to ensure ahead of now.</param>
public record ActivityLogPartitionOptions(int MonthsAhead = 24);

/// <summary>
/// Ensures monthly partitions exist ahead of time for <c>dbo.activitylog</c> by calling
/// <c>dbo.activitylog_ensure_month_partitions</c>. Retention is currently unlimited, so the job
/// only creates partitions; it never detaches or drops them.
/// </summary>
public static class ActivityLogPartitionJob
{
    public const string JobName = "ActivityLogPartitions";

    public static async Task RunAsync(DuoRepo repo, JobRun run, ActivityLogPartitionOptions opts, CancellationToken ct)
    {
        var monthsAhead = Math.Clamp(opts.MonthsAhead, 1, 120);
        run.AddLog($"Ensuring monthly activitylog partitions {monthsAhead} months ahead...");

        await using var conn = repo.CreateAccConnection();
        await conn.OpenAsync(ct);

        var result = await conn.QuerySingleAsync<PartitionResult>(new CommandDefinition(
            "SELECT created, skipped FROM dbo.activitylog_ensure_month_partitions(@monthsAhead);",
            new { monthsAhead },
            commandTimeout: 0,
            cancellationToken: ct));

        run.AddLog($"Done — {result.Created} new partition(s) created.");

        if (result.Skipped > 0)
        {
            throw new InvalidOperationException(
                $"{result.Skipped} month(s) still have no partition (see the database warnings): the default partition probably holds rows in that range and they must be moved out first.");
        }
    }

    private sealed record PartitionResult(int Created, int Skipped);
}

using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Jobs;
using Altinn.AccessMgmt.FFB.Jobs.Models;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Cronos;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.FFB.Services;

/// <summary>
/// Singleton background service that reads <see cref="JobSchedulesConfig"/> and fires
/// jobs on their configured cron schedules via <see cref="IJobRunner"/>.
/// </summary>
public sealed class JobSchedulerService(
    IOptions<JobSchedulesConfig> config,
    IJobRunner jobRunner,
    ILogger<JobSchedulerService> logger)
    : BackgroundService, IJobScheduler
{
    private readonly JobSchedulesConfig _config = config.Value;
    private readonly Dictionary<string, DateTimeOffset> _lastRun = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public IReadOnlyList<JobScheduleEntry> Schedules => _config.Schedules;

    public DateTimeOffset? GetLastRun(string scheduleId)
    {
        lock (_lock)
        {
            return _lastRun.TryGetValue(scheduleId, out var ts) ? ts : null;
        }
    }

    public DateTimeOffset? GetNextRun(string scheduleId)
    {
        var entry = _config.Schedules.FirstOrDefault(s =>
            string.Equals(s.Id, scheduleId, StringComparison.OrdinalIgnoreCase));

        if (entry is null || !entry.Enabled || string.IsNullOrWhiteSpace(entry.Cron))
        {
            return null;
        }

        if (!TryParseCron(entry.Cron, entry.Id, out var cron))
        {
            return null;
        }

        var from = GetLastRun(scheduleId) ?? DateTimeOffset.UtcNow;
        return cron.GetNextOccurrence(from, TimeZoneInfo.Utc);
    }

    public JobRun? TriggerNow(string scheduleId)
    {
        var entry = _config.Schedules.FirstOrDefault(s =>
            string.Equals(s.Id, scheduleId, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            logger.LogWarning("TriggerNow: schedule '{Id}' not found.", scheduleId);
            return null;
        }

        return FireSchedule(entry);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            logger.LogInformation("JobScheduler: Enabled = false — no schedules will fire.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var entry in _config.Schedules.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Cron)))
            {
                if (!TryParseCron(entry.Cron, entry.Id, out var cron))
                {
                    continue;
                }

                var lastRun = GetLastRun(entry.Id);
                var from = lastRun ?? now.AddSeconds(-60);  // first-ever check: treat last tick as reference
                var next = cron.GetNextOccurrence(from, TimeZoneInfo.Utc);

                if (next.HasValue && next.Value <= now)
                {
                    logger.LogInformation("Cron trigger: schedule '{Id}' (cron: {Cron}).", entry.Id, entry.Cron);
                    FireSchedule(entry);
                }
            }
        }
    }

    private JobRun? FireSchedule(JobScheduleEntry entry)
    {
        lock (_lock)
        {
            _lastRun[entry.Id] = DateTimeOffset.UtcNow;
        }

        try
        {
            return entry.JobType switch
            {
                JobTypes.IngestCleanup => FireIngestCleanup(entry),
                JobTypes.HistoryCleanup => FireHistoryCleanup(entry),
                JobTypes.AssignmentSync => FireAssignmentSync(entry),
                JobTypes.EntitySync => jobRunner.StartEntitySync(entry.Environment),
                _ => LogUnknownJobType(entry),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fire schedule '{Id}'.", entry.Id);
            return null;
        }
    }

    private JobRun? LogUnknownJobType(JobScheduleEntry entry)
    {
        logger.LogError(
            "Schedule '{Id}' has unknown JobType '{JobType}'. Valid values: {ValidTypes}",
            entry.Id, entry.JobType,
            string.Join(", ", JobTypes.IngestCleanup, JobTypes.HistoryCleanup, JobTypes.AssignmentSync, JobTypes.EntitySync));
        return null;
    }

    private JobRun FireIngestCleanup(JobScheduleEntry entry)
    {
        var opts = entry.IngestOptions ?? new ScheduledIngestOptions();
        return jobRunner.StartIngestCleanup(entry.Environment, new IngestCleanupOptions(
            Filter: opts.Filter,
            Size: opts.Size,
            Execute: opts.Execute,
            Runners: opts.Runners));
    }

    private JobRun FireHistoryCleanup(JobScheduleEntry entry)
    {
        var opts = entry.HistoryOptions ?? new ScheduledHistoryOptions();
        return jobRunner.StartHistoryCleanup(entry.Environment, new HistoryCleanupOptions(
            Table: opts.Table,
            Execute: opts.Execute));
    }

    private JobRun FireAssignmentSync(JobScheduleEntry entry)
    {
        var opts = entry.AssignmentOptions ?? new ScheduledAssignmentOptions();
        var systemAccountId = Guid.TryParse(opts.SystemAccountId, out var sid)
            ? sid
            : SystemEntityConstants.DBA.Id;

        return jobRunner.StartAssignmentSync(entry.Environment, new AssignmentSyncOptions(
            ForceFullScan: opts.ForceFullScan,
            SystemAccountId: systemAccountId));
    }

    private bool TryParseCron(string cron, string scheduleId, out CronExpression result)
    {
        try
        {
            result = CronExpression.Parse(cron);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid cron expression '{Cron}' on schedule '{Id}'.", cron, scheduleId);
            result = null!;
            return false;
        }
    }
}

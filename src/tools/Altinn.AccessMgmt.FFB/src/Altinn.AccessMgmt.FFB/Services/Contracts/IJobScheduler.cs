using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Jobs.Models;

namespace Altinn.AccessMgmt.FFB.Services.Contracts;

public interface IJobScheduler
{
    /// <summary>All configured schedules (enabled and disabled).</summary>
    IReadOnlyList<JobScheduleEntry> Schedules { get; }

    /// <summary>UTC timestamp of the last time this schedule was triggered (null if never).</summary>
    DateTimeOffset? GetLastRun(string scheduleId);

    /// <summary>UTC timestamp of the next planned trigger based on the cron expression.</summary>
    DateTimeOffset? GetNextRun(string scheduleId);

    /// <summary>
    /// Fire the schedule immediately, regardless of its cron expression.
    /// Returns the started <see cref="JobRun"/>, or null if the schedule was not found
    /// or the job type is unknown.
    /// </summary>
    JobRun? TriggerNow(string scheduleId);
}

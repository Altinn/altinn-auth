using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Jobs.Models;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.FFB.Jobs;

public interface IJobRunner
{
    JobRun StartAssignmentSync(string environment, AssignmentSyncOptions opts);

    JobRun StartAssignmentSyncExecute(Guid previewRunId, string environment);

    JobRun StartEntitySync(string environment);

    /// <summary>Executes the resync script from a completed EntitySync preview run against Register.</summary>
    JobRun StartEntitySyncExecute(Guid previewRunId, string environment);

    JobRun StartIngestCleanup(string environment, IngestCleanupOptions opts);

    IReadOnlyList<JobRun> StartIngestExecute(Guid previewRunId, string environment, int runners);

    JobRun StartHistoryCleanup(string environment, HistoryCleanupOptions opts);

    /// <summary>
    /// Executes the SQL statements from a completed preview HistoryCleanup run.
    /// Returns a new JobRun that tracks the execute phase.
    /// </summary>
    JobRun StartHistoryExecute(Guid previewRunId, string environment);

    /// <summary>Scans what would be deleted for the given resource refid. Generates a preview script.</summary>
    JobRun StartResourceCleanup(string environment, ResourceCleanupOptions opts);

    /// <summary>Executes the transaction script from a completed ResourceCleanup preview run.</summary>
    JobRun StartResourceCleanupExecute(Guid previewRunId, string environment);

    /// <summary>
    /// Moves assignment instances off their current (old role) assignment onto an
    /// app-controlled rightholder assignment, then removes the old assignment if it is left empty.
    /// Runs directly against AccessMgmt — there is no preview phase.
    /// </summary>
    JobRun StartAssignmentInstanceCleanup(string environment, AssignmentInstanceCleanupOptions opts);

    /// <summary>
    /// Requests cooperative cancellation of the given run.
    /// Returns false if the run is not found or is already in a terminal state.
    /// </summary>
    bool Cancel(Guid runId);
}

/// <summary>
/// Singleton service. Resolves connection strings from config, creates DuoRepo per run,
/// and fires jobs on the thread pool via Task.Run.
/// </summary>
public sealed class JobRunner(
    IOptions<EnvironmentsConfig> config,
    IJobRunStore store,
    INotificationService notifications) : IJobRunner
{
    private static readonly string Origin = $"{Environment.UserName}@{Environment.MachineName}";
    public JobRun StartAssignmentSync(string environment, AssignmentSyncOptions opts)
    {
        var run = CreateRun(AssignmentSyncJob.JobName, environment);

        FireAndForget(run, async ct =>
        {
            var repo = CreateDuoRepo(environment);
            await AssignmentSyncJob.RunAsync(repo, run, opts, ct);
        });

        return run;
    }

    public JobRun StartAssignmentSyncExecute(Guid previewRunId, string environment)
    {
        var run = CreateRun($"{AssignmentSyncJob.JobName}:Execute", environment);

        FireAndForget(run, async ct =>
        {
            var preview = store.Get(previewRunId)
                ?? throw new InvalidOperationException($"Preview run {previewRunId} not found.");

            var script = preview.DownloadScript
                ?? throw new InvalidOperationException("No transaction script found on preview run.");

            var repo = CreateAccRepo(environment);
            run.AddLog($"Executing transaction script ({preview.SqlOutput.Count} statements)...");
            await repo.ExecuteSql(script, ct);
            run.AddLog("Done.");
        });

        return run;
    }

    public JobRun StartEntitySync(string environment)
    {
        var run = CreateRun(EntitySyncJob.JobName, environment);

        FireAndForget(run, async ct =>
        {
            var repo = CreateDuoRepo(environment);
            await EntitySyncJob.RunAsync(repo, run, ct);
        });

        return run;
    }

    public JobRun StartEntitySyncExecute(Guid previewRunId, string environment)
    {
        var run = CreateRun($"{EntitySyncJob.JobName}:Execute", environment);

        FireAndForget(run, async ct =>
        {
            var preview = store.Get(previewRunId)
                ?? throw new InvalidOperationException($"Preview run {previewRunId} not found.");

            var script = preview.DownloadScript
                ?? throw new InvalidOperationException("No resync script found on preview run.");

            var repo = CreateDuoRepo(environment);
            var resyncCount = preview.SqlOutput.Count(s => s.StartsWith("UPDATE register.party"));
            run.AddLog($"Running {resyncCount} resync statement(s) against Register...");
            await repo.ExecuteRegSql(script, ct);
            run.AddLog("Done. Register will re-emit events for updated parties.");
        });

        return run;
    }

    public JobRun StartIngestCleanup(string environment, IngestCleanupOptions opts)
    {
        var filter = opts.Filter ?? "all";
        var run = CreateRun($"{IngestCleanupJob.JobName}:{filter}", environment);

        FireAndForget(run, async ct =>
        {
            var repo = CreateAccRepo(environment);
            await IngestCleanupJob.RunAsync(repo, run, opts, ct);

            if (opts.Execute && run.SqlOutput.Count > 0)
            {
                SpawnExecuteRunners(run.SqlOutput, environment, opts.Runners, filter);
            }
        });

        return run;
    }

    public IReadOnlyList<JobRun> StartIngestExecute(Guid previewRunId, string environment, int runners)
    {
        var preview = store.Get(previewRunId);
        if (preview is null)
        {
            var errRun = CreateRun($"{IngestCleanupJob.JobName}:Execute", environment);
            FireAndForget(errRun, _ =>
                throw new InvalidOperationException($"Preview run {previewRunId} not found."));
            return [errRun];
        }

        var filter = preview.JobName.Split(':') is [_, var f, ..] ? f : "all";
        return SpawnExecuteRunners(preview.SqlOutput, environment, runners, filter);
    }

    public JobRun StartHistoryCleanup(string environment, HistoryCleanupOptions opts)
    {
        var run = CreateRun(HistoryCleanupJob.JobName, environment);

        FireAndForget(run, async ct =>
        {
            var repo = CreateAccRepo(environment);
            await HistoryCleanupJob.RunAsync(repo, run, opts, ct);
        });

        return run;
    }

    public JobRun StartHistoryExecute(Guid previewRunId, string environment)
    {
        var run = CreateRun($"{HistoryCleanupJob.JobName}:Execute", environment);

        FireAndForget(run, async ct =>
        {
            var preview = store.Get(previewRunId)
                ?? throw new InvalidOperationException($"Preview run {previewRunId} not found.");

            var statements = preview.SqlOutput;
            var repo = CreateAccRepo(environment);
            await HistoryCleanupJob.ExecuteSqlAsync(repo, statements, run, ct);
        });

        return run;
    }

    public JobRun StartResourceCleanup(string environment, ResourceCleanupOptions opts)
    {
        var run = CreateRun(ResourceCleanupJob.JobName, environment);

        FireAndForget(run, async ct =>
        {
            var repo = CreateAccRepo(environment);
            await ResourceCleanupJob.RunAsync(repo, run, opts, ct);
        });

        return run;
    }

    public JobRun StartResourceCleanupExecute(Guid previewRunId, string environment)
    {
        var run = CreateRun($"{ResourceCleanupJob.JobName}:Execute", environment);

        FireAndForget(run, async ct =>
        {
            var preview = store.Get(previewRunId)
                ?? throw new InvalidOperationException($"Preview run {previewRunId} not found.");

            var script = preview.DownloadScript
                ?? throw new InvalidOperationException("No transaction script found on preview run.");

            var repo = CreateAccRepo(environment);
            run.AddLog($"Executing script ({preview.SqlOutput.Count} statements)...");
            await repo.ExecuteSql(script, ct);
            run.AddLog("Done.");
        });

        return run;
    }

    public JobRun StartAssignmentInstanceCleanup(string environment, AssignmentInstanceCleanupOptions opts)
    {
        var run = CreateRun(AssignmentInstanceCleanupJob.JobName, environment);

        FireAndForget(run, async ct =>
        {
            var repo = CreateAccRepo(environment);
            await AssignmentInstanceCleanupJob.RunAsync(repo, run, opts, ct);
        });

        return run;
    }

    public bool Cancel(Guid runId)
    {
        var run = store.Get(runId);
        return run?.RequestCancel() ?? false;
    }

    // ── Core fire-and-forget wrapper ─────────────────────────────────────────
    /// <summary>
    /// Runs <paramref name="work"/> on the thread pool. Handles status transitions,
    /// exception logging and notification dispatch for every job uniformly.
    /// Passes the run's own <see cref="CancellationToken"/> so cancellation propagates
    /// all the way down to DuoRepo/Dapper.
    /// </summary>
    private void FireAndForget(JobRun run, Func<CancellationToken, Task> work)
    {
        _ = Task.Run(async () =>
        {
            var ct = run.CancellationToken;
            try
            {
                run.SetStatus(JobStatus.Running);
                _ = notifications.SendAsync(
                    run.Environment,
                    NotificationLevel.Info,
                    FormatStarted(run));

                await work(ct);

                run.SetStatus(JobStatus.Completed);
                _ = notifications.SendAsync(
                    run.Environment,
                    NotificationLevel.Info,
                    FormatCompleted(run));
            }
            catch (OperationCanceledException)
            {
                run.SetStatus(JobStatus.Cancelled);
            }
            catch (Exception ex)
            {
                run.AddLog($"FAULTED: {ex.Message}", isError: true);
                run.SetStatus(JobStatus.Faulted);
                _ = notifications.SendAsync(
                    run.Environment,
                    NotificationLevel.Error,
                    FormatFaulted(run, ex));
            }
        });
    }

    // Overload for sync/throwing lambdas that don't need the token.
    private void FireAndForget(JobRun run, Action<CancellationToken> work) =>
        FireAndForget(
            run,
            ct =>
            {
                work(ct);
                return Task.CompletedTask;
            });

    // ── Notification message formatters ──────────────────────────────────────
    private static string FormatStarted(JobRun run) =>
        $"▶ <b>{HtmlEncode(run.JobName)}</b>\n" +
        $"Env: {HtmlEncode(run.Environment)} · {HtmlEncode(Origin)}\n" +
        $"Started at {run.StartedAt:HH:mm:ss} UTC";

    private static string FormatCompleted(JobRun run)
    {
        var duration = run.CompletedAt.HasValue
            ? (run.CompletedAt.Value - run.StartedAt).ToString(@"mm\:ss")
            : "?";

        return $"✅ <b>{HtmlEncode(run.JobName)}</b>\n" +
               $"Env: {HtmlEncode(run.Environment)} · {HtmlEncode(Origin)}\n" +
               $"Completed in {duration}";
    }

    private static string FormatFaulted(JobRun run, Exception ex) =>
        $"❌ <b>{HtmlEncode(run.JobName)}</b>\n" +
        $"Env: {HtmlEncode(run.Environment)} · {HtmlEncode(Origin)}\n" +
        $"<code>{HtmlEncode(ex.Message)}</code>";

    private static string HtmlEncode(string s) =>
        s.Replace("&", "&amp;")
         .Replace("<", "&lt;")
         .Replace(">", "&gt;");

    // ── Helpers ──────────────────────────────────────────────────────────────
    /// <summary>AccessMgmt only — for jobs that don't need the Register database.</summary>
    private DuoRepo CreateAccRepo(string environment)
    {
        var entry = GetEntry(environment);

        if (string.IsNullOrWhiteSpace(entry.AccessMgmt))
        {
            throw new InvalidOperationException(
                $"Environment \"{environment}\" has no AccessMgmt connection string.");
        }

        return new DuoRepo(entry.AccessMgmt);
    }

    /// <summary>Both databases — for jobs that diff against Register.</summary>
    private DuoRepo CreateDuoRepo(string environment)
    {
        var entry = GetEntry(environment);

        if (string.IsNullOrWhiteSpace(entry.AccessMgmt))
        {
            throw new InvalidOperationException(
                $"Environment \"{environment}\" has no AccessMgmt connection string.");
        }

        if (string.IsNullOrWhiteSpace(entry.Register))
        {
            throw new InvalidOperationException(
                $"Environment \"{environment}\" has no Register connection string.");
        }

        return new DuoRepo(entry.AccessMgmt, entry.Register);
    }

    private EnvironmentEntry GetEntry(string environment) =>
        config.Value.Environments
            .FirstOrDefault(e => string.Equals(e.Name, environment, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Environment \"{environment}\" not found in configuration.");

    private IReadOnlyList<JobRun> SpawnExecuteRunners(
        IReadOnlyList<string> sqlOutput,
        string environment,
        int runners,
        string filter = "all")
    {
        var tableNames = sqlOutput
            .Select(s => s
                .Replace("DROP TABLE IF EXISTS ingest.", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" CASCADE;", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var chunks = SplitIntoChunks(tableNames, runners);
        var runs = new List<JobRun>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var label = chunks.Count > 1 ? $" [{i + 1}/{chunks.Count}]" : string.Empty;
            var run = CreateRun($"{IngestCleanupJob.JobName}:{filter}:Execute{label}", environment);

            FireAndForget(run, async ct =>
            {
                var repo = CreateAccRepo(environment);
                await IngestCleanupJob.ExecuteDropsAsync(repo, chunk, run, ct);
            });

            runs.Add(run);
        }

        return runs;
    }

    private static IReadOnlyList<IReadOnlyList<T>> SplitIntoChunks<T>(IReadOnlyList<T> source, int count)
    {
        if (source.Count == 0)
        {
            return [];
        }

        count = Math.Clamp(count, 1, source.Count);
        var size = (int)Math.Ceiling((double)source.Count / count);

        return Enumerable.Range(0, count)
            .Select(i => (IReadOnlyList<T>)source.Skip(i * size).Take(size).ToList())
            .Where(c => c.Count > 0)
            .ToList();
    }

    private JobRun CreateRun(string jobName, string environment)
    {
        var run = new JobRun { JobName = jobName, Environment = environment };
        store.Add(run);
        return run;
    }
}

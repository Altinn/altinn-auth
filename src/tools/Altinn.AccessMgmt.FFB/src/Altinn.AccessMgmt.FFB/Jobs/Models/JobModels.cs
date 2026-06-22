namespace Altinn.AccessMgmt.FFB.Jobs.Models;

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Faulted,
    Cancelled,
}

public record LogEntry(DateTimeOffset Timestamp, string Message, bool IsError = false);

public class JobRun
{
    private readonly CancellationTokenSource _cts = new();
    private readonly List<LogEntry> _log = [];
    private readonly List<string> _sql = [];
    private readonly Lock _lock = new();

    public Guid Id { get; } = Guid.CreateVersion7();

    public string JobName { get; init; } = string.Empty;

    public string Environment { get; init; } = string.Empty;

    public JobStatus Status { get; private set; } = JobStatus.Queued;

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Token that is cancelled when <see cref="RequestCancel"/> is called.</summary>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>Snapshot of the log at the time of calling (thread-safe).</summary>
    public IReadOnlyList<LogEntry> Log
    {
        get
        {
            lock (_lock)
            {
                return _log.ToList();
            }
        }
    }

    /// <summary>Snapshot of generated SQL statements (thread-safe).</summary>
    public IReadOnlyList<string> SqlOutput
    {
        get
        {
            lock (_lock)
            {
                return _sql.ToList();
            }
        }
    }

    /// <summary>
    /// Optional full ready-to-execute script (e.g. wrapped in a transaction with audit context).
    /// When set, the job run card uses this for the SQL download instead of
    /// joining <see cref="SqlOutput"/>. When null, SqlOutput is joined as-is.
    /// </summary>
    public string? DownloadScript { get; private set; }

    /// <summary>
    /// Fired from the background thread whenever status, log or SQL changes.
    /// UI components must use InvokeAsync(StateHasChanged) in their handler.
    /// </summary>
    public event Action? OnUpdated;

    public void AddLog(string message, bool isError = false)
    {
        lock (_lock)
        {
            _log.Add(new LogEntry(DateTimeOffset.UtcNow, message, isError));
        }

        OnUpdated?.Invoke();
    }

    public void AddSql(IEnumerable<string> statements)
    {
        lock (_lock)
        {
            _sql.AddRange(statements);
        }

        OnUpdated?.Invoke();
    }

    public void SetDownloadScript(string script)
    {
        lock (_lock)
        {
            DownloadScript = script;
        }

        OnUpdated?.Invoke();
    }

    public void SetStatus(JobStatus status)
    {
        bool fire;
        lock (_lock)
        {
            // Terminal states cannot be overwritten (e.g. Completed racing with RequestCancel).
            if (Status is JobStatus.Completed or JobStatus.Faulted or JobStatus.Cancelled)
            {
                return;
            }

            Status = status;
            if (status is JobStatus.Completed or JobStatus.Faulted or JobStatus.Cancelled)
            {
                CompletedAt = DateTimeOffset.UtcNow;
            }

            fire = true;
        }

        if (fire)
        {
            OnUpdated?.Invoke();
        }
    }

    /// <summary>
    /// Requests cooperative cancellation of this run. Returns false if the run is
    /// already in a terminal state. The status will transition to Cancelled once the
    /// background work observes the token and throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public bool RequestCancel()
    {
        if (Status is JobStatus.Completed or JobStatus.Faulted or JobStatus.Cancelled)
        {
            return false;
        }

        _cts.Cancel();
        return true;
    }

    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : Status == JobStatus.Running ? DateTimeOffset.UtcNow - StartedAt : null;
}

public interface IJobRunStore
{
    void Add(JobRun run);

    /// <summary>Removes a single run from the store (e.g. after user dismisses it from the UI).</summary>
    void Remove(Guid runId);

    /// <summary>Removes all runs in a terminal state (Completed, Faulted, Cancelled).</summary>
    void ClearCompleted();

    IReadOnlyList<JobRun> GetForJob(string jobName);

    IReadOnlyList<JobRun> GetForJobPrefix(string prefix);

    /// <summary>Returns all runs across all job names, unordered.</summary>
    IReadOnlyList<JobRun> GetAll();

    JobRun? Get(Guid id);
}

/// <summary>
/// Thread-safe singleton store. Keeps the last 50 runs per job name.
/// </summary>
public sealed class JobRunStore : IJobRunStore
{
    private const int MaxPerJob = 50;
    private readonly Dictionary<string, List<JobRun>> _runs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, JobRun> _idIndex = [];
    private readonly Lock _lock = new();

    public void Add(JobRun run)
    {
        lock (_lock)
        {
            if (!_runs.TryGetValue(run.JobName, out var list))
            {
                list = [];
                _runs[run.JobName] = list;
            }

            list.Insert(0, run);
            _idIndex[run.Id] = run;

            if (list.Count > MaxPerJob)
            {
                var evicted = list[^1];
                list.RemoveAt(list.Count - 1);
                _idIndex.Remove(evicted.Id);
            }
        }
    }

    public void Remove(Guid runId)
    {
        lock (_lock)
        {
            if (!_idIndex.TryGetValue(runId, out var run))
                return;

            if (_runs.TryGetValue(run.JobName, out var list))
                list.Remove(run);

            _idIndex.Remove(runId);
        }
    }

    public void ClearCompleted()
    {
        lock (_lock)
        {
            foreach (var list in _runs.Values)
            {
                var toRemove = list
                    .Where(r => r.Status is JobStatus.Completed or JobStatus.Faulted or JobStatus.Cancelled)
                    .ToList();

                foreach (var run in toRemove)
                {
                    list.Remove(run);
                    _idIndex.Remove(run.Id);
                }
            }
        }
    }

    public IReadOnlyList<JobRun> GetForJob(string jobName)
    {
        lock (_lock)
        {
            return _runs.TryGetValue(jobName, out var list) ? list.ToList() : [];
        }
    }

    public IReadOnlyList<JobRun> GetForJobPrefix(string prefix)
    {
        lock (_lock)
        {
            return _runs
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .SelectMany(kv => kv.Value)
                .ToList();
        }
    }

    public IReadOnlyList<JobRun> GetAll()
    {
        lock (_lock)
        {
            return _runs.Values.SelectMany(l => l).ToList();
        }
    }

    public JobRun? Get(Guid id)
    {
        lock (_lock)
        {
            return _idIndex.TryGetValue(id, out var run) ? run : null;
        }
    }
}

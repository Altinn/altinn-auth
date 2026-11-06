using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.TestUtils.Concurrency;

/// <summary>
/// Holds the first save of every attached context until all participants have reached their first save, so each
/// of them has read the database before any of them writes. Two requests that both create a missing row are then
/// certain to collide, independent of timing. Later saves, such as a retry, are not held.
/// </summary>
/// <param name="participants">How many contexts must reach their first save before any of them may continue.</param>
/// <param name="timeout">How long a participant waits for the others before the save fails.</param>
public sealed class SaveBarrier(int participants, TimeSpan timeout) : IDisposable
{
    private readonly CountdownEvent _arrived = new(participants);

    /// <summary>
    /// Attaches the barrier to a context and returns a probe recording what the context saved.
    /// </summary>
    public SaveProbe Attach(DbContext db)
    {
        var probe = new SaveProbe();
        var first = true;

        db.SavingChanges += (_, _) =>
        {
            probe.Saves.Enqueue(db.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList());

            if (first)
            {
                first = false;
                _arrived.Signal();
                if (!_arrived.Wait(timeout))
                {
                    throw new TimeoutException($"Only {participants - _arrived.CurrentCount} of {participants} participants reached their first save.");
                }
            }
        };

        db.SaveChangesFailed += (_, e) => probe.Failures.Enqueue(e.Exception);
        return probe;
    }

    /// <inheritdoc/>
    public void Dispose() => _arrived.Dispose();
}

/// <summary>
/// What one context did while attached to a <see cref="SaveBarrier"/>.
/// </summary>
public sealed class SaveProbe
{
    /// <summary>
    /// Gets the entities in the Added state at each save, in the order the saves started.
    /// </summary>
    public ConcurrentQueue<List<object>> Saves { get; } = new();

    /// <summary>
    /// Gets the exceptions of the saves that failed.
    /// </summary>
    public ConcurrentQueue<Exception> Failures { get; } = new();
}

using System.Collections.Concurrent;

namespace Altinn.Authorization.Host.Lease.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IAltinnLease"/>.
/// Provides a thread-safe mechanism for leasing and storing temporary data.
/// There are no cleanup of the keys, creating indefinite keys will result in 
/// memory leak.
/// </summary>
public class InMemoryLease : IAltinnLease
{
    private static readonly ConcurrentDictionary<string, InMemoryLeaseState<object>> _state = new();

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static SemaphoreSlim GetLock(string leaseName) => _locks.GetOrAdd(leaseName, _ => new SemaphoreSlim(1, 1));

    /// <inheritdoc/>
    public async Task<LeaseResult<T>> Put<T>(LeaseResult<T> lease, T data, CancellationToken cancellationToken = default)
        where T : class
    {
        var leaseLock = GetLock(lease.LeaseName);
        await leaseLock.WaitAsync(cancellationToken);
        try
        {
            if (lease.HasLease)
            {
                _state.AddOrUpdate(
                    lease.LeaseName,
                    key => new()
                    {
                        Data = data,
                        AnyHasLease = true
                    },
                    (key, existing) =>
                    {
                        existing.Data = data;
                        return existing;
                    });
            }
            else
            {
                throw new InvalidOperationException("Can't update as lease is taken");
            }

            lease.Data = data;
            return lease;
        }
        finally
        {
            leaseLock.Release();
        }

        throw new ArgumentException("invalid partent type of lease", nameof(lease));
    }

    /// <inheritdoc/>
    public Task<LeaseResult<T>> RefreshLease<T>(LeaseResult<T> lease, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult(lease);
    }

    /// <inheritdoc/>
    public async Task Release<T>(LeaseResult<T> lease, CancellationToken cancellationToken = default)
        where T : class
    {
        var leaseLock = GetLock(lease.LeaseName);
        await leaseLock.WaitAsync(cancellationToken);
        try
        {
            if (lease.HasLease && _state.TryGetValue(lease.LeaseName, out var state) && lease is InMemoryResult<T> castedLease)
            {
                state.AnyHasLease = false;
                castedLease.SetLease = false;
            }
        }
        finally
        {
            leaseLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<LeaseResult<T>> TryAquireNonBlocking<T>(string leaseName, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var leaseLock = GetLock(leaseName);
        await leaseLock.WaitAsync(cancellationToken);
        try
        {
            if (_state.TryGetValue(leaseName, out var value))
            {
                return new InMemoryResult<T>()
                {
                    Data = (T)value.Data,
                    LeaseName = leaseName,
                    Implementation = this,
                    SetLease = !value.AnyHasLease,
                };
            }

            _state[leaseName] = new()
            {
                Data = default,
                AnyHasLease = true,
            };

            return new InMemoryResult<T>()
            {
                Data = default,
                LeaseName = leaseName,
                SetLease = true,
                Implementation = this,
            };
        }
        finally
        {
            leaseLock.Release();
        }
    }

    private sealed class InMemoryLeaseState<T>
    {
        internal bool AnyHasLease { get; set; }

        internal T Data { get; set; }
    }
}

using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.TestUtils;

internal sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }

    /// <summary>
    /// Acquires a lock to access the resource thread-safe.
    /// </summary>
    /// <returns>An <see cref="IDisposable" /> that releases the lock on <see cref="IDisposable.Dispose" />.</returns>
    public async Task<IDisposable> Acquire()
    {
        await _semaphoreSlim.WaitAsync();
        return new LockGuard(_semaphoreSlim);
    }

    /// <summary>
    /// A lock to synchronize threads.
    /// </summary>
    private sealed class LockGuard : IDisposable
    {
        private SemaphoreSlim? _semaphoreSlim;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockGuard" /> class.
        /// </summary>
        /// <param name="semaphoreSlim">The semaphore slim to synchronize threads.</param>
        public LockGuard(SemaphoreSlim semaphoreSlim)
        {
            _semaphoreSlim = semaphoreSlim;
        }

        ~LockGuard()
        {
            if (_semaphoreSlim != null)
            {
                ThrowHelper.ThrowInvalidOperationException("Lock not released.");
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _semaphoreSlim?.Release();
            _semaphoreSlim = null;
        }
    }
}

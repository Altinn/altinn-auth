namespace Altinn.Authorization.Host.Lease.Noop;

public class NoopLease : ILease
{
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public Task<T> Get<T>(CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return Task.FromResult(new T());
    }

    public Task Update<T>(T data, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return Task.CompletedTask;
    }

    public Task Update<T>(Action<T> configureData, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        return Task.CompletedTask;
    }
}

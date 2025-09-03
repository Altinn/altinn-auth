using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Host.Lease.Noop;

/// <summary>
/// In-memory implementation of <see cref="ILeaseService"/>.
/// Provides a thread-safe mechanism for leasing and storing temporary data.
/// There are no cleanup of the keys, creating indefinite keys will result in 
/// memory leak.
/// </summary>
[ExcludeFromCodeCoverage]
public class NoopLeaseService : ILeaseService
{
    /// <inheritdoc/>
    public Task<ILease> TryAcquireNonBlocking(string leaseName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NoopLease() as ILease);
    }
}

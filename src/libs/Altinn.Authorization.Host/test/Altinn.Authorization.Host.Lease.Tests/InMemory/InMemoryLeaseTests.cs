using Altinn.Authorization.Host.Lease.InMemory;

namespace Altinn.Authorization.Host.Lease.Tests;

/// <summary>
/// Unit tests for the in-memory lease implementation.
/// Inherits from <see cref="FanoutTests"/> to test lease behavior under concurrent conditions.
/// </summary>
public class InMemoryLeaseTests : FanoutTests
{
    /// <summary>
    /// Gets or sets the lease instance, using an in-memory implementation.
    /// </summary>
    public override IAltinnLease Lease { get; set; } = new InMemoryLease();

    /// <summary>
    /// Tests lease acquisition under various levels of concurrency.
    /// </summary>
    /// <param name="numThreads">The number of concurrent threads attempting to acquire the lease.</param>
    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task TestLease(int numThreads)
    {
        await TestThreadAquireExplosion(numThreads);
    }
}

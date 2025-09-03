namespace Altinn.Authorization.Host.Lease.Tests;

/// <summary>
/// Abstract base class for lease-related fanout tests.
/// </summary>
public abstract class FanoutTests
{
    /// <summary>
    /// Gets or sets the lease mechanism used for acquiring and releasing leases.
    /// </summary>
    public abstract ILeaseService Lease { get; set; }

    /// <summary>
    /// Tests concurrent lease acquisition by multiple threads to check for race conditions and correct behavior.
    /// </summary>
    /// <param name="numThreads">The number of concurrent threads attempting to acquire the lease.</param>
    public async Task TestThreadAquireExplosion(int numThreads)
    {
        var threads = new List<Task>();
        var lastAssigned = 0;

        for (int i = 0; i < numThreads; i++)
        {
            var loop = i;
            threads.Add(new(async () =>
            {
                await using var lease = await Lease.TryAcquireNonBlocking("test", CancellationToken.None);
                var content = new LeaseContent()
                {
                    Number = loop,
                };

                lastAssigned = loop;
                await lease.Update(content);
            }));
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            await thread;
        }

        await using var result = await Lease.TryAcquireNonBlocking("test", default);
        var data = await result.Get<LeaseContent>();
        Assert.Equal(data.Number, lastAssigned);
    }

    internal class LeaseContent
    {
        internal int Number { get; set; }
    }
}

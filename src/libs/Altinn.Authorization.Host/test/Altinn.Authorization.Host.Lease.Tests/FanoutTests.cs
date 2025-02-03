namespace Altinn.Authorization.Host.Lease.Tests;

/// <summary>
/// Abstract base class for lease-related fanout tests.
/// </summary>
public abstract class FanoutTests
{
    /// <summary>
    /// Gets or sets the lease mechanism used for acquiring and releasing leases.
    /// </summary>
    public abstract IAltinnLease Lease { get; set; }

    /// <summary>
    /// Tests concurrent lease acquisition by multiple threads to check for race conditions and correct behavior.
    /// </summary>
    /// <param name="numThreads">The number of concurrent threads attempting to acquire the lease.</param>
    public async Task TestThreadAquireExplosion(int numThreads)
    {
        var threads = new List<Task>();

        for (int i = 0; i < numThreads; i++)
        {
            var loop = i;
            threads.Add(new(async () =>
            {
                var result = await Lease.TryAquireNonBlocking<LeaseContent>("test", CancellationToken.None);

                if (result.HasLease)
                {
                    var content = result.Data ?? new LeaseContent();
                    if (content.Data.TryGetValue(loop.ToString(), out var data))
                    {
                        content.Data[loop.ToString()] = data + 1;
                    }
                    else
                    {
                        content.Data[loop.ToString()] = 1;
                    }

                    await Lease.Put(result, content, default);
                }
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

        var result = await Lease.TryAquireNonBlocking<LeaseContent>("test", default);

        Assert.True(result.Data.Data.Aggregate(0, (acc, entry) => acc + entry.Value) >= 1);
    }

    /// <summary>
    /// Represents the content stored within a lease, maintaining a dictionary of counters.
    /// </summary>
    public class LeaseContent
    {
        /// <summary>
        /// Dictionary storing key-value pairs where the key is a string identifier, and the value represents a count.
        /// </summary>
        public Dictionary<string, int> Data { get; set; } = new();
    }
}

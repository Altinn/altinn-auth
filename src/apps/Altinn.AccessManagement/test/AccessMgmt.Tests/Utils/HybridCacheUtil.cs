using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Tests.Utils;

/// <summary>
/// Builds the <see cref="HybridCache"/> the services take: the in-memory tier only, or with a distributed tier when one is given.
/// </summary>
public static class HybridCacheUtil
{
    /// <summary>
    /// A cache with the in-memory tier only, as the app runs with the distributed-cache flag off.
    /// </summary>
    public static HybridCache Create()
    {
        return Create(new ServiceCollection());
    }

    /// <summary>
    /// A cache that also writes to <paramref name="distributedCache"/>, for tests that check what reaches the distributed tier.
    /// </summary>
    public static HybridCache Create(IDistributedCache distributedCache)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(distributedCache);
        return Create(services);
    }

    private static HybridCache Create(ServiceCollection services)
    {
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }
}

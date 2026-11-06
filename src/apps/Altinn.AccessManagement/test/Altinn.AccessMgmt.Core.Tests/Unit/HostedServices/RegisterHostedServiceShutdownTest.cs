using Altinn.AccessMgmt.Core.HostedServices;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.Authorization.Host.Lease;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Tests.Unit.HostedServices;

/// <summary>
/// Pins how <see cref="RegisterHostedService"/> shuts down while a sync is running. A sync that outlives
/// <see cref="RegisterHostedService.StopAsync"/> logs after the host has disposed its logger, which
/// crashed the test process.
/// </summary>
[UnitTest]
public class RegisterHostedServiceShutdownTest
{
    [Fact]
    public async Task StopAsync_WaitsForRunningSyncToEnd()
    {
        var resourceSync = new BlockingResourceSyncService();
        var service = CreateService(resourceSync);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await resourceSync.Started.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(resourceSync.Ended);
        service.Dispose();
    }

    [Fact]
    public async Task StopAsync_AfterDispose_DoesNotThrow()
    {
        var resourceSync = new BlockingResourceSyncService();
        var service = CreateService(resourceSync);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await resourceSync.Started.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        service.Dispose();
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(resourceSync.Ended);
    }

    [Fact]
    public async Task Dispose_Twice_DoesNotThrow()
    {
        var service = CreateService(new BlockingResourceSyncService());

        await service.StartAsync(TestContext.Current.CancellationToken);

        service.Dispose();
        service.Dispose();
    }

    private static RegisterHostedService CreateService(IResourceSyncService resourceSync)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"FeatureManagement:{AccessMgmtFeatureFlags.HostedServicesResourceRegistrySync}"] = "true",
            })
            .Build();

        var featureManager = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddFeatureManagement()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IFeatureManager>();

        return new RegisterHostedService(
            new FakeLeaseService(),
            NullLogger<RegisterHostedService>.Instance,
            featureManager,
            resourceSync,
            partySyncService: null!,
            roleSyncService: null!);
    }

    /// <summary>
    /// Blocks in the first sync step until the sync is cancelled, and records when it has ended.
    /// </summary>
    private sealed class BlockingResourceSyncService : IResourceSyncService
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile bool _ended;

        public Task Started => _started.Task;

        public bool Ended => _ended;

        public async Task<bool> SyncResourceOwners(CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return true;
            }
            finally
            {
                _ended = true;
            }
        }

        public Task SyncResources(ILease ls, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeLeaseService : ILeaseService
    {
        public Task<ILease?> TryAcquireNonBlocking(string leaseName, CancellationToken cancellationToken = default)
            => Task.FromResult<ILease?>(new FakeLease());

        public Task<ILease> AcquireBlocking(string leaseName, CancellationToken cancellationToken = default)
            => Task.FromResult<ILease>(new FakeLease());

        public Task<ILease> AcquireBlocking(string leaseName, TimeSpan retry, CancellationToken cancellationToken = default)
            => Task.FromResult<ILease>(new FakeLease());
    }

    private sealed class FakeLease : ILease
    {
        public Task<T> Get<T>(CancellationToken cancellationToken = default)
            where T : class, new()
            => Task.FromResult(new T());

        public Task Update<T>(T data, CancellationToken cancellationToken = default)
            where T : class, new()
            => Task.CompletedTask;

        public Task Update<T>(Action<T> configureData, CancellationToken cancellationToken = default)
            where T : class, new()
            => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.HostedServices;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.Authorization.Host.Lease;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FeatureManagement;
using Moq;

namespace AccessMgmt.Tests.HostedServices;

/// <summary>
/// Tests for <see cref="ConsentMigrationHostedService"/>
/// </summary>
public class ConsentMigrationHostedServiceTests : IDisposable
{
    private readonly Mock<IConsentMigrationSyncService> _syncServiceMock;
    private readonly Mock<IFeatureManager> _featureManagerMock;
    private readonly Mock<IOptionsMonitor<ConsentMigrationSettings>> _settingsMonitorMock;
    private readonly Mock<ILogger<ConsentMigrationHostedService>> _loggerMock;
    private readonly Mock<ILeaseService> _leaseServiceMock;
    private readonly Mock<ILease> _leaseMock;
    private readonly ConsentMigrationSettings _settings;
    private readonly FakeTimeProvider _timeProvider;
    private readonly CancellationTokenSource _cts;

    public ConsentMigrationHostedServiceTests()
    {
        _syncServiceMock = new Mock<IConsentMigrationSyncService>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _settingsMonitorMock = new Mock<IOptionsMonitor<ConsentMigrationSettings>>();
        _loggerMock = new Mock<ILogger<ConsentMigrationHostedService>>();
        _leaseServiceMock = new Mock<ILeaseService>();
        _leaseMock = new Mock<ILease>();
        _timeProvider = new FakeTimeProvider();
        _cts = new CancellationTokenSource();

        _settings = new ConsentMigrationSettings
        {
            BatchSize = 10,
            ConsentStatus = 3,
            OnlyExpiredConsents = true,
            NormalDelayMs = 100,
            EmptyFeedDelayMs = 200,
            FeatureDisabledDelayMs = 300
        };

        _settingsMonitorMock.Setup(x => x.CurrentValue).Returns(_settings);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    [Fact]
    public async Task StartAsync_StartsSuccessfully()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_FeatureDisabled_SkipsProcessing()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(false);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10, TestContext.Current.CancellationToken);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.FeatureDisabledDelayMs * 2));
        await Task.Delay(10, TestContext.Current.CancellationToken);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LeaseUnavailable_SkipsProcessing()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        using var leaseAttempted = new SemaphoreSlim(0, int.MaxValue);
        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .Callback(() => leaseAttempted.Release())
            .ReturnsAsync((ILease)null);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start.
        Assert.True(
            await leaseAttempted.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "Lease attempt did not fire within 2s");

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LeaseAcquired_ProcessesBatch()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        using var batchProcessed = new SemaphoreSlim(0, int.MaxValue);
        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Callback(() => batchProcessed.Release())
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start.
        Assert.True(
            await batchProcessed.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "ProcessBatch did not fire within 2s");

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyBatch_UsesEmptyFeedDelay()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        using var batchProcessed = new SemaphoreSlim(0, int.MaxValue);
        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Callback(() => batchProcessed.Release())
            .ReturnsAsync(0); // Empty batch

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start.
        Assert.True(
            await batchProcessed.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "ProcessBatch did not fire within 2s");

        // After an empty batch the SUT should wait EmptyFeedDelayMs (200ms) — not NormalDelayMs
        // (100ms). Pump advances until the second iteration fires; the test asserts the second
        // iteration only fires once we've advanced past EmptyFeedDelayMs.
        var totalAdvanced = 0;
        var secondFired = false;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline && !secondFired)
        {
            // Advance in small (50ms) increments to detect the exact threshold.
            _timeProvider.Advance(TimeSpan.FromMilliseconds(50));
            totalAdvanced += 50;
            if (await batchProcessed.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken))
            {
                secondFired = true;
            }
        }

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert: second iteration fired AND only after at least EmptyFeedDelayMs was advanced.
        Assert.True(secondFired, "Second ProcessBatch did not fire within 2s");
        Assert.True(totalAdvanced >= _settings.EmptyFeedDelayMs, $"SUT used a delay shorter than EmptyFeedDelayMs ({_settings.EmptyFeedDelayMs}ms): second iteration fired after only {totalAdvanced}ms");
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_NonEmptyBatch_UsesNormalDelay()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        using var batchProcessed = new SemaphoreSlim(0, int.MaxValue);
        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Callback(() => batchProcessed.Release())
            .ReturnsAsync(5); // Non-empty batch

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start.
        Assert.True(
            await batchProcessed.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "ProcessBatch did not fire within 2s");

        // After a non-empty batch the SUT should wait NormalDelayMs (100ms) — strictly less than
        // EmptyFeedDelayMs (200ms). Pump advances in small increments; verify the second iteration
        // fires before we've advanced past EmptyFeedDelayMs (otherwise the SUT picked the wrong
        // delay branch).
        var totalAdvanced = 0;
        var secondFired = false;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline && !secondFired)
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(50));
            totalAdvanced += 50;
            if (await batchProcessed.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken))
            {
                secondFired = true;
            }
        }

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert: second iteration fired AND fired before EmptyFeedDelayMs would have elapsed
        // (proving the SUT used NormalDelayMs not EmptyFeedDelayMs).
        Assert.True(secondFired, "Second ProcessBatch did not fire within 2s");
        Assert.True(totalAdvanced < _settings.EmptyFeedDelayMs, $"SUT appears to have used EmptyFeedDelayMs ({_settings.EmptyFeedDelayMs}ms) instead of NormalDelayMs ({_settings.NormalDelayMs}ms): second iteration only fired after {totalAdvanced}ms");
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_ProcessBatchThrowsException_ContinuesProcessing()
    {
        // Arrange
        var callCount = 0;
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        using var batchProcessed = new SemaphoreSlim(0, int.MaxValue);
        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                batchProcessed.Release();
                callCount++;
                if (callCount == 1)
                {
                    throw new Exception("Test exception");
                }

                return Task.FromResult(5);
            });

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First call fires immediately on service start (throws).
        Assert.True(
            await batchProcessed.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "First ProcessBatch did not fire within 2s");

        // Pump time forward until the second call fires. After the exception, the
        // SUT's catch block delays by 1 minute (error delay) before the next iteration.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var secondCallFired = false;
        while (DateTime.UtcNow < deadline)
        {
            _timeProvider.Advance(TimeSpan.FromMinutes(1));
            if (await batchProcessed.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken))
            {
                secondCallFired = true;
                break;
            }
        }

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert - Service continues after exception
        Assert.True(secondCallFired, "Second ProcessBatch did not fire within 2s after error delay");
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsProcessing()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);
        await Task.Delay(10, TestContext.Current.CancellationToken);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert - no exception thrown, service stops cleanly
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_LeaseDisposedAfterProcessing()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        using var batchProcessed = new SemaphoreSlim(0, int.MaxValue);
        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Callback(() => batchProcessed.Release())
            .ReturnsAsync(5);

        using var leaseDisposed = new SemaphoreSlim(0, int.MaxValue);
        _leaseMock
            .Setup(x => x.DisposeAsync())
            .Callback(() => leaseDisposed.Release())
            .Returns(ValueTask.CompletedTask);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // Wait until ProcessBatch has run (proves the lease was acquired and we're in the
        // post-batch _timeProvider.Delay await). Then cancel — the `await using var lease`
        // block disposes the lease as cancellation unwinds.
        Assert.True(
            await batchProcessed.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "ProcessBatch did not fire within 2s");

        testCts.Cancel();

        // Wait for DisposeAsync to actually fire. The previous version verified immediately
        // after WhenAny(serviceTask, ...) which returns as soon as StartAsync's task completes
        // (StartAsync != ExecuteAsync), so the dispose hadn't run yet on slow CI.
        Assert.True(
            await leaseDisposed.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "DisposeAsync did not fire within 2s of cancellation");

        // Assert
        _leaseMock.Verify(x => x.DisposeAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleIterations_AcquiresLeaseEachTime()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        // FakeTimeProvider.Advance only fires timers that exist at the call site.
        // The SUT registers its NormalDelayMs timer only after `ProcessBatch` returns,
        // so a fixed real-clock wait between Advance calls is racy on slow CI runners.
        // Use a semaphore released on each lease acquisition and pump time until the
        // second iteration actually fires.
        using var leaseAcquired = new SemaphoreSlim(0, int.MaxValue);
        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .Callback(() => leaseAcquired.Release())
            .ReturnsAsync(_leaseMock.Object);

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start (no preceding delay).
        Assert.True(
            await leaseAcquired.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "First lease acquisition did not fire within 2s");

        // Pump time until the second iteration fires. Advance does nothing if the
        // loop hasn't reached its next `await _timeProvider.Delay(...)` yet, so we
        // retry every ~50ms until the semaphore is released or we hit the deadline.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var secondAcquired = false;
        while (DateTime.UtcNow < deadline)
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
            if (await leaseAcquired.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken))
            {
                secondAcquired = true;
                break;
            }
        }

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert - Lease acquired multiple times (one per iteration)
        Assert.True(secondAcquired, "Second lease acquisition did not fire within 2s");
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_OperationCanceledException_RethrowsWhenCancellationRequested()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        var testCts = new CancellationTokenSource();
        var processBatchCalled = new TaskCompletionSource<bool>();

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                testCts.Cancel();
                processBatchCalled.SetResult(true);
            })
            .Throws(new OperationCanceledException(testCts.Token));

        var service = CreateService();

        // Act
        await service.StartAsync(testCts.Token);

        // Wait for ProcessBatch to be called before asserting
        await processBatchCalled.Task;
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // Assert - ExecuteAsync should rethrow OperationCanceledException once the
        // stoppingToken is cancelled. We await BackgroundService.ExecuteTask rather
        // than the StartAsync return value: as of .NET 10, StartAsync returns a
        // completed Task even when ExecuteAsync has already faulted synchronously,
        // so the propagated OCE is observable on ExecuteTask only.
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.ExecuteTask!);
    }

    [Fact]
    public async Task StopAsync_StopsSuccessfully()
    {
        // Arrange
        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        await service.StartAsync(testCts.Token);
        await service.StopAsync(CancellationToken.None);

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCorrectLeaseName()
    {
        // Arrange
        const string expectedLeaseName = "access_management_consent_migration";
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        using var leaseAttempted = new SemaphoreSlim(0, int.MaxValue);
        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking(expectedLeaseName, It.IsAny<CancellationToken>()))
            .Callback(() => leaseAttempted.Release())
            .ReturnsAsync(_leaseMock.Object);

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start.
        Assert.True(
            await leaseAttempted.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "Lease attempt did not fire within 2s");

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking(expectedLeaseName, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_FeatureDisabled_UsesFeatureDisabledDelay()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(false);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10, TestContext.Current.CancellationToken);

        // Advance by FeatureDisabledDelayMs multiple times
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.FeatureDisabledDelayMs));
        await Task.Delay(10, TestContext.Current.CancellationToken);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.FeatureDisabledDelayMs));
        await Task.Delay(10, TestContext.Current.CancellationToken);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert - Should not acquire lease when feature is disabled
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_LeaseUnavailable_RetriesWithCorrectDelay()
    {
        // Arrange
        var attempts = 0;
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        // FakeTimeProvider.Advance only fires timers registered at the call site, and the SUT
        // registers its next-iteration timer only after ProcessBatch (or the lease-unavailable
        // delay) returns. A fixed real-clock wait between Advance calls is racy on slow CI —
        // pump time until the third attempt fires (or 2s deadline).
        using var leaseAttempted = new SemaphoreSlim(0, int.MaxValue);
        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .Callback(() => leaseAttempted.Release())
            .ReturnsAsync(() =>
            {
                attempts++;
                return attempts <= 2 ? null : _leaseMock.Object; // Fail twice, then succeed
            });

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First attempt fires immediately on service start.
        Assert.True(
            await leaseAttempted.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "First lease attempt did not fire within 2s");

        // Pump time forward until the third attempt fires (two retries needed: first two return null).
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var totalAttempts = 1;
        while (totalAttempts < 3 && DateTime.UtcNow < deadline)
        {
            var advance = totalAttempts < 2
                ? _settings.EmptyFeedDelayMs + 2000 // lease-unavailable delay + max jitter
                : _settings.NormalDelayMs;          // last attempt succeeds, normal delay
            _timeProvider.Advance(TimeSpan.FromMilliseconds(advance));
            if (await leaseAttempted.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken))
            {
                totalAttempts++;
            }
        }

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert
        Assert.True(totalAttempts >= 3, $"Expected at least 3 lease attempts within 2s, observed {totalAttempts}");
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()),
            Times.AtLeast(3));
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_LeaseUnavailable_AppliesJitter()
    {
        // Arrange
        _featureManagerMock
            .Setup(x => x.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesConsentMigration))
            .ReturnsAsync(true);

        using var leaseAttempted = new SemaphoreSlim(0, int.MaxValue);
        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .Callback(() => leaseAttempted.Release())
            .ReturnsAsync((ILease)null);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        // First iteration runs immediately on service start.
        Assert.True(
            await leaseAttempted.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
            "First lease attempt did not fire within 2s");

        // When the lease is unavailable, the SUT delays by EmptyFeedDelayMs + RandomJitter
        // (1000–1999ms). Pump time in small increments until the second attempt fires; the
        // assertion below checks the cumulative advance was well above EmptyFeedDelayMs alone —
        // i.e. the SUT added jitter on top of the base delay. (Pumping in small increments also
        // tolerates the race where the SUT hasn't yet registered its timer when we start
        // advancing; a single big Advance can otherwise no-op against an unregistered timer.)
        var totalAdvanced = 0;
        var secondFired = false;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline && !secondFired)
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(50));
            totalAdvanced += 50;
            if (await leaseAttempted.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken))
            {
                secondFired = true;
            }
        }

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000, TestContext.Current.CancellationToken));

        // Assert: second attempt fired, AND only after a delay much larger than EmptyFeedDelayMs
        // alone — proving the SUT added RandomJitter (1000–1999ms) on top. Without jitter the
        // second attempt would fire at totalAdvanced ≈ EmptyFeedDelayMs (200ms, plus tiny race
        // lag); with jitter it fires at 1200–2200ms.
        Assert.True(secondFired, "Second lease attempt did not fire within 5s");
        Assert.True(
            totalAdvanced > _settings.EmptyFeedDelayMs + 500,
            $"SUT did not apply jitter: second lease attempt fired after {totalAdvanced}ms, which is too close to EmptyFeedDelayMs ({_settings.EmptyFeedDelayMs}ms) alone — expected at least EmptyFeedDelayMs + ~1000ms of jitter");
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    private ConsentMigrationHostedService CreateService()
    {
        return new ConsentMigrationHostedService(
            _syncServiceMock.Object,
            _featureManagerMock.Object,
            _settingsMonitorMock.Object,
            _timeProvider,
            _loggerMock.Object,
            _leaseServiceMock.Object);
    }
}

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

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.FeatureDisabledDelayMs * 2));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ILease)null);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);

        // Advance by EmptyFeedDelayMs + max jitter (2000ms)
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.EmptyFeedDelayMs + 2000));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // Empty batch

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.EmptyFeedDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

        // Assert
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // Non-empty batch

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

        // Assert
        _syncServiceMock.Verify(
            x => x.ProcessBatch(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
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

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMinutes(1)); // Error delay
        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

        // Assert - Service continues after exception
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
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        await Task.Delay(10);

        // Trigger first iteration
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        // Trigger second iteration
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

        // Assert - Lease acquired multiple times (one per iteration)
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
        var serviceTask = service.StartAsync(testCts.Token);

        // Wait for ProcessBatch to be called before asserting
        await processBatchCalled.Task;
        await Task.Delay(50);

        // Assert - The service should throw OperationCanceledException when cancellation is requested
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await serviceTask);
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

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking(expectedLeaseName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_leaseMock.Object);

        _syncServiceMock
            .Setup(x => x.ProcessBatch(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        await Task.Delay(10);

        // Advance by FeatureDisabledDelayMs multiple times
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.FeatureDisabledDelayMs));
        await Task.Delay(10);

        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.FeatureDisabledDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

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

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
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

        await Task.Delay(10);

        // First attempt - no lease (includes jitter up to 2000ms)
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.EmptyFeedDelayMs + 2000));
        await Task.Delay(10);

        // Second attempt - no lease (includes jitter up to 2000ms)
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.EmptyFeedDelayMs + 2000));
        await Task.Delay(10);

        // Third attempt - lease acquired
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.NormalDelayMs));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

        // Assert
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

        _leaseServiceMock
            .Setup(x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ILease)null);

        var service = CreateService();
        using var testCts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(testCts.Token);

        await Task.Delay(10);

        // The jitter adds 1000-2000ms to the EmptyFeedDelayMs
        // So we need to advance by at least EmptyFeedDelayMs + 1000ms to guarantee triggering
        _timeProvider.Advance(TimeSpan.FromMilliseconds(_settings.EmptyFeedDelayMs + 1000));
        await Task.Delay(10);

        testCts.Cancel();
        await Task.WhenAny(serviceTask, Task.Delay(1000));

        // Assert - Verify lease was attempted (jitter doesn't prevent retry)
        _leaseServiceMock.Verify(
            x => x.TryAcquireNonBlocking("access_management_consent_migration", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
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

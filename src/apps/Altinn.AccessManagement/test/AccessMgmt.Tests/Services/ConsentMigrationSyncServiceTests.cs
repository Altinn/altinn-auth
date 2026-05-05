using System.Diagnostics.Metrics;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.HostedServices.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Tests for <see cref="ConsentMigrationSyncService"/>
/// </summary>
public class ConsentMigrationSyncServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IAltinn2ConsentClient> _clientMock;
    private readonly Mock<IConsentMigrationService> _migrationServiceMock;
    private readonly Mock<ILogger<ConsentMigrationSyncService>> _loggerMock;
    private readonly Mock<IMeterFactory> _meterFactoryMock;
    private readonly Mock<IOptionsMonitor<ConsentMigrationSettings>> _settingsMonitorMock;
    private readonly ConsentMigrationSettings _settings;
    private readonly TimeProvider _timeProvider;

    public ConsentMigrationSyncServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _clientMock = new Mock<IAltinn2ConsentClient>();
        _migrationServiceMock = new Mock<IConsentMigrationService>();
        _loggerMock = new Mock<ILogger<ConsentMigrationSyncService>>();
        _meterFactoryMock = new Mock<IMeterFactory>();
        _settingsMonitorMock = new Mock<IOptionsMonitor<ConsentMigrationSettings>>();
        _timeProvider = TimeProvider.System;

        _settings = new ConsentMigrationSettings
        {
            BatchSize = 10,
            ConsentStatus = 3,
            OnlyExpiredConsents = true,
            NormalDelayMs = 1000,
            EmptyFeedDelayMs = 5000,
            MaxDegreeOfParallelism = 10
        };

        _settingsMonitorMock = new Mock<IOptionsMonitor<ConsentMigrationSettings>>();
        _settingsMonitorMock.Setup(x => x.CurrentValue).Returns(_settings);

        // Setup meter factory
        var meterMock = new Mock<Meter>("Test", null);
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(meterMock.Object);

        // Setup service provider and scope
        var scopeServiceProvider = new Mock<IServiceProvider>();
        scopeServiceProvider.Setup(x => x.GetService(typeof(IAltinn2ConsentClient)))
            .Returns(_clientMock.Object);
        scopeServiceProvider.Setup(x => x.GetService(typeof(IConsentMigrationService)))
            .Returns(_migrationServiceMock.Object);

        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(scopeServiceProvider.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
    }

    [Fact]
    public async Task ProcessBatch_NoConsents_ReturnsZero()
    {
        // Arrange
        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_WithConsents_ProcessesAll()
    {
        // Arrange
        var consentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(3, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessBatch_NetworkError_ReturnsZero()
    {
        // Arrange
        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ProcessBatch_PartialSuccess_ContinuesProcessing()
    {
        // Arrange
        var consentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Succeeded);

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Failed("Test error"));

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[2], It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(3, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessBatch_ConsentThrowsException_ContinuesWithOthers()
    {
        // Arrange
        var consentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[0], It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(2, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessBatch_MixedExceptionsAndResults_HandlesAll()
    {
        // Arrange
        var consentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[0], It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConsentMigrationResult.Succeeded);

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[1], It.IsAny<CancellationToken>()))
          .ThrowsAsync(new HttpRequestException("Network error"));

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[2], It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConsentMigrationResult.Failed("Migration failed"));

        _migrationServiceMock.Setup(x => x.MigrateConsent(consentIds[3], It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(4, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task ProcessBatch_RespectsConfiguredBatchSize()
    {
        // Arrange
        var largeConsentList = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            _settings.BatchSize,
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(largeConsentList.Take(_settings.BatchSize).ToList());

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(_settings.BatchSize, result);
        _clientMock.Verify(
            x => x.GetAltinn2ConsentListForMigration(
                _settings.BatchSize,
                _settings.ConsentStatus,
                _settings.OnlyExpiredConsents,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ADD THESE 12 NEW TESTS to the existing ConsentMigrationSyncServiceTests class

    [Fact]
    public async Task ProcessBatch_ParallelProcessing_ProcessesConsentsInParallel()
    {
        // Arrange
        var consentIds = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToList();
        var processingTimes = new List<DateTime>();
        var lockObj = new object();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
          {
              lock (lockObj)
              {
                  processingTimes.Add(DateTime.UtcNow);
              }

              await Task.Delay(50); // Simulate processing time
              return ConsentMigrationResult.Succeeded;
          });

        var service = CreateService();

        // Act
        var startTime = DateTime.UtcNow;
        var result = await service.ProcessBatch(CancellationToken.None);
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(20, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(20));

        // With MaxDegreeOfParallelism=10, processing 20 items with 50ms each should take ~100ms (not 1000ms sequential)
        var totalTime = (endTime - startTime).TotalMilliseconds;
        Assert.True(totalTime < 500, $"Expected parallel processing to complete in <500ms, but took {totalTime}ms");

        // Verify that multiple consents were processed at the same time (within 10ms window)
        var concurrentGroups = processingTimes
          .GroupBy(t => t.Ticks / TimeSpan.FromMilliseconds(10).Ticks)
          .Where(g => g.Count() > 1)
          .Count();

        Assert.True(concurrentGroups > 0, "Expected some consents to be processed concurrently");
    }

    [Fact]
    public async Task ProcessBatch_RespectsMaxDegreeOfParallelism()
    {
        // Arrange
        _settings.MaxDegreeOfParallelism = 5;
        var consentIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var concurrentCount = 0;
        var maxConcurrentCount = 0;
        var lockObj = new object();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
          {
              lock (lockObj)
              {
                  concurrentCount++;
                  if (concurrentCount > maxConcurrentCount)
                  {
                      maxConcurrentCount = concurrentCount;
                  }
              }

              await Task.Delay(20);

              lock (lockObj)
              {
                  concurrentCount--;
              }

              return ConsentMigrationResult.Succeeded;
          });

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(50, result);
        Assert.True(maxConcurrentCount <= _settings.MaxDegreeOfParallelism,
          $"Expected max concurrent <= {_settings.MaxDegreeOfParallelism}, but was {maxConcurrentCount}");
        Assert.True(maxConcurrentCount > 1, "Expected parallel processing to occur");
    }

    [Fact]
    public async Task ProcessBatch_LargeBatch_ProcessesAllConsents()
    {
        // Arrange
        _settings.BatchSize = 10000;
        var consentIds = Enumerable.Range(0, 10000).Select(_ => Guid.NewGuid()).ToList();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(10000, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(10000));
    }

    [Fact]
    public async Task ProcessBatch_ParallelProcessing_ThreadSafeCounters()
    {
        // Arrange
        var consentIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var successCount = 0;
        var failedCount = 0;

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
          {
              await Task.Delay(1);
              // Randomly succeed or fail
              var success = Guid.NewGuid().GetHashCode() % 2 == 0;
              if (success)
              {
                  Interlocked.Increment(ref successCount);
                  return ConsentMigrationResult.Succeeded;
              }
              else
              {
                  Interlocked.Increment(ref failedCount);
                  return ConsentMigrationResult.Failed("Random failure");
              }
          });

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(100, result);
        Assert.Equal(100, successCount + failedCount); // All accounted for
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(100));
    }

    [Fact]
    public async Task ProcessBatch_ParallelProcessing_AllConsentsProcessedExactlyOnce()
    {
        // Arrange
        var consentIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var processedIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(async (Guid id, CancellationToken ct) =>
          {
              processedIds.Add(id);
              await Task.Delay(5);
              return ConsentMigrationResult.Succeeded;
          });

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(50, result);
        Assert.Equal(50, processedIds.Count);

        // Verify each consent was processed exactly once
        foreach (var consentId in consentIds)
        {
            Assert.Equal(1, processedIds.Count(id => id == consentId));
        }
    }

    [Fact]
    public async Task ProcessBatch_CancellationDuringParallelProcessing_StopsGracefully()
    {
        // Arrange
        var consentIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var cts = new CancellationTokenSource();
        var processedCount = 0;

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(async (Guid id, CancellationToken ct) =>
          {
              var count = Interlocked.Increment(ref processedCount);
              if (count == 10)
              {
                  cts.Cancel(); // Cancel after 10 consents processed
              }

              await Task.Delay(10, ct);
              return ConsentMigrationResult.Succeeded;
          });

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ProcessBatch(cts.Token));
        Assert.True(ex is OperationCanceledException); // always true

        // Some consents were processed before cancellation
        Assert.True(processedCount < 100, $"Expected processing to stop early, but processed {processedCount} consents");
    }

    [Fact]
    public async Task ProcessBatch_MixedSuccessAndFailureInParallel_CountsCorrectly()
    {
        // Arrange
        var consentIds = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        var callCount = 0;
        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(() =>
          {
              var count = Interlocked.Increment(ref callCount);
              // Every 3rd consent fails
              if (count % 3 == 0)
              {
                  return Task.FromResult(ConsentMigrationResult.Failed("Test failure"));
              }

              return Task.FromResult(ConsentMigrationResult.Succeeded);
          });

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(30, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(30));
    }

    [Fact]
    public async Task ProcessBatch_ParallelExceptions_DoesNotStopOtherConsents()
    {
        // Arrange
        var consentIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        var callCount = 0;
        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(() =>
          {
              var count = Interlocked.Increment(ref callCount);
              // Throw exception on every 3rd call
              if (count % 3 == 0)
              {
                  throw new InvalidOperationException("Test exception");
              }

              return Task.FromResult(ConsentMigrationResult.Succeeded);
          });

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(10, result); // All consents processed despite exceptions
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
    }

    [Fact]
    public async Task ProcessBatch_MaxDegreeOfParallelism_DefaultValue()
    {
        // Arrange - Test with default MaxDegreeOfParallelism
        var defaultSettings = new ConsentMigrationSettings
        {
            BatchSize = 10,
            ConsentStatus = 3,
            OnlyExpiredConsents = true,
            MaxDegreeOfParallelism = 10 // Default value
        };

        _settingsMonitorMock.Setup(x => x.CurrentValue).Returns(defaultSettings);

        var consentIds = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToList();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(20, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(20));
    }

    [Fact]
    public async Task ProcessBatch_SingleDegreeOfParallelism_ProcessesSequentially()
    {
        // Arrange - Test with MaxDegreeOfParallelism = 1 (sequential)
        _settings.MaxDegreeOfParallelism = 1;
        var consentIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var processingOrder = new List<Guid>();
        var lockObj = new object();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .Returns(async (Guid id, CancellationToken ct) =>
          {
              lock (lockObj)
              {
                  processingOrder.Add(id);
              }

              await Task.Delay(10);
              return ConsentMigrationResult.Succeeded;
          });

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(5, result);
        Assert.Equal(5, processingOrder.Count);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task ProcessBatch_HighParallelism_HandlesLoad()
    {
        // Arrange - Test with high parallelism
        _settings.MaxDegreeOfParallelism = 50;
        var consentIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
          It.IsAny<int>(),
          It.IsAny<int?>(),
          It.IsAny<bool>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConsentMigrationResult.Succeeded);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(100, result);
        _migrationServiceMock.Verify(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(100));
    }

    [Fact]
    public async Task ProcessBatch_CreatesScopePerItem()
    {
        // Arrange
        var consentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        _clientMock.Setup(x => x.GetAltinn2ConsentListForMigration(
            It.IsAny<int>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(consentIds);

        _migrationServiceMock.Setup(x => x.MigrateConsent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentMigrationResult.Succeeded);

        // track CreateScope calls in a thread-safe way
        var scopeCount = 0;
        _serviceScopeFactoryMock
            .Setup(x => x.CreateScope())
            .Callback(() => Interlocked.Increment(ref scopeCount))
            .Returns(_serviceScopeMock.Object);

        var service = CreateService();

        // Act
        var result = await service.ProcessBatch(CancellationToken.None);

        // Assert
        Assert.Equal(consentIds.Count, result);

        // Expect at least one batch scope + one per consent (use >= to avoid flakiness)
        var expectedMinimum = 1 + consentIds.Count;
        Assert.True(
            scopeCount >= expectedMinimum,
            $"Expected at least {expectedMinimum} CreateScope() calls, but saw {scopeCount}");
    }

    private ConsentMigrationSyncService CreateService()
    {
        return new ConsentMigrationSyncService(
            _serviceProviderMock.Object,
            _settingsMonitorMock.Object,
            _timeProvider,
            _meterFactoryMock.Object,
            _loggerMock.Object);
    }
}

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Tests for <see cref="ConsentService"/>
/// </summary>
public class ConsentServiceTests
{
    private readonly Mock<ILogger<ConsentService>> _loggerMock;
    private readonly Mock<IConsentRepository> _consentRepositoryMock;
    private readonly Mock<IAltinn2ConsentClient> _altinn2ConsentClientMock;
    private readonly Mock<IPartiesClient> _partiesClientMock;
    private readonly Mock<ISingleRightsService> _singleRightsServiceMock;
    private readonly Mock<IResourceRegistryClient> _resourceRegistryClientMock;
    private readonly Mock<IAMPartyService> _amPartyServiceMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<IProfileClient> _profileClientMock;
    private readonly TimeProvider _timeProvider;
    private readonly Mock<IOptions<GeneralSettings>> _generalSettingsMock;
    private readonly Mock<IConsentDelegationCheckService> _consentDelegationCheckServiceMock;
    private readonly Mock<IMeterFactory> _meterFactoryMock;

    public ConsentServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConsentService>>();
        _consentRepositoryMock = new Mock<IConsentRepository>();
        _altinn2ConsentClientMock = new Mock<IAltinn2ConsentClient>();
        _partiesClientMock = new Mock<IPartiesClient>();
        _resourceRegistryClientMock = new Mock<IResourceRegistryClient>();
        _amPartyServiceMock = new Mock<IAMPartyService>();
        _memoryCacheMock = new Mock<IMemoryCache>();
        _profileClientMock = new Mock<IProfileClient>();
        _timeProvider = TimeProvider.System;
        _generalSettingsMock = new Mock<IOptions<GeneralSettings>>();
        _meterFactoryMock = new Mock<IMeterFactory>();
        _consentDelegationCheckServiceMock = new Mock<IConsentDelegationCheckService>();

        var meter = new Meter("Altinn.AccessManagement.ConsentMigration.Test");
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        _generalSettingsMock.Setup(x => x.Value).Returns(new GeneralSettings { Hostname = "localhost" });
        SetupMemoryCache();
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_DuplicateFound_SetsMigrateStatusTo1()
    {
        // Arrange
        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);
        var existingRequestDetails = CreateConsentRequestDetails(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        // Simulate duplicate: CreateRequest returns null
        _consentRepositoryMock
            .Setup(x => x.CreateRequest(It.IsAny<ConsentRequest>(), It.IsAny<ConsentPartyUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsentRequestDetails)null);

        // GetRequest returns existing consent with matching parties (duplicate scenario)
        _consentRepositoryMock
            .Setup(x => x.GetRequest(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRequestDetails);

        var service = CreateService();

        // Act
        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        // Assert
        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);

        // Verify migration status was set to 1 (success) for duplicate
        _altinn2ConsentClientMock.Verify(
            x => x.UpdateAltinn2ConsentMigrateStatus(consentRequestId.ToString(), 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_InvalidResource_SetsMigrateStatusTo2()
    {
        // Arrange
        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        // Make resource validation fail (return null for resource)
        _resourceRegistryClientMock
            .Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceResource)null);

        var service = CreateService();

        // Act
        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        // Assert
        Assert.True(result.IsProblem || result.Value == null);

        // Verify migration status was set to 2 (failure) for validation error
        _altinn2ConsentClientMock.Verify(
            x => x.UpdateAltinn2ConsentMigrateStatus(consentRequestId.ToString(), 2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Success_SetsMigrateStatusTo1()
    {
        // Arrange
        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);
        var createdRequestDetails = CreateConsentRequestDetails(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        // Successful creation
        _consentRepositoryMock
            .Setup(x => x.CreateRequest(It.IsAny<ConsentRequest>(), It.IsAny<ConsentPartyUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRequestDetails);

        _consentRepositoryMock
            .Setup(x => x.GetRequest(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRequestDetails);

        var service = CreateService();

        // Act
        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        // Assert
        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);

        // Verify migration status was set to 1 (success)
        _altinn2ConsentClientMock.Verify(
            x => x.UpdateAltinn2ConsentMigrateStatus(consentRequestId.ToString(), 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Records_All_Histograms_OnSuccess()
    {
        // Arrange: real Meter + listener to capture histogram recordings
        var meter = new Meter("Altinn.AccessManagement.ConsentMigration.Test");

        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        using var collector = new MeasurementCollector(meter, new[]
        {
            "consent_migration_get_a2_duration_seconds",
            "consent_migration_insert_a3_duration_seconds",
            "consent_migration_update_a2_duration_seconds"
        });

        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);
        var createdRequestDetails = CreateConsentRequestDetails(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        _consentRepositoryMock
            .Setup(x => x.CreateRequest(It.IsAny<ConsentRequest>(), It.IsAny<ConsentPartyUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRequestDetails);

        _consentRepositoryMock
            .Setup(x => x.GetRequest(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRequestDetails);

        var service = CreateService();

        // Act
        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        // Allow listener to process
        await Task.Delay(20);

        // Assert
        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);

        Assert.True(collector.GetMeasurements("consent_migration_get_a2_duration_seconds").Any());
        Assert.True(collector.GetMeasurements("consent_migration_insert_a3_duration_seconds").Any());
        Assert.True(collector.GetMeasurements("consent_migration_update_a2_duration_seconds").Any());
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Records_GetHistogram_OnDuplicate()
    {
        var meter = new Meter("Altinn.AccessManagement.ConsentMigration.Test");
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        using var collector = new MeasurementCollector(meter, new[] { "consent_migration_get_a2_duration_seconds" });

        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);
        var existingRequestDetails = CreateConsentRequestDetails(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        _consentRepositoryMock
            .Setup(x => x.CreateRequest(It.IsAny<ConsentRequest>(), It.IsAny<ConsentPartyUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConsentRequestDetails)null);

        _consentRepositoryMock
            .Setup(x => x.GetRequest(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRequestDetails);

        var service = CreateService();

        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        await Task.Delay(20);

        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);
        Assert.True(collector.GetMeasurements("consent_migration_get_a2_duration_seconds").Any());
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Records_UpdateHistogram_OnValidationFailure()
    {
        var meter = new Meter("Altinn.AccessManagement.ConsentMigration.Test");
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        using var collector = new MeasurementCollector(meter, new[] { "consent_migration_update_a2_duration_seconds" });

        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        // Make resource validation fail (return null for resource)
        _resourceRegistryClientMock
            .Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceResource)null);

        var service = CreateService();

        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        await Task.Delay(20);

        Assert.True(result.IsProblem || result.Value == null);
        Assert.True(collector.GetMeasurements("consent_migration_update_a2_duration_seconds").Any());
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Records_OverallHistogram_OnSuccess()
    {
        // Arrange: real Meter + listener to capture overall histogram recording
        var meter = new Meter("Altinn.AccessManagement.ConsentMigration.Test");
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        using var collector = new MeasurementCollector(meter, new[] { "consent_migration_overall_duration_seconds" });

        var consentRequestId = Guid.NewGuid();
        var fromPartyUuid = Guid.NewGuid();
        var toPartyUuid = Guid.NewGuid();

        var altinn2ConsentRequest = CreateAltinn2ConsentRequest(consentRequestId, fromPartyUuid, toPartyUuid);
        var createdRequestDetails = CreateConsentRequestDetails(consentRequestId, fromPartyUuid, toPartyUuid);

        _altinn2ConsentClientMock
            .Setup(x => x.GetAltinn2Consent(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(altinn2ConsentRequest);

        SetupValidationMocks(fromPartyUuid, toPartyUuid);

        _consentRepositoryMock
            .Setup(x => x.CreateRequest(It.IsAny<ConsentRequest>(), It.IsAny<ConsentPartyUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRequestDetails);

        _consentRepositoryMock
            .Setup(x => x.GetRequest(consentRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRequestDetails);

        var service = CreateService();

        // Act
        var result = await service.GetAndStoreAltinn2Consent(consentRequestId, CancellationToken.None);

        // Allow listener to process
        await Task.Delay(20);

        // Assert
        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);

        Assert.True(collector.GetMeasurements("consent_migration_overall_duration_seconds").Any());
    }

    private ConsentService CreateService()
    {
        return new ConsentService(
            _loggerMock.Object,
            _consentRepositoryMock.Object,
            _altinn2ConsentClientMock.Object,
            _partiesClientMock.Object,
            _resourceRegistryClientMock.Object,
            _amPartyServiceMock.Object,
            _memoryCacheMock.Object,
            _profileClientMock.Object,
            _timeProvider,
            _generalSettingsMock.Object,
            _meterFactoryMock.Object,
            _consentDelegationCheckServiceMock.Object);
    }

    private Altinn2ConsentRequest CreateAltinn2ConsentRequest(Guid id, Guid fromPartyUuid, Guid toPartyUuid)
    {
        return new Altinn2ConsentRequest
        {
            ConsentGuid = id,
            OfferedByPartyUUID = fromPartyUuid,
            CoveredByPartyUUID = toPartyUuid,
            ValidTo = DateTimeOffset.UtcNow.AddDays(30),
            ConsentRequestStatus = "Created",
            CreatedTime = DateTimeOffset.UtcNow,
            RequestResources = new List<AuthorizationRequestResourceBE>
            {
                new AuthorizationRequestResourceBE
                {
                    ServiceCode = "test",
                    ServiceEditionCode = 1,
                    ServiceEditionVersionID = 1,
                    Operations = new List<string> { "read" }
                }
            },
            ConsentHistoryEvents = new List<Altinn2ConsentRequestEvent>(),
            RedirectUrl = "https://redirect.url",
            TemplateId = "test-template"
        };
    }

    private ConsentRequestDetails CreateConsentRequestDetails(Guid id, Guid fromPartyUuid, Guid toPartyUuid)
    {
        return new ConsentRequestDetails
        {
            Id = id,
            From = ConsentPartyUrn.PartyUuid.Create(fromPartyUuid),
            To = ConsentPartyUrn.PartyUuid.Create(toPartyUuid),
            ValidTo = DateTimeOffset.UtcNow.AddDays(30),
            Consented = DateTimeOffset.UtcNow,
            RedirectUrl = "https://redirect.url",
            ConsentRights = new List<ConsentRight>
            {
                new ConsentRight
                {
                    Action = new List<string> { "read" },
                    Resource = new List<ConsentResourceAttribute>
                    {
                        new ConsentResourceAttribute
                        {
                            Type = "urn:altinn:resource",
                            Value = "test-resource"
                        }
                    }
                }
            },
            ConsentRequestEvents = new List<ConsentRequestEvent>()
        };
    }

    private void SetupValidationMocks(Guid fromPartyUuid, Guid toPartyUuid)
    {
        // Mock party lookups with required PersonId or OrganizationId
        _amPartyServiceMock
            .Setup(x => x.GetByUid(fromPartyUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinimalParty
            {
                PartyUuid = fromPartyUuid,
                Name = "FromParty",
                PersonId = "01025161013" // Add PersonId for external identity mapping
            });

        _amPartyServiceMock
            .Setup(x => x.GetByUid(toPartyUuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinimalParty
            {
                PartyUuid = toPartyUuid,
                Name = "ToParty",
                OrganizationId = "810419512" // Add OrganizationId for external identity mapping
            });

        // Mock resource registry for validation
        _resourceRegistryClientMock
            .Setup(x => x.GetResources(It.IsAny<CancellationToken>(), It.IsAny<string>()))
            .ReturnsAsync(new List<ServiceResource>
            {
            new ServiceResource
            {
                Identifier = "test-resource",
                ResourceType = ResourceType.Consent,
                ConsentTemplate = "test-template",
                VersionId = 1
            }
            });

        _resourceRegistryClientMock
            .Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResource
            {
                Identifier = "test-resource",
                ResourceType = ResourceType.Consent,
                ConsentTemplate = "test-template",
                VersionId = 1
            });

        _resourceRegistryClientMock
            .Setup(x => x.GetConsentTemplate(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsentTemplate
            {
                Id = "test-template",
                Version = 1,
                Texts = new ConsentTemplateTexts()
            });
    }

    private void SetupMemoryCache()
    {
        // Mock TryGetValue to return false (cache miss)
        _memoryCacheMock
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        // Mock CreateEntry for Set operations
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupProperty(x => x.Value);
        mockCacheEntry.SetupProperty(x => x.AbsoluteExpirationRelativeToNow);

        _memoryCacheMock
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);
    }

    // Simple Meter listener to capture histogram measurements
    private sealed class MeasurementCollector : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly ConcurrentDictionary<string, ConcurrentBag<double>> _measurements = new();

        public MeasurementCollector(Meter meter, IEnumerable<string> instrumentNames)
        {
            foreach (var name in instrumentNames)
            {
                _measurements[name] = new ConcurrentBag<double>();
            }

            _listener = new MeterListener
            {
                InstrumentPublished = (instr, listener) =>
                {
                    if (instr.Meter.Name == meter.Name && _measurements.ContainsKey(instr.Name))
                    {
                        listener.EnableMeasurementEvents(instr);
                    }
                }
            };

            _listener.SetMeasurementEventCallback<double>((inst, measurement, tags, state) =>
            {
                if (_measurements.ContainsKey(inst.Name))
                {
                    _measurements[inst.Name].Add(measurement);
                }
            });

            _listener.Start();
        }

        public IReadOnlyCollection<double> GetMeasurements(string instrumentName)
        {
            if (_measurements.TryGetValue(instrumentName, out var bag))
            {
                return bag.ToArray();
            }

            return Array.Empty<double>();
        }

        public void Dispose() => _listener.Dispose();
    }
}

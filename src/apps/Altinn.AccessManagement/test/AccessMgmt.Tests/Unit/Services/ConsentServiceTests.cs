using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
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
using ContractsOrganizationNumber = Altinn.Authorization.Api.Contracts.Register.OrganizationNumber;
using ContractsPersonIdentifier = Altinn.Authorization.Api.Contracts.Register.PersonIdentifier;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="ConsentService"/>
/// </summary>
[UnitTest]
public class ConsentServiceTests
{
    private readonly Mock<ILogger<ConsentService>> _loggerMock;
    private readonly Mock<IConsentRepository> _consentRepositoryMock;
    private readonly Mock<IAltinn2ConsentClient> _altinn2ConsentClientMock;
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
    public async Task GetAndStoreAltinn2Consent_Success_RecordsAllHistograms()
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

        await collector.WaitForMeasurementsAsync(
            TestContext.Current.CancellationToken,
            "consent_migration_get_a2_duration_seconds",
            "consent_migration_insert_a3_duration_seconds",
            "consent_migration_update_a2_duration_seconds");

        // Assert
        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);

        Assert.True(collector.GetMeasurements("consent_migration_get_a2_duration_seconds").Any());
        Assert.True(collector.GetMeasurements("consent_migration_insert_a3_duration_seconds").Any());
        Assert.True(collector.GetMeasurements("consent_migration_update_a2_duration_seconds").Any());
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Duplicate_RecordsGetHistogram()
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

        await collector.WaitForMeasurementsAsync(
            TestContext.Current.CancellationToken,
            "consent_migration_get_a2_duration_seconds");

        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);
        Assert.True(collector.GetMeasurements("consent_migration_get_a2_duration_seconds").Any());
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_ValidationFailure_RecordsUpdateHistogram()
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

        await collector.WaitForMeasurementsAsync(
            TestContext.Current.CancellationToken,
            "consent_migration_update_a2_duration_seconds");

        Assert.True(result.IsProblem || result.Value == null);
        Assert.True(collector.GetMeasurements("consent_migration_update_a2_duration_seconds").Any());
    }

    [Fact]
    public async Task GetAndStoreAltinn2Consent_Success_RecordsOverallHistogram()
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

        await collector.WaitForMeasurementsAsync(
            TestContext.Current.CancellationToken,
            "consent_migration_overall_duration_seconds");

        // Assert
        Assert.False(result.IsProblem);
        Assert.NotNull(result.Value);

        Assert.True(collector.GetMeasurements("consent_migration_overall_duration_seconds").Any());
    }

    // =======================================================================
    // GetConsentStatusChangesForParty — Moq-based service unit tests
    //
    // The service is a thin orchestration over `IConsentRepository` plus a
    // call to `MapFromExternalIdenity`, which itself routes through
    // `IAMPartyService`. There's no DB and no HTTP pipeline involved, so per
    // docs/testing/WRITING_TESTS.md the right test type is a direct unit
    // test with hand-stubbed Moq dependencies — no fixture, no HttpClient.
    //
    // The shared mocks (`_consentRepositoryMock`, `_amPartyServiceMock`, …)
    // and the `CreateService()` factory below are reused so each test only
    // sets up the behaviour it actually exercises.
    //
    // Test naming follows MethodUnderTest_Scenario_ExpectedResult; assertions
    // use FluentAssertions (`.Should()`).
    // =======================================================================
    [Fact]
    public async Task GetConsentStatusChangesForParty_RepositoryReturnsList_ReturnsValueUnchanged()
    {
        // Arrange — caller already supplies a PartyUuid, so MapFromExternalIdenity
        // is a no-op and we don't need to mock IAMPartyService for this case.
        var partyUuid = Guid.NewGuid();
        var receiver = ConsentPartyUrn.PartyUuid.Create(partyUuid);

        var repoResult = new List<ConsentStatusChange>
        {
            new()
            {
                ConsentRequestId = Guid.NewGuid(),
                EventType = ConsentRequestEventType.Accepted,
                ChangedDate = DateTimeOffset.UtcNow,
                ConsentEventId = Guid.NewGuid(),
            },
        };

        var query = new ConsentEventsQuery(null, null, null, null, null);

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(partyUuid, query, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoResult);

        var service = CreateService();

        // Act
        var result = await service.GetConsentEventsForParty(receiver, query, pageSize: 100, CancellationToken.None);

        // Assert
        result.IsProblem.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(repoResult);

        // The repo must be called with the *resolved* internal partyUuid, not
        // the external identity. Verify with the exact partyUuid we expect.
        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(partyUuid, query, 100, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConsentStatusChangesForParty_OrganizationIdReceiver_ResolvesToInternalPartyUuidBeforeQuery()
    {
        // Arrange — caller supplies an external OrganizationId. The service
        // is expected to resolve it to an internal partyUuid via
        // IAMPartyService.GetByOrgNo before calling the repository. This
        // mapping is the only non-trivial step the service performs, so it
        // gets its own focused test.
        var orgNumber = ContractsOrganizationNumber.Parse("810419512");
        var receiver = ConsentPartyUrn.OrganizationId.Create(orgNumber);
        var resolvedPartyUuid = Guid.NewGuid();

        _amPartyServiceMock
            .Setup(s => s.GetByOrgNo(orgNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinimalParty
            {
                PartyUuid = resolvedPartyUuid,
                Name = "SmekkFull Bank AS",
                OrganizationId = "810419512",
            });

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(resolvedPartyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConsentStatusChange>());

        var service = CreateService();
        var query = new ConsentEventsQuery(null, null, null, null, new Guid("cd12b899-0795-4a3c-a65f-f8de792ff382"));

        // Act
        var result = await service.GetConsentEventsForParty(receiver, query, pageSize: 25, CancellationToken.None);

        // Assert
        result.IsProblem.Should().BeFalse();

        _amPartyServiceMock.Verify(
            s => s.GetByOrgNo(orgNumber, It.IsAny<CancellationToken>()),
            Times.Once,
            "the org number must be resolved through the AM party service before the repository is queried");

        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(resolvedPartyUuid, query, 25, It.IsAny<CancellationToken>()),
            Times.Once,
            "the repository must receive the resolved partyUuid plus the caller's pagination args verbatim");
    }

    [Fact]
    public async Task GetConsentStatusChangesForParty_RepositoryReturnsProblem_PropagatesProblemUnchanged()
    {
        // Arrange — `Result<T>` has an implicit conversion from
        // `ProblemDescriptor`, so we can stub the repository to return any
        // existing Problem. We pick `ConsentNotFound` only because it's
        // already wired up; the assertion is on the propagation, not the
        // specific descriptor.
        var partyUuid = Guid.NewGuid();
        var receiver = ConsentPartyUrn.PartyUuid.Create(partyUuid);

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(partyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Problems.ConsentNotFound);

        var service = CreateService();
        var query = new ConsentEventsQuery(null, null, null, null, null);

        // Act
        var result = await service.GetConsentEventsForParty(receiver, query, pageSize: 100, CancellationToken.None);

        // Assert — `Result<T>.Problem` exposes a `ProblemInstance` materialised
        // from the descriptor, not the descriptor itself, so `BeSameAs` would
        // fail. Compare on the stable identifying fields (error code + status)
        // instead. This is the same pattern used elsewhere in the suite when
        // asserting on propagated problems.
        result.IsProblem.Should().BeTrue();
        result.Problem.ErrorCode.Should().Be(Problems.ConsentNotFound.ErrorCode);
        result.Problem.StatusCode.Should().Be(Problems.ConsentNotFound.StatusCode);
    }

    [Fact]
    public async Task GetConsentStatusChangesForParty_PersonIdReceiver_ResolvesViaGetByPersonNo()
    {
        // Mirror of the OrganizationId test for the other branch of MapFromExternalIdenity: a PersonId
        // receiver must be resolved to an internal partyUuid via GetByPersonNo before the repository call.
        var personIdentifier = ContractsPersonIdentifier.Parse("01025161013");
        var receiver = ConsentPartyUrn.PersonId.Create(personIdentifier);
        var resolvedPartyUuid = Guid.NewGuid();

        _amPartyServiceMock
            .Setup(s => s.GetByPersonNo(personIdentifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinimalParty { PartyUuid = resolvedPartyUuid, Name = "Test Person" });

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(resolvedPartyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConsentStatusChange>());

        var service = CreateService();
        var query = new ConsentEventsQuery(null, null, null, null, null);

        var result = await service.GetConsentEventsForParty(receiver, query, pageSize: 50, CancellationToken.None);

        result.IsProblem.Should().BeFalse();
        _amPartyServiceMock.Verify(s => s.GetByPersonNo(personIdentifier, It.IsAny<CancellationToken>()), Times.Once);
        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(resolvedPartyUuid, query, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConsentEventsForParty_OrganizationIdReceiverNotFound_ReturnsInvalidOrganizationIdentifierProblem()
    {
        // Arrange — external OrganizationId that the register can't resolve.
        // GetByOrgNo returns null, so MapFromExternalIdenity returns null, and
        // the service surfaces InvalidOrganizationIdentifier rather than
        // dereferencing the null urn.
        var orgNumber = ContractsOrganizationNumber.Parse("810419512");
        var receiver = ConsentPartyUrn.OrganizationId.Create(orgNumber);

        _amPartyServiceMock
            .Setup(s => s.GetByOrgNo(orgNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MinimalParty)null);

        var service = CreateService();
        var query = new ConsentEventsQuery(null, null, null, null, null);

        // Act
        var result = await service.GetConsentEventsForParty(receiver, query, pageSize: 100, CancellationToken.None);

        // Assert
        result.IsProblem.Should().BeTrue();
        result.Problem.ErrorCode.Should().Be(Problems.InvalidOrganizationIdentifier.ErrorCode);
        result.Problem.StatusCode.Should().Be(Problems.InvalidOrganizationIdentifier.StatusCode);

        // The repository must never be reached when the receiver can't be resolved.
        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(It.IsAny<Guid>(), It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetConsentEventsForParty_PersonIdReceiverNotFound_ReturnsInvalidPersonIdentifierProblem()
    {
        // Arrange — external PersonId that the register can't resolve.
        // GetByPersonNo returns null → InvalidPersonIdentifier.
        var personIdentifier = ContractsPersonIdentifier.Parse("01025161013");
        var receiver = ConsentPartyUrn.PersonId.Create(personIdentifier);

        _amPartyServiceMock
            .Setup(s => s.GetByPersonNo(personIdentifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MinimalParty)null);

        var service = CreateService();
        var query = new ConsentEventsQuery(null, null, null, null, null);

        // Act
        var result = await service.GetConsentEventsForParty(receiver, query, pageSize: 100, CancellationToken.None);

        // Assert
        result.IsProblem.Should().BeTrue();
        result.Problem.ErrorCode.Should().Be(Problems.InvalidPersonIdentifier.ErrorCode);
        result.Problem.StatusCode.Should().Be(Problems.InvalidPersonIdentifier.StatusCode);

        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(It.IsAny<Guid>(), It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetConsentStatusChangesForParty_RepositoryReturnsEmptyList_ReturnsOkWithEmptyList()
    {
        // The controller materialises this into a paginated response with no next link, so an empty
        // (not null) collection matters.
        var partyUuid = Guid.NewGuid();
        var receiver = ConsentPartyUrn.PartyUuid.Create(partyUuid);

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(partyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConsentStatusChange>());

        var service = CreateService();

        var result = await service.GetConsentEventsForParty(receiver, new ConsentEventsQuery(null, null, null, null, null), pageSize: 100, CancellationToken.None);

        result.IsProblem.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task GetConsentStatusChangesForParty_PassesQueryAndPageSizeVerbatim(int pageSize)
    {
        // Page-size clamping lives in the repository, so the service must forward the query object and
        // the page size unchanged for every valid page size.
        var partyUuid = Guid.NewGuid();
        var receiver = ConsentPartyUrn.PartyUuid.Create(partyUuid);
        var query = new ConsentEventsQuery(null, null, null, null, new Guid("cd12b899-0795-4a3c-a65f-f8de792ff382"));

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(partyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConsentStatusChange>());

        var service = CreateService();

        await service.GetConsentEventsForParty(receiver, query, pageSize, CancellationToken.None);

        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(partyUuid, query, pageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConsentStatusChangesForParty_CancellationTokenForwarded()
    {
        // Catches "default-token" regressions where the parameter is dropped on the way through.
        var partyUuid = Guid.NewGuid();
        var receiver = ConsentPartyUrn.PartyUuid.Create(partyUuid);
        using var cts = new CancellationTokenSource();

        _consentRepositoryMock
            .Setup(r => r.GetConsentEventsForParty(partyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConsentStatusChange>());

        var service = CreateService();

        await service.GetConsentEventsForParty(receiver, new ConsentEventsQuery(null, null, null, null, null), pageSize: 100, cts.Token);

        _consentRepositoryMock.Verify(
            r => r.GetConsentEventsForParty(partyUuid, It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.Is<CancellationToken>(t => t == cts.Token)),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // Where the *other* tests for this feature live:
    //
    //  - Controller (Altinn.AccessManagement.Api.Enterprise.Controllers.ConsentController):
    //    The integration tests in
    //    `AccessMgmt.Tests/Controllers/Enterprise/ConsentControllerTestEnterpriseFetchStatusChanges.cs`
    //    cover auth (401/403), happy path, paging, and tie-breaking. Those
    //    use the (legacy) `LegacyApiFixture`. New controller tests should
    //    prefer either:
    //      a) a direct unit test that instantiates `ConsentController` with
    //         `Mock<IConsent>` + a stubbed `ClaimsPrincipal` — fastest, and
    //         enough for the controller's new branches (continuation-link
    //         building, `Unauthorized` when there's no party in the token,
    //         `result.Problem.ToActionResult()` propagation), or
    //      b) `ApiFixture` (docs/testing/FIXTURES.md) when the test needs
    //         the full MVC pipeline (model binding, auth policies, routing).
    //    Do not add new consumers of `LegacyApiFixture` (see FIXTURES.md
    //    section 3).
    //
    //  - Repository (Altinn.AccessManagement.Persistence.Consent.ConsentRepository):
    //    `GetConsentStatusChangesForParty` is mostly a SQL query plus base64
    //    cursor parsing. The cursor-parsing branch (invalid token → starts
    //    from beginning, valid token → resumes) and the "latest event per
    //    consentrequest" projection are the things worth pinning down.
    //    These need a real Postgres → write them in
    //    `Altinn.AccessMgmt.PersistenceEF.Tests` against `EFPostgresFactory`
    //    (template-cloned DB; ~100–500 ms per test). See
    //    docs/testing/FIXTURES.md "EFPostgresFactory" for the seeding
    //    strategy.
    // -----------------------------------------------------------------------
    private ConsentService CreateService()
    {
        return new ConsentService(
            null, // AppDbContext not needed for these tests
            _loggerMock.Object,
            _consentRepositoryMock.Object,
            _altinn2ConsentClientMock.Object,
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

        private bool HasMeasurements(string instrumentName) =>
            _measurements.TryGetValue(instrumentName, out var bag) && !bag.IsEmpty;

        /// <summary>
        /// Polls until every named instrument has at least one measurement, or the
        /// timeout elapses (then throws). Replaces a fixed <c>Task.Delay</c> "let the
        /// listener catch up" wait: returns as soon as the metrics are observed and
        /// fails loudly — instead of silently — if they never arrive.
        /// </summary>
        public async Task WaitForMeasurementsAsync(CancellationToken cancellationToken, params string[] instrumentNames)
        {
            var deadline = Environment.TickCount64 + 5000;
            while (instrumentNames.Any(name => !HasMeasurements(name)))
            {
                if (Environment.TickCount64 >= deadline)
                {
                    var missing = string.Join(", ", instrumentNames.Where(name => !HasMeasurements(name)));
                    throw new TimeoutException($"No measurements for instrument(s) [{missing}] within 5000 ms.");
                }

                await Task.Delay(10, cancellationToken);
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}

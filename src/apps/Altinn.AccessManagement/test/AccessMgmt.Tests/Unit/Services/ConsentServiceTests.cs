using System.Collections.Concurrent;
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
    private readonly Mock<IResourceRegistryClient> _resourceRegistryClientMock;
    private readonly Mock<IAMPartyService> _amPartyServiceMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly TimeProvider _timeProvider;
    private readonly Mock<IOptions<GeneralSettings>> _generalSettingsMock;
    private readonly Mock<IConsentDelegationCheckService> _consentDelegationCheckServiceMock;

    public ConsentServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConsentService>>();
        _consentRepositoryMock = new Mock<IConsentRepository>();
        _resourceRegistryClientMock = new Mock<IResourceRegistryClient>();
        _amPartyServiceMock = new Mock<IAMPartyService>();
        _memoryCacheMock = new Mock<IMemoryCache>();
        _timeProvider = TimeProvider.System;
        _generalSettingsMock = new Mock<IOptions<GeneralSettings>>();
        _consentDelegationCheckServiceMock = new Mock<IConsentDelegationCheckService>();

        _generalSettingsMock.Setup(x => x.Value).Returns(new GeneralSettings { Hostname = "localhost" });
        SetupMemoryCache();
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
    // use AwesomeAssertions (`.Should()`).
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
            _resourceRegistryClientMock.Object,
            _amPartyServiceMock.Object,
            _memoryCacheMock.Object,
            _timeProvider,
            _generalSettingsMock.Object,
            _consentDelegationCheckServiceMock.Object);
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
}

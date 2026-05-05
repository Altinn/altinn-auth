using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.Authorization.Api.Contracts.Register;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Tests for <see cref="ConsentMigrationService"/>
/// </summary>
public class ConsentMigrationServiceTests
{
    private readonly Mock<IConsent> _consentServiceMock;
    private readonly Mock<ILogger<ConsentMigrationService>> _loggerMock;

    public ConsentMigrationServiceTests()
    {
        _consentServiceMock = new Mock<IConsent>();
        _loggerMock = new Mock<ILogger<ConsentMigrationService>>();
    }

    [Fact]
    public async Task MigrateConsent_Success_ReturnsSucceeded()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var consentDetails = new ConsentRequestDetails
        {
            Id = consentId,
            From = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()),
            To = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()),
            ValidTo = DateTimeOffset.UtcNow.AddDays(1),
            ConsentRights = new List<ConsentRight>(),
            ConsentRequestEvents = new List<ConsentRequestEvent>(),
            RedirectUrl = "https://example.com/redirect"
        };

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentDetails);

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        _consentServiceMock.Verify(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MigrateConsent_NotFound_ReturnsFailed()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        // Return a ProblemDescriptor (it will implicitly convert to Result<T>)
        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(Problems.ConsentNotFound);

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Migration failed", result.ErrorMessage);
    }

    [Fact]
    public async Task MigrateConsent_NetworkError_ReturnsFailed()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network error", result.ErrorMessage);
    }

    [Fact]
    public async Task MigrateConsent_UnexpectedException_LogsAndReturnsFailed()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var exception = new InvalidOperationException("Unexpected error");

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ThrowsAsync(exception);

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Exception", result.ErrorMessage);
        _loggerMock.Verify(
          x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            exception,
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
          Times.Once);
    }

    [Fact]
    public async Task MigrateConsent_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        using CancellationTokenSource cts = new CancellationTokenSource();
        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                cts.Cancel();
                cts.Token.ThrowIfCancellationRequested();
            });

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await service.MigrateConsent(consentId, cts.Token));
    }

    [Fact]
    public async Task MigrateConsent_TaskCancelledWithoutTokenCancellation_ReturnsFailed()
    {
        // Arrange - TaskCanceledException thrown but token NOT cancelled
        var consentId = Guid.NewGuid();
        using CancellationTokenSource cts = new CancellationTokenSource();

        // Mock throws TaskCanceledException but token is still active
        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Operation timed out"));

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, cts.Token);

        // Assert - Should be handled as an exception since token wasn't cancelled
        Assert.False(result.Success);
        Assert.Contains("Operation timed out", result.ErrorMessage);
    }

    [Fact]
    public async Task MigrateConsent_MultipleConsents_IndependentResults()
    {
        // Arrange
        var consentId1 = Guid.NewGuid();
        var consentId2 = Guid.NewGuid();
        var consentDetails1 = new ConsentRequestDetails
        {
            Id = consentId1,
            From = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()),
            To = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()),
            ValidTo = DateTimeOffset.UtcNow.AddDays(1),
            ConsentRights = new List<ConsentRight>(),
            ConsentRequestEvents = new List<ConsentRequestEvent>(),
            RedirectUrl = "https://example.com/redirect"
        };

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId1, It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentDetails1);

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId2, It.IsAny<CancellationToken>()))
          .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act
        var result1 = await service.MigrateConsent(consentId1, CancellationToken.None);
        var result2 = await service.MigrateConsent(consentId2, CancellationToken.None);

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        _consentServiceMock.Verify(x => x.GetAndStoreAltinn2Consent(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MigrateConsent_DatabaseError_ReturnsFailed()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Database connection failed", result.ErrorMessage);
    }

    [Fact]
    public async Task MigrateConsent_ReturnsNull_ReturnsFailed()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        // Altinn2 returns null (consent doesn't exist)
        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ReturnsAsync((ConsentRequestDetails)null);

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Migration failed", result.ErrorMessage);
    }

    [Fact]
    public async Task MigrateConsent_AlreadyMigrated_ReturnsSucceeded()
    {
        // Arrange - Consent already exists in A3
        var consentId = Guid.NewGuid();
        var consentDetails = new ConsentRequestDetails
        {
            Id = consentId,
            From = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()),
            To = ConsentPartyUrn.PartyUuid.Create(Guid.NewGuid()),
            ValidTo = DateTimeOffset.UtcNow.AddDays(1),
            ConsentRights = new List<ConsentRight>(),
            ConsentRequestEvents = new List<ConsentRequestEvent>(),
            RedirectUrl = "https://example.com/redirect"
        };

        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(consentDetails);

        var service = CreateService();

        // Act - Migrate twice
        var result1 = await service.MigrateConsent(consentId, CancellationToken.None);
        var result2 = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert - Both should succeed (idempotent)
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        _consentServiceMock.Verify(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MigrateConsent_InvalidConsentData_ReturnsFailed()
    {
        // Arrange
        var consentId = Guid.NewGuid();

        // Simulate invalid consent data causing a problem
        _consentServiceMock.Setup(x => x.GetAndStoreAltinn2Consent(consentId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(Problems.InvalidConsentResource);

        var service = CreateService();

        // Act
        var result = await service.MigrateConsent(consentId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Migration failed", result.ErrorMessage);
    }

    private ConsentMigrationService CreateService()
    {
        return new ConsentMigrationService(
          _consentServiceMock.Object,
          _loggerMock.Object);
    }
}

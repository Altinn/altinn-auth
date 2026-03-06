using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.Core.Services;

/// <summary>
/// Service for migrating consents from old application to new system
/// </summary>
public class ConsentMigrationService : IConsentMigrationService
{
    private readonly IConsent _consentService;
    private readonly ILogger<ConsentMigrationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentMigrationService"/> class
    /// </summary>
    public ConsentMigrationService(
        IConsent consentService,
        ILogger<ConsentMigrationService> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ConsentMigrationResult> MigrateConsent(Guid consentId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _consentService.GetAndStoreAltinn2Consent(consentId, cancellationToken);
            if (result.IsProblem || result.Value is null)
            {
                return ConsentMigrationResult.Failed("Migration failed");
            }

            return ConsentMigrationResult.Succeeded;
        }
        catch (HttpRequestException httpEx)
        {
            return ConsentMigrationResult.Failed($"Network error: {httpEx.Message}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while migrating consent {ConsentId}", consentId);
            return ConsentMigrationResult.Failed($"Exception: {ex.Message}");
        }
    }
}

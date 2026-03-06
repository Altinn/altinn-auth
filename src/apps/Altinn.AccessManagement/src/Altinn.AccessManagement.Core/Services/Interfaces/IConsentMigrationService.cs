using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;

namespace Altinn.AccessManagement.Core.Services.Interfaces;

/// <summary>
/// Service for migrating individual consents from old application to new system
/// </summary>
public interface IConsentMigrationService
{
    /// <summary>
    /// Migrates a consent request to the new system
    /// </summary>
    /// <param name="consentId">The Altinn2 consent guid</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success, failure, or duplicate</returns>
    Task<ConsentMigrationResult> MigrateConsent(Guid consentId, CancellationToken cancellationToken);
}

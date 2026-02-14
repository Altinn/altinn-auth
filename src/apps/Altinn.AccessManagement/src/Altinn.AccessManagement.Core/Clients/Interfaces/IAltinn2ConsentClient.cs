using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.SblBridge;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Core.Clients.Interfaces;

/// <summary>
/// Interface for a client wrapper for integration with SBL bridge delegation request API
/// </summary>
public interface IAltinn2ConsentClient
{
    /// <summary>
    /// Returns Consent
    /// </summary>
    /// <param name="consentGuid">The consent GUID to lookup</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>Consent information</returns>
    Task<Altinn2ConsentRequest> GetAltinn2Consent(Guid consentGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a list of unmigrated consents
    /// </summary>
    /// <param name="numberOfConsentsToReturn">The number of consents to return</param>
    /// <param name="status">The status for the consents to be returned</param>
    /// <param name="onlyGetExpired">Whether to only get expired consents</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>List of consent guids</returns>
    Task<List<Guid>> GetAltinn2ConsentListForMigration(int numberOfConsentsToReturn, int? status, bool onlyGetExpired, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a list of consents
    /// </summary>
    /// <param name="consentList">The list of consents to return</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>List of consent guids</returns>
    Task<List<Altinn2ConsentRequest>> GetMultipleAltinn2Consents(List<string> consentList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the consent migrated status
    /// </summary>
    /// <param name="consentGuid">The guid of the consent migrate status</param>
    /// <param name="status">The migrate status for the consent. 1 = Migrated, 2 = Failed</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>HttpResponse Message</returns>
    Task<bool> UpdateAltinn2ConsentMigrateStatus(string consentGuid, int status, CancellationToken cancellationToken = default);
}

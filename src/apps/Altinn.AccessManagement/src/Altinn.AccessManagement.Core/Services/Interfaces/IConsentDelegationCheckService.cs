using Altinn.AccessManagement.Core.Models;

namespace Altinn.AccessManagement.Core.Services.Interfaces;

/// <summary>
/// Service for performing delegation checks for consent scenarios.
/// Uses the new delegation check that supports access packages, roles, and direct resource rights.
/// </summary>
public interface IConsentDelegationCheckService
{
    /// <summary>
    /// Checks which rights the authenticated user can delegate on behalf of the specified party for a given resource.
    /// The resource's Delegable flag is ignored since consent only needs to verify user access, not re-delegation capability.
    /// </summary>
    /// <param name="authenticatedUserUuid">The UUID of the authenticated user performing the delegation.</param>
    /// <param name="partyUuid">The UUID of the party on whose behalf the delegation check is performed.</param>
    /// <param name="resourceIdentifier">The resource identifier (e.g. "skd_samtykketest").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ConsentDelegationCheckResult"/> indicating which actions the user can delegate.</returns>
    Task<ConsentDelegationCheckResult> CheckDelegatableRights(Guid authenticatedUserUuid, Guid partyUuid, string resourceIdentifier, CancellationToken cancellationToken = default);
}

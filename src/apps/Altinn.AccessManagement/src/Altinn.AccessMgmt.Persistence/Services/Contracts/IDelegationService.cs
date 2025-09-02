using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Delegation service
/// </summary>
public interface IDelegationService
{
    /// <summary>
    /// Create a new delegation betweeen two assignments
    /// </summary>
    /// <param name="userId">User</param>
    /// <param name="fromAssignmentId">From</param>
    /// <param name="toAssignmentId">To</param>
    /// <param name="options">ChangeRequestOptions</param>
    /// <returns></returns>
    Task<ExtDelegation> CreateDelgation(Guid userId, Guid fromAssignmentId, Guid toAssignmentId, ChangeRequestOptions options);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddPackageToDelegation(Guid userId, Guid delegationId, Guid packageId, ChangeRequestOptions options);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddResourceToDelegation(Guid userId, Guid delegationId, Guid resourceId, ChangeRequestOptions options);

    /// <summary>
    /// Create a delegation and required assignments for system agent flow
    /// </summary>
    Task<IEnumerable<Delegation>> CreateClientDelegation(CreateSystemDelegationRequestDto request, Guid facilitatorPartyId, ChangeRequestOptions options);

    /// <summary>
    /// Import client delegation and required assignments for A2 client delegations
    /// </summary>
    /// <param name="request">The delegation to import</param>
    /// <param name="options">ChangeRequestOptions</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<Delegation>> ImportClientDelegation(ImportClientDelegationRequestDto request, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes client delegation based on the specified request.
    /// </summary>
    /// <remarks>This method processes the revocation of client delegations as specified in the request.
    /// Ensure that the request and options are properly configured before calling this method.</remarks>
    /// <param name="request">The request containing details of the client delegation to be revoked. Cannot be null.</param>
    /// <param name="options">Options for processing the change request, such as validation and logging preferences. Cannot be null.</param>
    /// <returns>An integer indicating the number of delegations successfully revoked.</returns>
    Task<int> RevokeClientDelegation(ImportClientDelegationRequestDto request, ChangeRequestOptions options, CancellationToken cancellationToken = default);
}

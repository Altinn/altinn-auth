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
}

using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Delegation service
/// </summary>
public interface IDelegationService
{
    /// <summary>
    /// Gets a delegation by id
    /// </summary>
    /// <returns></returns>
    Task<Delegation> GetDelegation(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new delegation betweeen two assignments
    /// </summary>
    /// <param name="userId">User</param>
    /// <param name="fromAssignmentId">From</param>
    /// <param name="toAssignmentId">To</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<Delegation> CreateDelgation(Guid userId, Guid fromAssignmentId, Guid toAssignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddPackageToDelegation(Guid userId, Guid delegationId, Guid packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddResourceToDelegation(Guid userId, Guid delegationId, Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a delegation and required assignments for system agent flow
    /// </summary>
    Task<IEnumerable<CreateDelegationResponseDto>> CreateClientDelegation(CreateSystemDelegationRequestDto request, Guid facilitatorPartyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a delegation and required assignments for imported clint roles from A2
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<CreateDelegationResponseDto>> ImportClientDelegation(ImportClientDelegationRequestDto request, AuditValues audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a client delegation
    /// </summary>
    /// <returns></returns>
    Task<int> RevokeClientDelegation(ImportClientDelegationRequestDto request, AuditValues audit, bool onlyRemoveA2 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a delegation of the given id if found.
    /// </summary>
    Task<ProblemInstance> DeleteDelegation(Guid delegationId, CancellationToken cancellationToken = default);
}

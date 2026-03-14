using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Defines a contract for managing request assignments, including creation, retrieval, and updating of assignment packages and resources.
/// </summary>
public interface IRequestService
{
    /// <summary>
    /// Retrieves the request associated with the specified request identifier.
    /// </summary>
    Task<Result<RequestDto>> GetRequest(Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a collection of request DTOs matching the specified filtering criteria.
    /// </summary>
    Task<Result<IEnumerable<RequestDto>>> GetRequests(Guid? fromId, Guid? toId, IEnumerable<RequestStatus> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Creates a new request
    /// </summary>
    Task<Result<RequestDto>> CreateRequest(CreateRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a request.
    /// </summary>
    Task<Result<RequestDto>> UpdateRequest(Guid partyUuid, Guid requestId, RequestStatus status, CancellationToken ct = default);
}

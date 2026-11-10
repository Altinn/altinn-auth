using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Utils.Helper;
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
    Task<Result<AccessRequest>> GetRequest(Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Get requests sent by the party
    /// </summary>
    Task<Result<IEnumerable<AccessRequest>>> GetSentRequests(Guid partyId, Guid? toId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Get requests received by the party
    /// </summary>
    Task<Result<IEnumerable<AccessRequest>>> GetReceivedRequests(Guid partyId, Guid? fromId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Get count of requests sent by the party
    /// </summary>
    Task<Result<int>> GetSentRequestsCount(Guid partyId, Guid? toId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Get count of requests received by the party
    /// </summary>
    Task<Result<int>> GetReceivedRequestsCount(Guid partyId, Guid? fromId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Get count of requests received by each subunit of the party.
    /// Every direct subunit is included, also those with no requests.
    /// </summary>
    Task<Result<IEnumerable<ReceivedRequestCountSubunitDto>>> GetReceivedRequestsCountForSubunits(Guid partyId, Guid? fromId, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Creates a new request
    /// A Request from Kari by NAV to BakerAS for AppResource01.
    /// Will create an Assignment from BakerAS to Kari with an AssignmentResource for AppResource01.
    /// The right keys being requested must already be validated with <see cref="ValidateResourceRightKeys(string, IEnumerable{string}?, CancellationToken)"/>;
    /// pass null or an empty list when no specific rights are requested.
    /// </summary>
    Task<Result<AccessRequest>> CreateResourceRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, Guid resourceId, IEnumerable<string>? requestedRightKeys, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default);

    /// <summary>
    /// Validates the right keys of a resource request against the policy of the resource.
    /// Every key must exist in the policy and be delegable to a user, that is, be given by at least one
    /// role or access package in the policy. Returns null when the keys are valid or none are given.
    /// </summary>
    /// <param name="resource">The resource registry identifier of the resource (its RefId)</param>
    /// <param name="rightKeys">The requested right keys</param>
    /// <param name="ct">Cancellation token</param>
    Task<ValidationProblemInstance?> ValidateResourceRightKeys(string resource, IEnumerable<string>? rightKeys, CancellationToken ct = default);

    /// <summary>
    /// Validates right keys against the policy of the resource, like <see cref="ValidateResourceRightKeys(string, IEnumerable{string}?, CancellationToken)"/>,
    /// reporting each invalid key at <paramref name="jsonPointer"/> followed by its index, e.g. <c>/rightKeys/0</c>.
    /// </summary>
    /// <param name="resource">The resource registry identifier of the resource (its RefId)</param>
    /// <param name="rightKeys">The right keys to validate</param>
    /// <param name="jsonPointer">Where the keys are in the request, e.g. <c>/rightKeys</c></param>
    /// <param name="ct">Cancellation token</param>
    Task<ValidationProblemInstance?> ValidateResourceRightKeys(string resource, IEnumerable<string>? rightKeys, string jsonPointer, CancellationToken ct = default);

    /// <summary>
    /// Approves a pending resource request: sets its status and stores the rights that were given.
    /// The delegation must already have been made; this records the outcome on the request.
    /// Only the recipient of the request can approve it.
    /// </summary>
    /// <param name="partyUuid">The party approving, which must be the recipient of the request</param>
    /// <param name="requestId">The request to approve</param>
    /// <param name="status"><see cref="RequestStatusV2.Approved"/> or <see cref="RequestStatusV2.PartialApprove"/>, see <see cref="RequestApproval.DetermineStatus"/></param>
    /// <param name="approvedRightKeys">The right keys that were delegated</param>
    /// <param name="ct">Cancellation token</param>
    Task<Result<AccessRequest>> ApproveResourceRequest(Guid partyUuid, Guid requestId, RequestStatusV2 status, IEnumerable<string> approvedRightKeys, CancellationToken ct = default);

    /// <summary>
    /// Creates a new request
    /// A Request from Kari by NAV to BakerAS for Taxes.
    /// Will create an Assignment from BakerAS to Kari with an AssignmentPackage for Taxes.
    /// </summary>
    Task<Result<AccessRequest>> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, Guid packageId, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default);

    /// <summary>
    /// Creates a new request
    /// A Request from Kari by NAV to BakerAS for Taxes.
    /// Will create an Assignment from BakerAS to Kari with an AssignmentPackage for Taxes.
    /// </summary>
    Task<Result<AccessRequest>> CreatePackageRequest(Guid toId, Guid fromId, Guid byId, Guid roleId, string package, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default);

    /// <summary>
    /// Creates a new package request where the requester (from-party) is a system user.
    /// The <paramref name="systemUserId"/> must resolve to a system user entity, the
    /// <see cref="AccessMgmtFeatureFlags.EnableSystemUserRequests"/> feature must be enabled, the
    /// consumer organization (<paramref name="consumerOrgNo"/>) must be a public sector organization
    /// (KOMM, FYLK or STAT) and the recipient is the organization identified by <paramref name="organizationNo"/>.
    /// </summary>
    Task<Result<AccessRequest>> CreateSystemUserPackageRequest(string organizationNo, Guid systemUserId, string consumerOrgNo, Guid roleId, string package, RequestStatusV2 status = RequestStatusV2.Pending, CancellationToken ct = default);

    /// <summary>
    /// Gets the requests sent by a system user, optionally filtered by the recipient organization number.
    /// Applies the same sender rules as <see cref="CreateSystemUserPackageRequest"/>.
    /// </summary>
    Task<Result<IEnumerable<AccessRequest>>> GetSystemUserSentRequests(Guid systemUserId, string consumerOrgNo, string? toOrganizationNo, IEnumerable<RequestStatusV2> status, string? type, CancellationToken ct = default);

    /// <summary>
    /// Withdraws a request sent by a system user.
    /// Applies the same sender rules as <see cref="CreateSystemUserPackageRequest"/>.
    /// </summary>
    Task<Result<AccessRequest>> WithdrawSystemUserRequest(Guid systemUserId, string consumerOrgNo, Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a request.
    /// </summary>
    Task<Result<AccessRequest>> UpdateRequest(Guid partyUuid, Guid requestId, RequestStatusV2 status, CancellationToken ct = default);
}

using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Represents a request to assign a resource from one party to another using an assignment.
/// </summary>
public class BaseRequestAssignmentResource : BaseAudit, IEntityId
{
    /// <summary>
    /// Identifier for the request
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identify the assignment that is the source of the request
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Identify the resource that is being requested for assignment
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Identify the action that is being requested for the resource
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// The status of the request (e.g., pending, approved, rejected)
    /// </summary>
    public RequestStatus Status { get; set; }
}

using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Represents a request to assign a package from one party to another using an assignment.
/// </summary>
public class BaseRequestAssignmentPackage : BaseAudit, IEntityId
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
    /// Identify the package that is being requested for assignment
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// The status of the request (e.g., pending, approved, rejected)
    /// </summary>
    public RequestStatus Status { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Represents a request for assigning a resource to a party using an assignment
/// </summary>
public class RequestAssignmentResource : BaseRequestAssignmentResource, IHasLastUpdatedBy
{
    /// <summary>
    /// The assignment associated with this request
    /// </summary>
    public RequestAssignment Assignment { get; set; }

    /// <summary>
    /// The package associated with this request
    /// </summary>
    public Resource Resource { get; set; }

    /// <summary>
    /// The party that last changed the request, resolved from Audit_ChangedBy by the service.
    /// Deliberately not an EF relationship, so that no foreign key lands on an audit column.
    /// </summary>
    [NotMapped]
    public Entity? LastUpdatedBy { get; set; }
}

using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Represents a request for assigning a resource to a party using an assignment
/// </summary>
public class RequestAssignmentResource : BaseRequestAssignmentResource
{
    /// <summary>
    /// The assignment associated with this request
    /// </summary>
    public RequestAssignment Assignment { get; set; }

    /// <summary>
    /// The package associated with this request
    /// </summary>
    public Resource Resource { get; set; }
}

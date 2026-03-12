using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Represents a request to assign a package to a party using an assignment.
/// </summary>
public class RequestAssignmentPackage : BaseRequestAssignmentPackage
{
    /// <summary>
    /// The assignment associated with this request
    /// </summary>
    public RequestAssignment Assignment { get; set; }

    /// <summary>
    /// The package associated with this request
    /// </summary>
    public Package Package { get; set; }
}

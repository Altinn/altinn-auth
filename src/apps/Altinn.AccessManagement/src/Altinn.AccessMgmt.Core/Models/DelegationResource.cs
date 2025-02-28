namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Resources given to a delegation
/// </summary>
public class DelegationResource
{
    /// <summary>
    /// Identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Delegation identifier
    /// </summary>
    public Guid DelegationId { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; set; }
    
    //// public Guid DependencyId { get; set; }
}

/// <summary>
/// Extended delegation resource
/// </summary>
public class ExtDelegationResource : DelegationResource
{
    /// <summary>
    /// Delegation
    /// </summary>
    public Delegation Delegation { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}

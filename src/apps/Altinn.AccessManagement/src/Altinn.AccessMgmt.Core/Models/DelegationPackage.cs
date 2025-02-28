namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Packages given to the delegation
/// </summary>
public class DelegationPackage
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
    /// Package identifier
    /// </summary>
    public Guid PackageId { get; set; }

    //// public Guid DependencyId { get; set; }
}

/// <summary>
/// Extended delgation package
/// </summary>
public class ExtDelegationPackage : DelegationPackage
{
    /// <summary>
    /// Delegation
    /// </summary>
    public Delegation Delegation { get; set; }

    /// <summary>
    /// Package
    /// </summary>
    public Package Package { get; set; }
}

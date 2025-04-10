using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Packages given to the delegation
/// </summary>
public class DelegationPackage
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegationPackage"/> class.
    /// </summary>
    public DelegationPackage()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id
    {
        get => _id;
        set
        {
            if (!value.IsVersion7Uuid())
            {
                throw new ArgumentException("Id must be a version 7 UUID", nameof(value));
            }

            _id = value;
        }
    }

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

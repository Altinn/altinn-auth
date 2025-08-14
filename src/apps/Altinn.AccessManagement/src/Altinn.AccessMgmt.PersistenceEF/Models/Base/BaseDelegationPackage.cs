using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Packages given to the delegation
/// </summary>
[NotMapped]
public class BaseDelegationPackage : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDelegationPackage"/> class.
    /// </summary>
    public BaseDelegationPackage()
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

    /// <summary>
    /// Role dependency
    /// </summary>
    public Guid? RolePackageId { get; set; }

    /// <summary>
    /// Assignment dependency
    /// </summary>
    public Guid? AssignmentPackageId { get; set; }
}

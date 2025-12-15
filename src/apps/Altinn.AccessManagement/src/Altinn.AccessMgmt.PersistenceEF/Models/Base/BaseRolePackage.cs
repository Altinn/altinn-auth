using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// RolePackage
/// </summary>
[NotMapped]
public class BaseRolePackage : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRolePackage"/> class.
    /// </summary>
    public BaseRolePackage()
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
    /// RoleId
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// PackageId
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// EntityVariantId (optional)
    /// </summary>
    public Guid? EntityVariantId { get; set; }

    /// <summary>
    /// HasAccess
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// CanAssign
    /// </summary>
    public bool CanAssign { get; set; }

    /// <summary>
    /// CanDelegate
    /// </summary>
    public bool CanDelegate { get; set; }
}

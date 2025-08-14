using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Resources mapped directly to roles
/// </summary>
[NotMapped]
public class BaseRoleResource : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRoleResource"/> class.
    /// </summary>
    public BaseRoleResource()
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
    /// Role identity
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Resource identity
    /// </summary>
    public Guid ResourceId { get; set; }
}

using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// RoleMap
/// Entities with a one roile can also get another one
/// </summary>
[NotMapped]
public class BaseRoleMap : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRoleMap"/> class.
    /// </summary>
    public BaseRoleMap()
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
    /// HasRoleId
    /// </summary>
    public Guid HasRoleId { get; set; }

    /// <summary>
    /// GetRoleId
    /// </summary>
    public Guid GetRoleId { get; set; }
}

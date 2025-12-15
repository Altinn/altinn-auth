using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Delegation between two assignments
/// </summary>
[NotMapped]
public class BaseDelegation : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDelegation"/> class.
    /// </summary>
    public BaseDelegation()
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
    /// Assignment to delegate from
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// Assignment to delegate to
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// Entity owner of the Delegation
    /// </summary>
    public Guid FacilitatorId { get; set; }
}

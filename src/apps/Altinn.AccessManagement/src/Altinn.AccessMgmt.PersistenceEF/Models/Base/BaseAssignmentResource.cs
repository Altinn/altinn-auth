using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Resources added to an assignment
/// </summary>
[NotMapped]
public class BaseAssignmentResource : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAssignmentResource"/> class.
    /// </summary>
    public BaseAssignmentResource()
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
    /// Assignment identity
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Path for policy file
    /// </summary>
    public string PolicyPath { get; set; }

    /// <summary>
    /// Policy version
    /// </summary>
    public string PolicyVersion { get; set; }

    /// <summary>
    /// Legacy DelegationChangeId
    /// </summary>
    public long DelegationChangeId { get; set; }
}

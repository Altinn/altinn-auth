using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Audit;

/// <inheritdoc />
public class AuditDelegation : BaseDelegation, IAudit 
{
    /// <inheritdoc />
    public DateTimeOffset Audit_ValidFrom { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? Audit_ValidTo { get; set; }

    /// <inheritdoc />
    public Guid? Audit_ChangedBy { get; set; }

    /// <inheritdoc />
    public Guid? Audit_ChangedBySystem { get; set; }

    /// <inheritdoc />
    public string Audit_ChangeOperation { get; set; }

    /// <inheritdoc />
    public Guid? Audit_DeletedBy { get; set; }

    /// <inheritdoc />
    public Guid? Audit_DeletedBySystem { get; set; }

    /// <inheritdoc />
    public string Audit_DeleteOperation { get; set; }
}

using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Audit;

/// <inheritdoc />
public class AuditRequestAssignmentResource : BaseRequestAssignmentResource, IAudit
{
    /// <inheritdoc />
    public DateTimeOffset? Audit_ValidTo { get; set; }

    /// <inheritdoc />
    public Guid? Audit_DeletedBy { get; set; }

    /// <inheritdoc />
    public Guid? Audit_DeletedBySystem { get; set; }

    /// <inheritdoc />
    public string Audit_DeleteOperation { get; set; }
}

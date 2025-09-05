using Altinn.AccessMgmt.PersistenceEF.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;

public class BaseAudit
{
    public Guid? Audit_ChangedBy { get; set; } = Utils.AuditConfiguration.ChangedBy;

    public Guid? Audit_ChangedBySystem { get; set; } = Utils.AuditConfiguration.ChangedBySystem;

    public string Audit_ChangeOperation { get; set; } = Guid.NewGuid().ToString();

    public DateTimeOffset Audit_ValidFrom { get; set; } = DateTimeOffset.UtcNow;

    public void SetAuditValues(AuditValues values)
    {
        Audit_ChangedBy = values.ChangedBy;
        Audit_ChangedBySystem = values.ChangedBySystem;
        Audit_ChangeOperation = values.OperationId;
        Audit_ValidFrom = DateTimeOffset.UtcNow;
    }

    public void SetAuditValues(Guid changedBy, Guid changedBySystem)
    {
        Audit_ChangedBy = changedBy;
        Audit_ChangedBySystem = changedBySystem;
        Audit_ChangeOperation = Guid.NewGuid().ToString();
        Audit_ValidFrom = DateTimeOffset.Now;
    }

    public void SetAuditValues(Guid changedBy, Guid changedBySystem, string changeOperation)
    {
        Audit_ChangedBy = changedBy;
        Audit_ChangedBySystem = changedBySystem;
        Audit_ChangeOperation = changeOperation;
        Audit_ValidFrom = DateTimeOffset.Now;
    }

    public void SetAuditValues(Guid changedBy, Guid changedBySystem, string changeOperation, DateTimeOffset validFrom)
    {
        Audit_ChangedBy = changedBy;
        Audit_ChangedBySystem = changedBySystem;
        Audit_ChangeOperation = changeOperation;
        Audit_ValidFrom = validFrom;
    }
}

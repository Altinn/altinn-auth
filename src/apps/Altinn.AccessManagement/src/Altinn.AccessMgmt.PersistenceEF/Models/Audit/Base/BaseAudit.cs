namespace Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;

public class BaseAudit
{
    public Guid? Audit_ChangedBy { get; set; }

    public Guid? Audit_ChangedBySystem { get; set; }

    public string Audit_ChangeOperation { get; set; }

    public DateTime Audit_ValidFrom { get; set; }
}

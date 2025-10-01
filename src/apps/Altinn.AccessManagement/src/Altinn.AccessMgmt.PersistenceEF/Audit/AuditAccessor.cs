using Altinn.AccessMgmt.PersistenceEF.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Audit;

public class AuditAccessor : IAuditAccessor
{
    public AuditValues AuditValues { get; set; }
}

public interface IAuditAccessor
{
    AuditValues? AuditValues { get; set; }
}

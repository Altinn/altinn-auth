namespace Altinn.AccessMgmt.PersistenceEF.Utils;

public static class BaseConfiguration
{
    public static string BaseSchema { get; set; } = "dbo";

    public static string AuditSchema { get; set; } = "dbo_history";
}

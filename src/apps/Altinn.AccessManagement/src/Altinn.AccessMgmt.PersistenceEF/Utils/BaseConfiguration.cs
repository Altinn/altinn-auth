namespace Altinn.AccessMgmt.PersistenceEF.Utils;

public static class BaseConfiguration
{
    public static string BaseSchema { get; set; } = "dbo";

    public static string AuditSchema { get; set; } = "dbo_history";
}

public static class AuditConfiguration
{
    public static Guid ChangedBy { get; set; } = Guid.Parse("0195efb8-7c80-7e58-8ef9-2363c63b8909");

    public static Guid ChangedBySystem { get; set; } = Guid.Parse("0195efb8-7c80-7fc6-a42c-f8c8eef8750b");
}

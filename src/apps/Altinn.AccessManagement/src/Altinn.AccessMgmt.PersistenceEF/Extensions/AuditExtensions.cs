using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class AuditExtensions
{
    public const string AnnotationName = "Altinn:AuditVersion";
    
    public static EntityTypeBuilder EnableAudit(this EntityTypeBuilder builder)
    {
        /*
        builder.Property("ChangedBy").HasColumnName("audit_changedby");
        builder.Property("ChangedBySystem").HasColumnName("audit_changedbysystem");
        builder.Property("ChangeOperation").HasColumnName("audit_changeoperation");
        builder.Property("ValidFrom").HasColumnName("audit_validfrom");
        */

        // Use this to regenerate all audit functions and triggers in the database.
        builder.HasAnnotation(AnnotationName, AuditEFConfiguration.Version);

        return builder;
    }

    public static int? GetAuditVersion(this IEntityType entityType)
    {
        return entityType.FindAnnotation(AnnotationName)?.Value as int?;
    }
}

public static class AuditEFConfiguration
{
    /// <summary>
    /// Increment this when audit SQL changes
    /// </summary>
    public const int Version = 3;
}

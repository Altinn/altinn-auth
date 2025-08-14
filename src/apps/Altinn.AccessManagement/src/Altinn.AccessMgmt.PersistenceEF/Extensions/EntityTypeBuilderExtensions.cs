using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder EnableAudit(this EntityTypeBuilder builder)
    {
        builder.Property("ChangedBy").HasColumnName("audit_changedby");
        builder.Property("ChangedBySystem").HasColumnName("audit_changedbysystem");
        builder.Property("ChangeOperation").HasColumnName("audit_changeoperation");
        builder.Property("ValidFrom").HasColumnName("audit_validfrom");

        return builder.HasAnnotation("EnableAudit", true);
    }

    public static EntityTypeBuilder EnableTranslation(this EntityTypeBuilder builder)
    {
        return builder.HasAnnotation("EnableTranslation", true);
    }
}

public static class ViewEntityBuilderExtensions
{
    public static EntityTypeBuilder<T> ConfigureAsView<T>(
        this EntityTypeBuilder<T> builder,
        string viewName,
        string schema = "dbo") 
        where T : class
    {
        builder.ToTable(viewName, schema, t => t.ExcludeFromMigrations());
        builder.HasNoKey();
        return builder;
    }
}

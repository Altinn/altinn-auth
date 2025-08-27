using System.Linq.Expressions;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class BuilderExtensions
{
    public static void UseLowerCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Lowercase tables
            entity.SetTableName(entity.GetTableName()?.ToLowerInvariant());
            entity.SetSchema(entity.GetSchema()?.ToLowerInvariant());

            // Lowercase columns
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName(), entity.GetSchema()))?.ToLowerInvariant());
            }

            // Lowercase primary keys
            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToLowerInvariant());
            }

            // Lowercase foreign keys
            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(fk.GetConstraintName()?.ToLowerInvariant());
            }

            // Lowercase index
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToLowerInvariant());
            }
        }
    }

    public static EntityTypeBuilder EnableTranslation(this EntityTypeBuilder builder)
    {
        return builder.HasAnnotation("EnableTranslation", true);
    }

    /// <summary>
    /// Configure table to use default schema <see cref="BaseConfiguration.BaseSchema"/> and name of <typeparamref name="TEntity"/>
    /// </summary>
    public static EntityTypeBuilder<TEntity> ToDefaultTable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.ToTable(typeof(TEntity).Name.ToLower(), BaseConfiguration.BaseSchema);

        return builder;
    }

    /// <summary>
    /// Configure table to use default schema <see cref="BaseConfiguration.BaseSchema"/> and name of <typeparamref name="TEntity"/>
    /// </summary>
    public static EntityTypeBuilder<TEntity> ToDefaultAuditTable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.ToTable(typeof(TEntity).Name.ToLower(), BaseConfiguration.AuditSchema);

        return builder;
    }

    /// <summary>
    /// Configure required FK (+ index) with chosen delete behavior.
    /// </summary>
    public static EntityTypeBuilder<TEntity> PropertyWithReference<TEntity, TReference>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object>> foreignKey,
        Expression<Func<TReference, object>> principalKey,
        DeleteBehavior deleteBehavior = DeleteBehavior.Cascade,
        bool hasIndex = true,
        bool required = true)
        where TEntity : class
        where TReference : class
    {
        var rel = builder.HasOne<TReference>()
                            .WithMany()
                            .HasForeignKey(foreignKey)
                            .HasPrincipalKey(principalKey)
                            .OnDelete(deleteBehavior);

        if (required)
        {
            rel.IsRequired();
        }

        if (hasIndex)
        {
            builder.HasIndex(foreignKey);
        }

        return builder;
    }

    /// <summary>
    /// Configure required FK (+ index) with chosen delete behavior.
    /// </summary>
    public static EntityTypeBuilder<TEntity> PropertyWithReference<TEntity, TReference>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TReference>> navKey,
        Expression<Func<TEntity, object>> foreignKey,
        Expression<Func<TReference, object>> principalKey,
        DeleteBehavior deleteBehavior = DeleteBehavior.Cascade,
        bool hasIndex = true,
        bool required = true)
        where TEntity : class
        where TReference : class
    {
        var rel = builder.HasOne<TReference>(navKey)
                            .WithMany()
                            .HasForeignKey(foreignKey)
                            .HasPrincipalKey(principalKey)
                            .OnDelete(deleteBehavior);

        if (required)
        {
            rel.IsRequired();
        }

        if (hasIndex)
        {
            builder.HasIndex(foreignKey);
        }

        return builder;
    }

    /// <summary>
    /// Configure required FK (+ index) with chosen delete behavior.
    /// </summary>
    public static EntityTypeBuilder<TEntity> PropertyWithReference<TEntity, TReference>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object>> foreignKey,
        DeleteBehavior deleteBehavior = DeleteBehavior.Cascade,
        bool hasIndex = true,
        bool required = true)
        where TEntity : class
        where TReference : class
    {
        var rel = builder.HasOne<TReference>()
                            .WithMany()
                            .HasForeignKey(foreignKey)
                            .OnDelete(deleteBehavior);

        if (required)
        {
            rel.IsRequired();
        }

        if (hasIndex)
        {
            builder.HasIndex(foreignKey);
        }

        return builder;
    }

    public static EntityTypeBuilder EnableAudit(this EntityTypeBuilder builder)
    {
        builder.Property("ChangedBy").HasColumnName("audit_changedby");
        builder.Property("ChangedBySystem").HasColumnName("audit_changedbysystem");
        builder.Property("ChangeOperation").HasColumnName("audit_changeoperation");
        builder.Property("ValidFrom").HasColumnName("audit_validfrom");

        return builder.HasAnnotation("EnableAudit", true);
    }

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

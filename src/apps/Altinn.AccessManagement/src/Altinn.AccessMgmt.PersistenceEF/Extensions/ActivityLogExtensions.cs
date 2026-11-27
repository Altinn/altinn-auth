using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class ActivityLogExtensions
{
    public const string AnnotationName = "Altinn:ActivityLogVersion";

    public const string PartitionAnnotationName = "Altinn:PartitionByRangeColumn";

    /// <summary>
    /// Marks the entity for activity logging. <see cref="CustomMigrationsSqlGenerator"/> emits the
    /// table's trigger scripts from <see cref="ActivityLogTriggerScripts"/> whenever a migration
    /// operation touches the table (or an AlterDatabase/AlterTable operation carries the annotation).
    /// </summary>
    /// <remarks>
    /// The EF model differ does not scaffold operations for custom annotation changes, so to
    /// (re)generate the trigger SQL, bump <see cref="ActivityLogEFConfiguration.Version"/> and add a
    /// migration containing <c>migrationBuilder.AlterTable(name, schema).Annotation(AnnotationName, Version)</c>
    /// for each activity-logged table whose trigger SQL changed (mirroring how the audit triggers are regenerated).
    /// Functions are replaced, but a trigger is only created if it does not exist, so a migration that
    /// changes a trigger's <c>WHEN</c> condition must drop that trigger first
    /// (see <c>ActivityLogRequestRightKeysChanged</c>). The scripts are not versioned, so the original activity
    /// log migration also emits the current SQL; trigger conditions and functions that use a column added
    /// later must read it through <c>to_jsonb(OLD)</c>/<c>to_jsonb(NEW)</c>, so they work before the column
    /// exists and after a rollback past it.
    /// </remarks>
    public static EntityTypeBuilder EnableActivityLog(this EntityTypeBuilder builder)
    {
        builder.HasAnnotation(AnnotationName, ActivityLogEFConfiguration.Version);

        return builder;
    }

    /// <summary>
    /// Declares that the table must be created with <c>PARTITION BY RANGE (columnName)</c>.
    /// Handled by <see cref="CustomMigrationsSqlGenerator"/>, since EF Core cannot emit
    /// partitioned-table DDL itself. Partitions are created by SQL in the migration.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasPartitionByRange<TEntity>(this EntityTypeBuilder<TEntity> builder, string columnName)
        where TEntity : class
    {
        builder.HasAnnotation(PartitionAnnotationName, columnName.ToLowerInvariant());

        return builder;
    }

    public static int? GetActivityLogVersion(this IEntityType entityType)
    {
        return entityType.FindAnnotation(AnnotationName)?.Value as int?;
    }
}

public static class ActivityLogEFConfiguration
{
    /// <summary>
    /// Increment this when activity log trigger SQL changes
    /// </summary>
    public const int Version = 2;
}

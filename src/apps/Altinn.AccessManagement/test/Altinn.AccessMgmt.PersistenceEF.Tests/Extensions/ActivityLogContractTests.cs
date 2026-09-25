using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Extensions;

/// <summary>
/// Pins the contract between the EF model and the activity log trigger machinery:
/// every entity marked with <see cref="ActivityLogExtensions.EnableActivityLog"/> must have
/// hand-written trigger SQL registered in <see cref="ActivityLogTriggerScripts"/> (and vice
/// versa), and the activitylog table itself must stay range-partitioned on "when" with the
/// partition column leading the primary key. A mismatch surfaces here instead of as a failed
/// migration or silently missing triggers.
/// </summary>
[UnitTest]
public class ActivityLogContractTests
{
    private static readonly Lazy<IModel> Model = new(() =>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=activitylog-contract-test")
            .Options;

        using var context = new AppDbContext(options);
        return context.Model;
    });

    private static readonly string[] TablesWithUpdateTriggers =
    [
        "assignmentinstance",
        "requestassignmentpackage",
        "requestassignmentresource",
    ];

    [Fact]
    public void AnnotatedEntities_MatchTriggerScriptRegistryExactly()
    {
        var annotatedTables = Model.Value.GetEntityTypes()
            .Where(t => t.FindAnnotation(ActivityLogExtensions.AnnotationName) is not null)
            .Select(t => t.GetTableName())
            .Order()
            .ToList();

        Assert.Equal(ActivityLogTriggerScripts.Tables.Order(), annotatedTables);
    }

    [Fact]
    public void ActivityLogTable_IsPartitionedOnWhen_WithPartitionColumnLeadingThePrimaryKey()
    {
        var entityType = Model.Value.FindEntityType(typeof(ActivityLog));

        Assert.NotNull(entityType);
        Assert.Equal("when", entityType!.FindAnnotation(ActivityLogExtensions.PartitionAnnotationName)?.Value);

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal([nameof(ActivityLog.When), nameof(ActivityLog.Id)], primaryKey!.Properties.Select(p => p.Name));
    }

    [Fact]
    public void ActivityLogTable_HasNoAuditOrActivityLogTriggersOfItsOwn()
    {
        var entityType = Model.Value.FindEntityType(typeof(ActivityLog));

        Assert.NotNull(entityType);
        Assert.Null(entityType!.FindAnnotation(AuditExtensions.AnnotationName));
        Assert.Null(entityType.FindAnnotation(ActivityLogExtensions.AnnotationName));
    }

    [Fact]
    public void RegistryScripts_ContainExpectedTriggersAndTheMissingTableGuard()
    {
        foreach (var table in ActivityLogTriggerScripts.Tables)
        {
            var sql = string.Join("\n", ActivityLogTriggerScripts.ForTable("dbo", table));

            Assert.Contains($"activitylog_{table}_insert_fn", sql);
            Assert.Contains($"activitylog_{table}_insert_trg", sql);
            Assert.Contains($"activitylog_{table}_delete_fn", sql);

            // Delete triggers must stay deferred so DB-level cascades resolve parent
            // snapshots from history written earlier in the same transaction.
            Assert.Contains($"CREATE CONSTRAINT TRIGGER activitylog_{table}_delete_trg", sql);
            Assert.Contains("DEFERRABLE INITIALLY DEFERRED", sql);
            Assert.Equal(TablesWithUpdateTriggers.Contains(table), sql.Contains($"activitylog_{table}_update_trg"));

            Assert.Contains("INSERT INTO dbo.activitylog", sql);
            Assert.Contains("IF to_regclass('dbo.activitylog') IS NULL THEN RETURN NULL; END IF;", sql);
        }
    }

    [Fact]
    public void ForTable_UnknownTableOrSchema_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ActivityLogTriggerScripts.ForTable("dbo", "entity"));
        Assert.Throws<InvalidOperationException>(() => ActivityLogTriggerScripts.ForTable("dbo_history", "assignment"));
    }
}

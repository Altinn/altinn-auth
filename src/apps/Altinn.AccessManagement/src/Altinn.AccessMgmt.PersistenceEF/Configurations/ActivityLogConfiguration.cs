using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToDefaultTable();
        builder.HasPartitionByRange(nameof(ActivityLog.When));

        // The partition key must be part of the primary key on a partitioned table.
        builder.HasKey(p => new { p.When, p.Id });

        builder.Property(p => p.Id)
            .HasDefaultValueSql("dbo.uuid_generate_v7()")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Details).HasColumnType("jsonb");

        // Every arm of the involved-party OR (from/to/via, plus by for the any-party anchor)
        // must be indexed for the planner to BitmapOr them; one unindexed arm forces a scan
        // of every partition.
        builder.HasIndex(p => new { p.FromId, p.When });
        builder.HasIndex(p => new { p.ToId, p.When });
        builder.HasIndex(p => new { p.ViaId, p.When });
        builder.HasIndex(p => new { p.ById, p.When });
        builder.HasIndex(p => p.ItemId);
        builder.HasIndex(p => p.ParentId);
    }
}

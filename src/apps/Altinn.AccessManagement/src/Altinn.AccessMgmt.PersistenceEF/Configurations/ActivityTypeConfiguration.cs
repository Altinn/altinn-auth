using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ActivityTypeConfiguration : IEntityTypeConfiguration<ActivityType>
{
    public void Configure(EntityTypeBuilder<ActivityType> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired();
        builder.Property(p => p.Description).IsRequired();

        builder.HasIndex(p => new { p.Type, p.Subtype, p.Trigger, p.Status })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}

public class AuditActivityTypeConfiguration : AuditConfiguration<AuditActivityType> { }

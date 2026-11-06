using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Altinn.AccessMgmt.PersistenceEF.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.RefId).IsRequired();

        // The sentinel is true so that an explicit false is written instead of being replaced by the database default.
        builder.Property(t => t.Delegable).IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.Property(t => t.AccessListMode).IsRequired().HasConversion<string>().HasDefaultValue(ResourceAccessListMode.Disabled);

        builder.PropertyWithReference(navKey: t => t.Type, foreignKey: t => t.TypeId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);
        builder.PropertyWithReference(navKey: t => t.Provider, foreignKey: t => t.ProviderId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.RefId }).IsUnique();
    }
}

public class AuditResourceConfiguration : AuditConfiguration<AuditResource>
{
    public override void Configure(EntityTypeBuilder<AuditResource> builder)
    {
        base.Configure(builder);

        // The audit trigger copies rows from the resource table, so the columns must match it.
        builder.Property(t => t.Delegable).IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.Property(t => t.AccessListMode).IsRequired().HasConversion<string>().HasDefaultValue(ResourceAccessListMode.Disabled);
    }
}

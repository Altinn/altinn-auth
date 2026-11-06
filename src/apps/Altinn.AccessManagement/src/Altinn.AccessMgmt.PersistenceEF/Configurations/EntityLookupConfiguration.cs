using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

#pragma warning disable CS0612 // EntityLookup is obsolete, but its table is still part of the model
public class EntityLookupConfiguration : IEntityTypeConfiguration<EntityLookup>
{
    public void Configure(EntityTypeBuilder<EntityLookup> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Entity, foreignKey: t => t.EntityId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.Property(t => t.Key).IsRequired();
        builder.Property(t => t.Value).IsRequired();
        builder.Property(t => t.IsProtected).IsRequired().HasDefaultValue<bool>(false);

        builder.HasIndex(t => new { t.EntityId, t.Key }).IncludeProperties(p => new { p.Value, p.Id }).IsUnique();
    }
}

public class AuditEntityLookupConfiguration : AuditConfiguration<AuditEntityLookup> { }
#pragma warning restore CS0612

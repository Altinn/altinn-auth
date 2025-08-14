using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class EntityVariantConfiguration : IEntityTypeConfiguration<EntityVariant> 
{
    public void Configure(EntityTypeBuilder<EntityVariant> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Description).IsRequired();
        builder.PropertyWithReference(navKey: t => t.Type, foreignKey: t => t.TypeId, principalKey: t => t.Id);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class AuditEntityVariantConfiguration : AuditConfiguration<AuditEntityVariant> { }

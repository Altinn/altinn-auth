using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class EntityConfiguration : IEntityTypeConfiguration<Entity> 
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.RefId);

        builder.PropertyWithReference(navKey: t => t.Type, foreignKey: t => t.TypeId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);
        builder.PropertyWithReference(navKey: t => t.Variant, foreignKey: t => t.VariantId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);
        builder.PropertyWithReference(navKey: t => t.Parent, foreignKey: t => t.ParentId, principalKey: t => t.Id, required: false, deleteBehavior: DeleteBehavior.Restrict);

        builder.HasIndex(["Name", "RefId", "TypeId", "VariantId"]).IsUnique();
    }
}

public class AuditEntityConfiguration : AuditConfiguration<AuditEntity> { }

using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class EntityVariantRoleConfiguration : IEntityTypeConfiguration<EntityVariantRole> 
{
    public void Configure(EntityTypeBuilder<EntityVariantRole> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Variant, foreignKey: t => t.VariantId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Role, foreignKey: t => t.RoleId, principalKey: t => t.Id);

        builder.HasIndex(t => new { t.VariantId, t.RoleId }).IsUnique();
    }
}

public class AuditEntityVariantRoleConfiguration : AuditConfiguration<AuditEntityVariantRole> { }

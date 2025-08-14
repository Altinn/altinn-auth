using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RoleMapConfiguration : IEntityTypeConfiguration<RoleMap> 
{
    public void Configure(EntityTypeBuilder<RoleMap> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.HasRoleId).IsRequired();
        builder.Property(t => t.GetRoleId).IsRequired();

        builder.PropertyWithReference(navKey: t => t.HasRole, foreignKey: t => t.HasRoleId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.GetRole, foreignKey: t => t.GetRoleId, principalKey: t => t.Id);

        builder.HasIndex(t => new { t.HasRoleId, t.GetRoleId }).IsUnique();
    }
}

public class AuditRoleMapConfiguration : AuditConfiguration<AuditRoleMap> { }

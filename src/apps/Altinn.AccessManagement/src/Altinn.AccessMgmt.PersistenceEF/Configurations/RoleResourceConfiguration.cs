using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RoleResourceConfiguration : IEntityTypeConfiguration<RoleResource> 
{
    public void Configure(EntityTypeBuilder<RoleResource> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Role, foreignKey: t => t.RoleId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.RoleId, t.ResourceId }).IsUnique();
    }
}

public class AuditRoleResourceConfiguration : AuditConfiguration<AuditRoleResource> { }

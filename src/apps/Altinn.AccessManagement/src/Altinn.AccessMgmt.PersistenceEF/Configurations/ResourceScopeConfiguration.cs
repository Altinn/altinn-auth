using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ResourceScopeConfiguration : IEntityTypeConfiguration<ResourceScope>
{
    public void Configure(EntityTypeBuilder<ResourceScope> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Scope).IsRequired();

        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id, withMany: t => t.Scopes, deleteBehavior: DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.ResourceId, t.Scope }).IsUnique();
    }
}

public class AuditResourceScopeConfiguration : AuditConfiguration<AuditResourceScope> { }

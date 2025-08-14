using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class DelegationResourceConfiguration : IEntityTypeConfiguration<DelegationResource> 
{
    public void Configure(EntityTypeBuilder<DelegationResource> builder)
    {
        builder.ToTable(nameof(DelegationResource).ToLower(), BaseConfiguration.BaseSchema);

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Delegation, foreignKey: t => t.DelegationId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id);

        builder.HasIndex(t => new { t.DelegationId, t.ResourceId }).IsUnique();
    }
}

public class AuditDelegationResourceConfiguration : AuditConfiguration<AuditDelegationResource> { }

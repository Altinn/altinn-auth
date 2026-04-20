using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.From, foreignKey: t => t.FromId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.To, foreignKey: t => t.ToId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.Facilitator, foreignKey: t => t.FacilitatorId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);

        builder.CollectionPropertyWithReference(
            collectionNav: t => t.DelegationPackages,
            referenceNav: t => t.Delegation,
            foreignKey: t => t.DelegationId,
            deleteBehavior: DeleteBehavior.Cascade
            );

        builder.HasIndex(t => new { t.FromId, t.ToId, t.FacilitatorId }).IsUnique();
        builder.HasIndex(t => new { t.ToId }).IncludeProperties(["Id", "FromId"]);
    }
}

public class AuditDelegationConfiguration : AuditConfiguration<AuditDelegation> { }

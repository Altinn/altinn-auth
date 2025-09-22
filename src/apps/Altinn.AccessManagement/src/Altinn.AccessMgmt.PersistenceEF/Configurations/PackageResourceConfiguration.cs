using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class PackageResourceConfiguration : IEntityTypeConfiguration<PackageResource> 
{
    public void Configure(EntityTypeBuilder<PackageResource> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Package, foreignKey: t => t.PackageId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.PackageId, t.ResourceId }).IsUnique();
    }
}

public class AuditPackageResourceConfiguration : AuditConfiguration<AuditPackageResource> { }

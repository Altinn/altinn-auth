using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name);
        builder.Property(t => t.Description);
        builder.Property(t => t.Urn);
        builder.Property(t => t.IsAssignable).HasDefaultValue(false);
        builder.Property(t => t.IsDelegable).HasDefaultValue(false);
        builder.Property(t => t.HasResources);

        builder.PropertyWithReference(navKey: t => t.Provider, foreignKey: t => t.ProviderId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);
        builder.PropertyWithReference(navKey: t => t.EntityType, foreignKey: t => t.EntityTypeId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);
        builder.PropertyWithReference(navKey: t => t.Area, foreignKey: t => t.AreaId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ProviderId, t.Name }).IsUnique();
    }
}

public class AuditPackageConfiguration : AuditConfiguration<AuditPackage> { }

using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RolePackageConfiguration : IEntityTypeConfiguration<RolePackage>
{
    public void Configure(EntityTypeBuilder<RolePackage> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.HasAccess).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.CanDelegate).IsRequired().HasDefaultValue(false);

        builder.PropertyWithReference(navKey: t => t.Role, foreignKey: t => t.RoleId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Package, foreignKey: t => t.PackageId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.EntityVariant, foreignKey: t => t.EntityVariantId, principalKey: t => t.Id, required: false);

        builder.HasIndex(t => new { t.RoleId, t.PackageId, t.EntityVariantId }).IsUnique();
    }
}

public class AuditRolePackageConfiguration : AuditConfiguration<AuditRolePackage> { }

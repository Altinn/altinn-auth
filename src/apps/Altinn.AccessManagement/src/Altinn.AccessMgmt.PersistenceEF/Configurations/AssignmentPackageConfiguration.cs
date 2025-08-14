using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class AssignmentPackageConfiguration : IEntityTypeConfiguration<AssignmentPackage> 
{
    public void Configure(EntityTypeBuilder<AssignmentPackage> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Assignment, foreignKey: t => t.AssignmentId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Package, foreignKey: t => t.PackageId, principalKey: t => t.Id);

        builder.HasIndex(t => new { t.AssignmentId, t.PackageId }).IsUnique();
    }
}

public class AuditAssignmentPackageConfiguration : AuditConfiguration<AuditAssignmentPackage> { }

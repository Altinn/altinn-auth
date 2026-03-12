using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RequestAssignmentPackageConfiguration : IEntityTypeConfiguration<RequestAssignmentPackage>
{
    public void Configure(EntityTypeBuilder<RequestAssignmentPackage> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);
        builder.Property(t => t.Status).IsRequired();

        builder.PropertyWithReference(navKey: t => t.Assignment, foreignKey: t => t.AssignmentId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.Package, foreignKey: t => t.PackageId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);

        builder.HasIndex(["AssignmentId", "PackageId", "Status"]).IsUnique();
    }
}

public class AuditRequestAssignmentPackageConfiguration : AuditConfiguration<AuditRequestAssignmentPackage> { }

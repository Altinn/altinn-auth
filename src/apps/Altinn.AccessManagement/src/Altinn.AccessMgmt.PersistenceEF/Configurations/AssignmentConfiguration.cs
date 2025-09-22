using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.From, foreignKey: t => t.FromId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.To, foreignKey: t => t.ToId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.Role, foreignKey: t => t.RoleId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.FromId, t.ToId, t.RoleId }).IsUnique().IncludeProperties(t => t.Id);
    }
}

public class AuditAssignmentConfiguration : AuditConfiguration<AuditAssignment> { }

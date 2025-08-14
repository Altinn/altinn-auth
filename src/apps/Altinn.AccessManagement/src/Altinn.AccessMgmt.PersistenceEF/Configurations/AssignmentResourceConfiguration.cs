using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class AssignmentResourceConfiguration : IEntityTypeConfiguration<AssignmentResource> 
{
    public void Configure(EntityTypeBuilder<AssignmentResource> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Assignment, foreignKey: t => t.AssignmentId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id);

        builder.HasIndex(t => new { t.AssignmentId, t.ResourceId }).IsUnique();
    }
}

public class AuditAssignmentResourceConfiguration : AuditConfiguration<AuditAssignmentResource> { }

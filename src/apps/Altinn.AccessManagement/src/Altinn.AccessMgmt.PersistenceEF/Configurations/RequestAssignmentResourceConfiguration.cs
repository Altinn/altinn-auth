using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RequestAssignmentResourceConfiguration : IEntityTypeConfiguration<RequestAssignmentResource>
{
    public void Configure(EntityTypeBuilder<RequestAssignmentResource> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.Action);

        builder.PropertyWithReference(navKey: t => t.Assignment, foreignKey: t => t.AssignmentId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);
        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id, deleteBehavior: DeleteBehavior.Cascade);

        builder.HasIndex(["AssignmentId", "ResourceId"]).IncludeProperties(["Status"]);
    }
}

public class AuditRequestAssignmentResourceConfiguration : AuditConfiguration<AuditRequestAssignmentResource> { }

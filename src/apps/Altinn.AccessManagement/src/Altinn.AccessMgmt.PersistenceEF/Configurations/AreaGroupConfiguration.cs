using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class AreaGroupConfiguration : IEntityTypeConfiguration<AreaGroup>
{
    public void Configure(EntityTypeBuilder<AreaGroup> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.Urn).IsRequired();
        builder.PropertyWithReference(navKey: t => t.EntityType, foreignKey: t => t.EntityTypeId, principalKey: t => t.Id);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class AuditAreaGroupConfiguration : AuditConfiguration<AuditAreaGroup> { }

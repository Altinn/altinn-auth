using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area> 
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.Urn).IsRequired();
        builder.Property(t => t.IconUrl);
        builder.PropertyWithReference(navKey: t => t.Group, foreignKey: t => t.GroupId, principalKey: t => t.Id);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class AuditAreaConfiguration : AuditConfiguration<AuditArea> { }

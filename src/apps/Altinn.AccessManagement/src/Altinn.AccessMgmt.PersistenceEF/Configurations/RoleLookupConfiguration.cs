using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RoleLookupConfiguration : IEntityTypeConfiguration<RoleLookup>
{
    public void Configure(EntityTypeBuilder<RoleLookup> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.PropertyWithReference(navKey: t => t.Role, foreignKey: t => t.RoleId, principalKey: t => t.Id);
        builder.Property(t => t.Key).IsRequired();
        builder.Property(t => t.Value).IsRequired();

        builder.HasIndex(t => new { t.RoleId, t.Key }).IncludeProperties(p => new { p.Value, p.Id }).IsUnique();
    }
}

public class AuditRoleLookupConfiguration : AuditConfiguration<AuditRoleLookup> { }

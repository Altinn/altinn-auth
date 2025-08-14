using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role> 
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToDefaultTable();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired().Translate();
        builder.Property(t => t.Code).IsRequired();
        builder.Property(t => t.Urn).IsRequired();
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.IsKeyRole).HasDefaultValue(false);
        builder.Property(t => t.IsAssignable).HasDefaultValue(false);
        builder.PropertyWithReference(navKey: t => t.Provider, foreignKey: t => t.ProviderId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.EntityType, foreignKey: t => t.EntityTypeId, principalKey: t => t.Id);

        builder.HasIndex(t => t.Urn).IsUnique();
        builder.HasIndex(t => new { t.ProviderId, t.Name }).IsUnique();
        builder.HasIndex(t => new { t.ProviderId, t.Code }).IsUnique();
    }
}

public class AuditRoleConfiguration : AuditConfiguration<AuditRole> { }

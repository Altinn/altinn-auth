using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider> 
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToDefaultTable();
        builder.EnableAudit();

        builder.HasKey(p => p.Id);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.RefId);
        builder.Property(t => t.Code);
        builder.Property(t => t.LogoUrl);
        builder.PropertyWithReference(navKey: t => t.Type, foreignKey: t => t.TypeId, principalKey: t => t.Id);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class AuditProviderConfiguration : AuditConfiguration<AuditProvider> { }

using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceType> 
{
    public void Configure(EntityTypeBuilder<ResourceType> builder)
    {
        builder.ToDefaultTable();
        builder.HasKey(p => p.Id);
        builder.Property(t => t.Name).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class AuditResourceTypeConfiguration : AuditConfiguration<AuditResourceType> { }

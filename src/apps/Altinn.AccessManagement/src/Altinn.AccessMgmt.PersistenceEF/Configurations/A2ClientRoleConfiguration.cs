using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class A2ClientRoleConfiguration : IEntityTypeConfiguration<A2ClientRole>
{
    public void Configure(EntityTypeBuilder<A2ClientRole> builder)
    {
        builder.ToDefaultTable();

        builder.HasKey(p => p.Id);
        builder.HasIndex(t => new { t.FacilitatorId, t.FromId }).IncludeProperties(t => t.Id);
    }
}

using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ActivityLogBackfillProgressConfiguration : IEntityTypeConfiguration<ActivityLogBackfillProgress>
{
    public void Configure(EntityTypeBuilder<ActivityLogBackfillProgress> builder)
    {
        builder.ToDefaultTable();

        builder.HasKey(p => p.Source);
        builder.Property(p => p.Source).IsRequired();
        builder.Property(p => p.Cutoff).IsRequired();
    }
}

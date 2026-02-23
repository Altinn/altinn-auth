using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations
{
    public class RightImportProgressConfiguration : IEntityTypeConfiguration<RightImportProgress>
    {
        public void Configure(EntityTypeBuilder<RightImportProgress> builder)
        {
            builder.ToDefaultTable();

            builder.HasKey(p => p.Id);

            builder.HasIndex(p => new { p.DelegationChangeId, p.OriginType })
                .IsUnique();
        }
    }
}

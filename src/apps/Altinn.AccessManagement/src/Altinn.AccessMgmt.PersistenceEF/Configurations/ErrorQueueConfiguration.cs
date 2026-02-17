using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ErrorQueueConfiguration : IEntityTypeConfiguration<ErrorQueue>
{
    public void Configure(EntityTypeBuilder<ErrorQueue> builder)
    {
        builder.ToDefaultTable();
        
        builder.HasKey(p => p.Id);
    }
}

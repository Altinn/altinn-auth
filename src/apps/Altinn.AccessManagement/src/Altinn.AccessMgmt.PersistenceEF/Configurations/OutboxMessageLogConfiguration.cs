using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class OutboxMessageLogConfiguration : IEntityTypeConfiguration<OutboxMessageLog>
{
    public void Configure(EntityTypeBuilder<OutboxMessageLog> builder)
    {
        builder.ToDefaultTable();
        
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Attempt);
        builder.Property(p => p.Log);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAdd();

        builder.HasOne(p => p.OutboxMessage)
            .WithMany(p => p.OutboxMessageLogs)
            .HasForeignKey(p => p.OutboxMessageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

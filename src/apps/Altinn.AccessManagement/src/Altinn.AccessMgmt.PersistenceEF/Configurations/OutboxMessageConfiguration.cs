using System.Drawing;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToDefaultTable();
        
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.RefId);

        builder.Property(p => p.CompletedAt);
        builder.Property(p => p.CorrelationId);
        
        builder.Property(p => p.Data)
            .HasColumnType("jsonb")
            .IsRequired();
        
        builder.HasMany(p => p.OutboxMessageLogs)
            .WithOne(p => p.OutboxMessage);

        builder.Property(p => p.Handler);
        builder.Property(p => p.Retries)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.Schedule);
        builder.Property(p => p.StartedAt);

        builder.Property(p => p.HandlerMessage);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasDefaultValue(OutboxStatus.Pending)
            .IsRequired();

        builder.Property(b => b.Timeout)
            .HasDefaultValue(TimeSpan.FromSeconds(10));
    }
}

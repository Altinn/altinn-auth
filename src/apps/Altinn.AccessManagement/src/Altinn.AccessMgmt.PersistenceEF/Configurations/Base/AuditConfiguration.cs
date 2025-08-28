using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations.Base;

public abstract class AuditConfiguration<T> : IEntityTypeConfiguration<T>, IAuditDbConfiguration
    where T : class, IAudit
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ToDefaultAuditTable();
        builder.HasKey(k => k.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Audit_ValidFrom).HasColumnName("audit_validfrom");
        builder.Property(e => e.Audit_ValidTo).HasColumnName("audit_validto");
        builder.Property(e => e.Audit_ChangedBy).HasColumnName("audit_changedby");
        builder.Property(e => e.Audit_ChangedBySystem).HasColumnName("audit_changedbysystem");
        builder.Property(e => e.Audit_ChangeOperation).HasColumnName("audit_changeoperation");
        builder.Property(e => e.Audit_DeletedBy).HasColumnName("audit_deletedby");
        builder.Property(e => e.Audit_DeletedBySystem).HasColumnName("audit_deletedbysystem");
        builder.Property(e => e.Audit_DeleteOperation).HasColumnName("audit_deleteoperation");
    }
}

using Altinn.AccessMgmt.PersistenceEF.Configurations.Base;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class RelationConfiguration : IEntityTypeConfiguration<Relation>
{
    public void Configure(EntityTypeBuilder<Relation> builder)
    {
        builder.ConfigureAsView("relation", "dbo");

        builder.PropertyWithReference(navKey: t => t.From, foreignKey: t => t.FromId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.To, foreignKey: t => t.ToId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Via, foreignKey: t => t.ViaId, principalKey: t => t.Id, required: false);
    }
}

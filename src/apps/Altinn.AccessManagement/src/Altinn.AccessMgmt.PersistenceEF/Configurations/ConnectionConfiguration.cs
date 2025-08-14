using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ConfigureAsView("connection", "dbo");

        builder.PropertyWithReference(navKey: t => t.From, foreignKey: t => t.FromId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.To, foreignKey: t => t.ToId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Via, foreignKey: t => t.FacilitatorId, principalKey: t => t.Id);
    }
}

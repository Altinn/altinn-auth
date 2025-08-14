using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ConnectionPackageConfiguration : IEntityTypeConfiguration<ConnectionPackage>
{
    public void Configure(EntityTypeBuilder<ConnectionPackage> builder)
    {
        builder.ConfigureAsView(nameof(ConnectionPackage).ToLower(), BaseConfiguration.BaseSchema);
    }
}

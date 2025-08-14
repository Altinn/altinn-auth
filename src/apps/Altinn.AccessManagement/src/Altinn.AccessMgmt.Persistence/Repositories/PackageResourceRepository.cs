using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Services;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories;

/// <summary>
/// Data service for PackageResource
/// </summary>
public class PackageResourceRepository : CrossRepository<PackageResource, ExtPackageResource, Package, ExtResource>, IPackageResourceRepository
{
    /// <inheritdoc/>
    public PackageResourceRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    {
    }
}

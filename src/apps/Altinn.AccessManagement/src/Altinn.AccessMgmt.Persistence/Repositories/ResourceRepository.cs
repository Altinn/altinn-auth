using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Services;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories;

/// <summary>
/// Data service for Resource
/// </summary>
public class ResourceRepository : ExtendedRepository<Resource, ExtResource>, IResourceRepository
{
    /// <inheritdoc/>
    public ResourceRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    {
    }
}

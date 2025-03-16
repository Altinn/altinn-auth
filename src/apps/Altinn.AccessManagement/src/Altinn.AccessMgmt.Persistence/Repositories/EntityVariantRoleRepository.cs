using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Services;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories;

/// <summary>
/// Data service for EntityVariantRole
/// </summary>
public class EntityVariantRoleRepository : CrossRepository<EntityVariantRole, ExtEntityVariantRole, EntityVariant, Role>, IEntityVariantRoleRepository
{
    /// <inheritdoc/>
    public EntityVariantRoleRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    {
    }
}

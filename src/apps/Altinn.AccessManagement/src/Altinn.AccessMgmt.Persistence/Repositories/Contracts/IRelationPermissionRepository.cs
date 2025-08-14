using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Repositories.Contracts;

/// <inheritdoc/>
public interface IRelationPermissionRepository : IDbExtendedRepository<Relation, ExtRelation> 
{
    Task<IEnumerable<PackageDelegationCheck>> GetAssignableAccessPackages(Guid fromId, Guid toId, IEnumerable<Guid> packageIds, CancellationToken cancellationToken = default);
}

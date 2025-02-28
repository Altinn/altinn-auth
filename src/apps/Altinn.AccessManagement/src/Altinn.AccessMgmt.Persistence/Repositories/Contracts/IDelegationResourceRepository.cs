using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories.Contracts;

/// <inheritdoc/>
public interface IDelegationResourceRepository : IDbCrossRepository<DelegationResource, ExtDelegationResource, Delegation, Resource> { }

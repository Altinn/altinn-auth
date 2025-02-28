using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories.Contracts;

/// <inheritdoc/>
public interface IDelegationPackageRepository : IDbCrossRepository<DelegationPackage, ExtDelegationPackage, Delegation, Package> { }

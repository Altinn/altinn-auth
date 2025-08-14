using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Repositories.Contracts;

/// <inheritdoc/>
public interface IPackageResourceRepository : IDbCrossRepository<PackageResource, ExtPackageResource, Package, ExtResource> { }

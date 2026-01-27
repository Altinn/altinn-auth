using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Delegation-check service which provides information on which delegations the user is allowed to perform.
/// </summary>
public interface IDelegationCheckService
{
    /// <summary>
    /// Performs delegation check of access packages. Gets a list of packages where the user have permission to assign the packages to others, on behalf of the specified party.
    /// </summary>
    public Task<IEnumerable<AccessPackageDto.AccessPackageDtoCheck>> GetAssignableAccessPackages(Guid toId, Guid fromId, IEnumerable<Guid>? packageIds = null, CancellationToken cancellationToken = default);
}

using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Manage Roles
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Get all roles
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role based on code
    /// </summary>
    /// <param name="code">Role code</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetByCode(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role based on one or more ids
    /// </summary>
    /// <param name="ids">Role identities</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetById(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role for provider
    /// </summary>
    /// <param name="providerId">Provider identity</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetByProvider(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role resources
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <param name="variantId">Variant to check packages for</param>
    /// <param name="includePackageResoures">Include packages and resources in them</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<ResourceDto>> GetRoleResources(Guid id, Guid? variantId = null, bool includePackageResoures = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role packages
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <param name="variantId">Variant to check packages for</param>
    /// <param name="includeResources">Include resources in packages</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<PackageDto>> GetRolePackages(Guid id, Guid? variantId = null, bool includeResources = false, CancellationToken cancellationToken = default);
}

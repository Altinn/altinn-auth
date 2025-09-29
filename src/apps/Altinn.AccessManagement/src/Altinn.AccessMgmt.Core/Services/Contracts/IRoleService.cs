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
    /// Get role based on id
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<RoleDto> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role based on Lookup
    /// </summary>
    /// <param name="key">Key from lookup</param>
    /// <param name="value">Value from lookup</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<RoleDto> GetByKeyValue(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get possible lookup keys
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<string>> GetLookupKeys(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role for provider
    /// </summary>
    /// <param name="providerId">Provider identity</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetByProvider(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get packages for role
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RolePackageDto>> GetPackagesForRole(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get resources for role
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<Resource>> GetRoleResources(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get resources for role from packages
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<Resource>> GetRolePackageResources(Guid id, CancellationToken cancellationToken = default);
}

using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Manage Roles
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Get all roles
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetAll();

    /// <summary>
    /// Get role based on code
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetByCode(string code);

    /// <summary>
    /// Get role based on id
    /// </summary>
    /// <returns></returns>
    Task<RoleDto> GetById(Guid id);

    /// <summary>
    /// Get role based on Lookup
    /// </summary>
    /// <param name="key">Key from lookup</param>
    /// <param name="value">Value from lookup</param>
    /// <returns></returns>
    Task<RoleDto> GetByKeyValue(string key, string value);

    /// <summary>
    /// Get possible lookup keys
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<string>> GetLookupKeys();

    /// <summary>
    /// Get role for provider
    /// </summary>
    /// <param name="providerId">Provider identity</param>
    /// <returns></returns>
    Task<IEnumerable<RoleDto>> GetByProvider(Guid providerId);

    /// <summary>
    /// Get packages for role
    /// </summary>
    /// <param name="id">Role identity</param>
    /// <returns></returns>
    Task<IEnumerable<RolePackageDto>> GetPackagesForRole(Guid id);
}

using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Manage Roles
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Get role based on code
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<Role>> GetByCode(string code);

    /// <summary>
    /// Get role based on id
    /// </summary>
    /// <returns></returns>
    Task<ExtRole> GetById(Guid id);

    /// <summary>
    /// Get role based on Lookup
    /// </summary>
    /// <param name="key">Key from lookup</param>
    /// <param name="value">Value from lookup</param>
    /// <returns></returns>
    Task<IEnumerable<Role>> GetByKeyValue(string key, string value);

    /// <summary>
    /// Get role for provider
    /// </summary>
    /// <param name="providerId">Provider identity</param>
    /// <returns></returns>
    Task<IEnumerable<ExtRole>> GetByProvider(Guid providerId);
}

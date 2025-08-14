using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Entity Service
/// </summary>
public interface IEntityService
{
    /// <summary>
    /// Get Entity based on OrgNo
    /// </summary>
    /// <param name="orgNo">Organization No</param>
    /// <returns></returns>
    Task<Entity> GetByOrgNo(string orgNo);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="persNo">persNo</param>
    /// <returns></returns>
    Task<Entity> GetByPersNo(string persNo);

    /// <summary>
    /// Get Entity based on ProfileId
    /// </summary>
    /// <param name="profileId">profileId</param>
    /// <returns></returns>
    Task<Entity> GetByProfile(string profileId);

    /// <summary>
    /// Get parent Entity based on parentId
    /// </summary>
    /// <param name="parentId">profileId</param>
    /// <returns></returns>
    Task<Entity> GetParent(Guid parentId);

    /// <summary>
    /// Get all child entities based on parentId
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <returns></returns>
    Task<IEnumerable<Entity>> GetChildren(Guid parentId);
}

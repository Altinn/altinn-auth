using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

public interface IEntityService
{
    ValueTask<Entity> GetEntity(Guid id, CancellationToken cancellationToken);

    Task<bool> CreateEntity(Entity entity, CancellationToken cancellationToken);

    ValueTask<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get Entity based on OrgNo
    /// </summary>
    /// <param name="orgNo">Organization No</param>
    /// <returns></returns>
    Task<Entity> GetByOrgNo(string orgNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="persNo">persNo</param>
    /// <returns></returns>
    Task<Entity> GetByPersNo(string persNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on ProfileId
    /// </summary>
    /// <param name="profileId">profileId</param>
    /// <returns></returns>
    Task<Entity> GetByProfile(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parent Entity based on parentId
    /// </summary>
    /// <param name="parentId">profileId</param>
    /// <returns></returns>
    Task<Entity> GetParent(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all child entities based on parentId
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <returns></returns>
    Task<IEnumerable<Entity>> GetChildren(Guid parentId, CancellationToken cancellationToken = default);
}

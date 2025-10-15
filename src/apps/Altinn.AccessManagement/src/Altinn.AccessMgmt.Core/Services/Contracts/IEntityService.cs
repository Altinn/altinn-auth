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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByOrgNo(string orgNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="persNo">persNo</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByPersNo(string persNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="partyId">partyid</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByPartyId(string partyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="partyId">partyid</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByPartyId(int partyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="userId">userId</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByUserId(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on PersNo
    /// </summary>
    /// <param name="userId">userId</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByUserId(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on ProfileId
    /// </summary>
    /// <param name="profileId">profileId</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByProfileId(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Entity based on ProfileId
    /// </summary>
    /// <param name="profileId">profileId</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetByProfileId(int profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parent Entity based on parentId
    /// </summary>
    /// <param name="parentId">profileId</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<Entity> GetParent(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all child entities based on parentId
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<IEnumerable<Entity>> GetChildren(Guid parentId, CancellationToken cancellationToken = default);
}

using System.Threading.Tasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.Platform.Authorization.Repositories.Interface
{
    /// <summary>
    /// Interface for operations on instance information
    /// </summary>
    public interface IInstanceMetadataRepository
    {
        /// <summary>
        /// Gets auth info for a process
        /// </summary>
        /// <param name="instanceId">the instance id</param>
        /// <returns>Auth info</returns>
        Task<AuthInfo> GetAuthInfo(string instanceId);
    }
}

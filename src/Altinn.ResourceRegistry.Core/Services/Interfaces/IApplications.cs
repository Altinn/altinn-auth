using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.ResourceRegistry.Core.Services.Interfaces
{
    /// <summary>
    /// Interface 
    /// </summary>
    public interface IApplications
    {
        /// <summary>
        /// Get a full list of Altinn 3 applications
        /// </summary>
        /// <returns></returns>
        Task<ApplicationList> GetApplicationList();
    }
}

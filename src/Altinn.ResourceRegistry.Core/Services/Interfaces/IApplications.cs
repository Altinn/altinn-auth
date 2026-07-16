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
        /// <param name="includeMigratedApps">filter migrated apps from A1/A2, default er false</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<ApplicationList> GetApplicationList(bool includeMigratedApps, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single Altinn 3 application directly from Storage by org and app name.
        /// Unlike <see cref="GetApplicationList"/> this bypasses the cached application list, so a freshly
        /// published app is resolvable immediately instead of returning null until the list cache expires.
        /// </summary>
        /// <param name="org">The organisation/service owner code</param>
        /// <param name="app">The application name (without the org prefix)</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The application, or <see langword="null"/> if it does not exist</returns>
        Task<Application> GetApplication(string org, string app, CancellationToken cancellationToken = default);
    }
}

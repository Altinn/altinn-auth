using Altinn.ResourceRegistry.Core.Models.Altinn2;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Interface to support various queries agaoim
    /// </summary>
    public interface IAltinn2Services
    {
        /// <summary>
        /// Return a list of Available services from Altinn 2
        /// </summary>
        public Task<List<AvailableService>> AvailableServices(int languageId);
    }
}

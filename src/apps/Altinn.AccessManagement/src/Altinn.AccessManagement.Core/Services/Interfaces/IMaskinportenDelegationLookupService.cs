using Altinn.AccessManagement.Core.Models;

namespace Altinn.AccessManagement.Core.Services.Interfaces
{
    /// <summary>
    /// Service for looking up active MaskinportenSchema delegations between organizations,
    /// used by the Maskinporten delegations proxy endpoint consumed by Maskinporten and service owners.
    /// </summary>
    public interface IMaskinportenDelegationLookupService
    {
        /// <summary>
        /// Gets active MaskinportenSchema delegations, filtered by supplier organization,
        /// consumer organization and/or scope.
        /// </summary>
        /// <param name="supplierOrg">Organization number of the supplier that received the delegation</param>
        /// <param name="consumerOrg">Organization number of the consumer that gave the delegation</param>
        /// <param name="scope">Maskinporten scope the delegation gives access to</param>
        /// <param name="cancellationToken">CancellationToken</param>
        Task<List<Delegation>> GetMaskinportenDelegations(string? supplierOrg, string? consumerOrg, string? scope, CancellationToken cancellationToken = default);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Register.Models;

namespace Altinn.Platform.Authorization.Services.Interfaces
{
    /// <summary>
    /// Interface to handle services exposed in Platform Register
    /// </summary>
    public interface IRegisterService
    {
        /// <summary>
        /// Returns party information
        /// </summary>
        /// <param name="partyId">The partyId</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The party for the given partyId</returns>
        Task<Party> GetParty(int partyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of parties by their party ids
        /// </summary>
        /// <param name="partyIds">List of partyIds to lookup</param>
        /// <param name="includeSubunits">(Optional) Whether subunits should be included as ChildParties, if any of the lookup party IDs are for a main unit</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>List of parties</returns>
        Task<List<Party>> GetPartiesAsync(List<int> partyIds, bool includeSubunits = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Party lookup
        /// </summary>
        /// <param name="orgNo">organisation number</param>
        /// <param name="person">f or d number</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<Party> PartyLookup(string orgNo, string person, CancellationToken cancellationToken = default);
    }
}

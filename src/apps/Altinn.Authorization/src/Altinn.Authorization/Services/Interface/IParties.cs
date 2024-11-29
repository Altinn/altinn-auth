using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.Platform.Authorization.Services.Interface
{
    /// <summary>
    /// Interface for actions related to actor
    /// </summary>
    public interface IParties
    {
        /// <summary>
        /// Method that fetches parties list based on user id
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>list of parties that the logged in user can represent</returns>
        Task<List<Party>> GetParties(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method that fetches a given party
        /// </summary>
        /// <param name="partyId">The party id</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>The party</returns>
        Task<Party> GetParty(int partyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method that fetches a list of PartyIds the given user id has key role access to (where the user inherit delegations to their organization)
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>list of PartyIds where the logged in user have key role access</returns>
        Task<List<int>> GetKeyRoleParties(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method that fetches a list of main units for the input list of sub unit partyIds. If any of the input partyIds are not a sub unit the response model will have null values for main unit properties.
        /// </summary>
        /// <param name="subunitPartyIds">The list of PartyIds to check and retrieve any main units for</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>list of main units</returns>
        Task<List<MainUnit>> GetMainUnits(MainUnitQuery subunitPartyIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies that the selected party is contained in the user's party list
        /// </summary>
        /// <param name="userId">The user id"</param>
        /// <param name="partyId">The party id"</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns> Boolean indicating whether or not the user can represent the selected party.</returns>
        Task<bool> ValidateSelectedParty(int userId, int partyId, CancellationToken cancellationToken = default);
    }
}

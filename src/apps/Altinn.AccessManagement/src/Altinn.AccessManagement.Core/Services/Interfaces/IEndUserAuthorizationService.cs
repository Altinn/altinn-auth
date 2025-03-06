using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.AccessManagement.Core.Services.Interfaces
{
    /// <summary>
    /// Service for end user authorization
    /// </summary>
    public interface IEndUserAuthorizationService
    {
        /// <summary>
        /// Checks if a party is in the list of authorized parties
        /// </summary>
        /// <param name="userPartyUuid">The UUID of the user party</param>
        /// <param name="fromPartyUuid">The UUID of the party from which the authorization is being checked</param>
        /// <param name="directionPartyUuid">The UUID of the party to which the authorization is being checked</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the party is in the list of authorized parties</returns>
        public Task<bool> HasPartyInAuthorizedParties(Guid? userPartyUuid, Guid? fromPartyUuid, Guid? directionPartyUuid);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authenticaiton.Extensions;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authorization.Controllers
{
    /// <summary>
    /// Contains all actions related to the party
    /// </summary>
    [Route("authorization/api/v1/parties")]
    [ApiController]
    public class PartiesController : ControllerBase
    {
        private readonly IParties _partiesWrapper;
        private readonly IAccessManagementWrapper _accessMgmt;
        private readonly IFeatureManager _featureManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartiesController"/> class
        /// </summary>
        public PartiesController(IParties partiesWrapper, IAccessManagementWrapper accessManagement, IFeatureManager featureManager)
        {
            _partiesWrapper = partiesWrapper;
            _accessMgmt = accessManagement;
            _featureManager = featureManager;
        }

        /// <summary>
        /// Gets the list of parties that the logged in user can represent
        /// </summary>
        /// <param name="userId">the user id</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetPartyList(int userId, CancellationToken cancellationToken = default)
        {
            if (userId == 0)
            {
                return NotFound();
            }
            
            int? authnUserId = User.GetUserIdAsInt();
            if (userId != authnUserId)
            {
                return Forbid();
            }

            List<Party> partyList = null;
            if (await _featureManager.IsEnabledAsync(FeatureFlags.AccessManagementAuthorizedParties))
            {
                partyList = await GetAuthorizedParties(cancellationToken);
            }
            else
            {
                partyList = await _partiesWrapper.GetParties(userId, cancellationToken);
            }

            if (partyList == null || partyList.Count == 0)
            {
                return NotFound();
            }
            else
            {
                return Ok(partyList);
            }
        }

        /// <summary>
        /// Verifies that the user can represent the given party
        /// </summary>
        /// <param name="userId">The user id"</param>
        /// <param name="partyId">The party id"</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        [HttpGet("{partyId}/validate")]
        [Authorize]
        public async Task<ActionResult> ValidateSelectedParty(int userId, int partyId, CancellationToken cancellationToken = default)
        {
            if (userId == 0 || partyId == 0)
            {
                return NotFound();
            }

            if (await _featureManager.IsEnabledAsync(FeatureFlags.AccessManagementAuthorizedParties))
            {
                int? authnUserId = User.GetUserIdAsInt();
                if (userId != authnUserId)
                {
                    return Forbid();
                }
            
                return Ok(await ValidateSelectedAuthorizedParty(partyId, cancellationToken));
            }
            else
            {
                return Ok(await _partiesWrapper.ValidateSelectedParty(userId, partyId, cancellationToken));
            }
        }

        private async Task<List<Party>> GetAuthorizedParties(CancellationToken cancellationToken)
        {
            IEnumerable<AuthorizedPartyDto> authorizedParties = await _accessMgmt.GetAuthorizedParties(cancellationToken);
            return authorizedParties.Select(p => p.GetAsParty()).ToList();
        }

        private async Task<bool> ValidateSelectedAuthorizedParty(int partyId, CancellationToken cancellationToken)
        {
            AuthorizedPartyDto authorizedParty = await _accessMgmt.GetAuthorizedParty(partyId, cancellationToken);
            return authorizedParty != null;
        }
    }
}

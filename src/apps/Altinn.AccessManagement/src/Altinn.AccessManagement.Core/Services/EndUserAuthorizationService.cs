using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.Core.Services
{
    /// <summary>
    /// Service for end user authorization
    /// </summary>
    public class EndUserAuthorizationService : IEndUserAuthorizationService
    {
        private readonly ILogger<IEndUserAuthorizationService> _logger;
        private readonly IAuthorizedPartiesService _authorizedPartiesService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndUserAuthorizationService"/> class.
        /// </summary>
        /// <param name="authorizedPartiesService">Service to get authorized parties</param>
        /// <param name="logger">Logger instance for logging</param>
        public EndUserAuthorizationService(
            IAuthorizedPartiesService authorizedPartiesService,
            ILogger<IEndUserAuthorizationService> logger)
        {
            _authorizedPartiesService = authorizedPartiesService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> HasPartyInAuthorizedParties(Guid? userPartyUuid, Guid? fromPartyUuid, Guid? directionPartyUuid)
        {
            // The user is not authorized to access the resource by policy check if the user has party in authorized party list
            if (fromPartyUuid == null || directionPartyUuid == null || userPartyUuid == null || !directionPartyUuid.Equals(fromPartyUuid))
            {
                return false;
            }

            List<AuthorizedParty> authorizedParties = await _authorizedPartiesService.GetAuthorizedPartiesForUser(1, true, includeAuthorizedResourcesThroughRoles: false, default);
            AuthorizedParty authorizedParty = authorizedParties.Find(ap => ap.PartyUuid == directionPartyUuid && !ap.OnlyHierarchyElementWithNoAccess) ??
            authorizedParties.SelectMany(ap => ap.Subunits).FirstOrDefault(subunit => subunit.PartyUuid == directionPartyUuid);

            if (authorizedParty != null)
            {
                return true;
            }

            return false;
        }
    }
}

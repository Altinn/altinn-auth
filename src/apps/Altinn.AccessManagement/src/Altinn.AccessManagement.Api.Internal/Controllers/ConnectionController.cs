using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Internal.Controllers
{
    /// <summary>
    /// Endpoint to expose Connections
    /// Connections are a combination of Assignments and Delegations
    /// </summary>
    [Route("/accessmanagement/api/v1/connections")]
    [ApiController]
    public class ConnectionController(
        IConnectionRepository connectionRepository,
        IConnectionPackageRepository connectionPackageRepository,
        IConnectionResourceRepository connectionResourceRepository,
        IDelegationService delegationService,
        IConnectionService connectionService
        ) : ControllerBase
    {
        private readonly IConnectionRepository connectionRepository = connectionRepository;
        private readonly IConnectionPackageRepository connectionPackageRepository = connectionPackageRepository;
        private readonly IConnectionResourceRepository connectionResourceRepository = connectionResourceRepository;
        private readonly IDelegationService delegationService = delegationService;
        private readonly IConnectionService connectionService = connectionService;

        /// <summary>
        /// Alle enheter {id} har gitt tilgang til.
        /// </summary>
        [Route("testing")]
        [HttpGet]
        public async Task<ActionResult<ExtConnection>> GetSingleTesting(Guid? fromId, Guid? toId)
        {
            if (!fromId.HasValue && !toId.HasValue)
            {
                return BadRequest();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, fromId);
            filter.Equal(t => t.ToId, toId);
            var res = await connectionRepository.Get(filter);

            return Ok(res);
        }

        /// <summary>
        /// Alle enheter {id} har gitt tilgang til.
        /// </summary>
        [Route("testingpackages")]
        [HttpGet]
        public async Task<ActionResult<ConnectionPackage>> GetSingleTestingPackages(Guid? fromId, Guid? toId)
        {
            if (!fromId.HasValue && !toId.HasValue)
            {
                return BadRequest();
            }

            var res = await connectionService.GetPackages(fromId, toId);

            return Ok(res);
        }

        /// <summary>
        /// Alle enheter {id} har gitt tilgang til.
        /// </summary>
        [Route("{id}")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ExtConnection>> GetSingle(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var res = await connectionRepository.GetExtended(id);

            return Ok(res);
        }

        /// <summary>
        /// Alle enheter {id} har gitt tilgang til.
        /// </summary>
        [Route("given/{id}")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ExtConnection>> GetGiven(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FromId, id);
            var res = await connectionRepository.GetExtended(filter);

            return Ok(res);
        }

        /// <summary>
        /// Alle enheter {id} har fått tilgang fra.
        /// </summary>
        [Route("recived/{id}")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ExtConnection>> GetRecived(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.ToId, id);
            var res = await connectionRepository.GetExtended(filter);

            return Ok(res);
        }

        /// <summary>
        /// Alle enheter der {id} fasiliterer en delegering.
        /// </summary>
        [Route("facilitated/{id}")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ExtConnection>> GetFacilitated(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var filter = connectionRepository.CreateFilterBuilder();
            filter.Equal(t => t.FacilitatorId, id);
            var res = await connectionRepository.GetExtended(filter);

            return Ok(res);
        }

        /// <summary>
        /// Alle enheter der {id} fasiliterer en delegering.
        /// </summary>
        [Route("{from}/{to}/packages")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<Package>> GetPackages(Guid from, Guid to)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var connectionFilter = connectionRepository.CreateFilterBuilder();
            connectionFilter.Equal(t => t.FromId, from);
            connectionFilter.Equal(t => t.ToId, to);
            var connections = await connectionRepository.Get(connectionFilter);

            var filter = connectionPackageRepository.CreateFilterBuilder();
            filter.In(t => t.Id, connections.Select(t => t.Id));
            var r = await connectionPackageRepository.GetExtended(filter);

            return Ok(r.Select(t => t.Package));
        }

        /// <summary>
        /// All packages on connection
        /// </summary>
        [Route("{id}/packages")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<Package>> GetPackages(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var filter = connectionPackageRepository.CreateFilterBuilder();
            filter.Equal(t => t.Id, id);
            return Ok((await connectionPackageRepository.GetExtended(filter)).Select(t => t.Package));
        }

        /// <summary>
        /// All resources on connection
        /// </summary>
        [Route("{id}/resources")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<Resource>> GetResources(Guid id)
        {
            var userId = AuthenticationHelper.GetPartyUuid(HttpContext);
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            return Ok(await connectionResourceRepository.GetB(id));
        }
    }
}

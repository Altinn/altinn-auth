using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ProblemDetails;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// Temporary export controller. Will be removed when Altinn 2 is shut down. Made so it possible to access data from bridge
    /// </summary>
    /// 
    [Route("resourceregistry/api/v1/altinn2export")]
    [ApiController]
    public class Altinn2ExportController : ControllerBase
    {
        private readonly IAltinn2Services _altinn2ServicesClient;
        private readonly IResourceRegistryRepository _resourceRegistryRepository;
        private readonly IResourceRegistry _resourceRegistry;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Altinn2ExportController(IAltinn2Services altinnService2Client, IResourceRegistryRepository resourceRegistryRepository, IResourceRegistry resourceRegistry)
        {
            _altinn2ServicesClient = altinnService2Client;
            _resourceRegistryRepository = resourceRegistryRepository;
            _resourceRegistry = resourceRegistry;
        }

        /// <summary>
        /// Returns a Service Resources based on Altinn 2 ServiceMetadata for a service
        /// </summary>
        [HttpGet("resource")]
        [Produces("application/json")]
        public async Task<ActionResult<ServiceResource>> GetServiceResourceForAltinn2Service([FromQueryAttribute] string serviceCode, [FromQueryAttribute] int serviceEditionCode)
        {
            try
            {
                return await _altinn2ServicesClient.GetServiceResourceFromService(serviceCode, serviceEditionCode);
            }
            catch
            {
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Returns a Service Resources based on Altinn 2 ServiceMetadata for a service
        /// </summary>
        [HttpGet("policy")]
        public async Task<ActionResult> GetPolicyFromAltinn2Service([FromQueryAttribute] string serviceCode, [FromQueryAttribute] int serviceEditionCode, [FromQueryAttribute] string resourceIdentifier)
        {
            XacmlPolicy xacmlPolicy = await _altinn2ServicesClient.GetXacmlPolicy(serviceCode, serviceEditionCode, resourceIdentifier);

            string xsd;
            await using (MemoryStream stream = new MemoryStream())
            await using (var xw = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Async = true }))
            {
                XacmlSerializer.WritePolicy(xw, xacmlPolicy);
                await xw.FlushAsync();
                stream.Position = 0;
                xsd = Encoding.UTF8.GetString(stream.ToArray());
            }

            return Ok(xsd);
        }

        /// <summary>
        /// Information about number of delegations for a service
        /// </summary>
        [Authorize(Policy = AuthzConstants.POLICY_ADMIN)]
        [HttpGet("delegationcount")]
        public async Task<ActionResult<DelegationCountOverview>> GetDelegationCount([FromQueryAttribute] string serviceCode, [FromQueryAttribute] int serviceEditionCode, CancellationToken cancellationToken = default)
        {
            DelegationCountOverview delegationCount = await _altinn2ServicesClient.GetDelegationCount(serviceCode, serviceEditionCode, cancellationToken);

            return Ok(delegationCount);
        }

        /// <summary>
        /// Request a batch run of delegations from service in Altinn 2 to resource in Altinn 3
        /// </summary>
        [Authorize(Policy = AuthzConstants.POLICY_ADMIN)]
        [HttpPost("exportdelegations")]
        public async Task<ActionResult> ExportDelegations([FromBody] ExportDelegationsRequestBE exportDelegationsRequestBE, CancellationToken cancellationToken = default)
        {
            ServiceResource resource = await _resourceRegistryRepository.GetResource(exportDelegationsRequestBE.ResourceId, cancellationToken);
            if (resource == null)
            {
                return Problems.ResourceReference_NotFound.ToActionResult();
             }

            if (!await ValidateMatchingOrgForDelegaton(exportDelegationsRequestBE, resource.HasCompetentAuthority.Orgcode, cancellationToken))
            {
                return Problems.IncorrectMatchingOrgForResource.ToActionResult();
            }

            await _altinn2ServicesClient.ExportDelegations(exportDelegationsRequestBE, cancellationToken);

            return Created();
        }

        /// <summary>
        /// Sets a given service expired to hide delegation functionality. Proxy for bridge functionality. Called by Altinn Studio and used as part of the migration of delegation process
        /// </summary>
        /// <returns></returns>
        [Authorize(Policy = AuthzConstants.POLICY_ADMIN)]
        [HttpGet("setserviceeditionexpired")]
        public async Task<ActionResult> SetServiceEditionExpired([FromQueryAttribute] string externalServiceCode, [FromQueryAttribute] int externalServiceEditionCode, CancellationToken cancellationToken = default)
        {
            await _altinn2ServicesClient.SetServiceEditionExpired(externalServiceCode, externalServiceEditionCode, cancellationToken);
            return Ok();
        }

        [NonAction]
        private async Task<bool> ValidateMatchingOrgForDelegaton(ExportDelegationsRequestBE exportRequest, string org,  CancellationToken cancellationToken = default)
        {
            List<ServiceResource> altinnService = await _resourceRegistry.GetResourceList(includeApps: false, includeAltinn2: true, includeExpired: true, cancellationToken);
            if (altinnService == null)
            {
                return false;
            }

            foreach (ServiceResource resource in altinnService)
            {
                if (resource.Identifier.Equals($"se_{exportRequest.ServiceCode}_{exportRequest.ServiceEditionCode}"))
                {       
                    if (resource.HasCompetentAuthority.Orgcode.Equals(org, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    }
}

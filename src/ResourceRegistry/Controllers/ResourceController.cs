using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Extensions;
using Altinn.ResourceRegistry.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Nerdbank.Streams;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// Controller responsible for all operations for managing resources in the resource registry
    /// </summary>
    [Route("resourceregistry/api/v1/resource")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private IResourceRegistry _resourceRegistry;
        private readonly ILogger<ResourceController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceController"/> controller.
        /// </summary>
        /// <param name="resourceRegistry">Service implementation for operations on resources in the resource registry</param>
        /// <param name="logger">Logger</param>
        public ResourceController(
            IResourceRegistry resourceRegistry,
            ILogger<ResourceController> logger)
        {
            _resourceRegistry = resourceRegistry;
            _logger = logger;
        }

        /// <summary>
        /// List of all resources
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        [HttpGet("resourcelist")]
        [Produces("application/json")]
        public async Task<List<ServiceResource>> ResourceList(CancellationToken cancellationToken)
        {
            return await _resourceRegistry.GetResourceList(includeApps: true, includeAltinn2: true, cancellationToken);
        }

        /// <summary>
        /// List of all resources
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        [HttpGet("export")]
        [Produces("application/xml+rdf")]
        public async Task<string> Export(CancellationToken cancellationToken)
        {
            ResourceSearch search = new ResourceSearch();
            List<ServiceResource> serviceResources = await _resourceRegistry.Search(search, cancellationToken);
            string rdfString = RdfUtil.CreateRdf(serviceResources);
            return rdfString;
        }

        /// <summary>
        /// Gets a single resource by its resource identifier if it exists in the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to retrieve</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>ServiceResource</returns>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ServiceResource> Get(string id, CancellationToken cancellationToken)
        {
            return await _resourceRegistry.GetResource(id, cancellationToken);
        }

        /// <summary>
        /// Creates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="serviceResource">Service resource model to create in the resource registry</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>ActionResult describing the result of the operation</returns>
        [SuppressModelStateInvalidFilter]
        [HttpPost]
        [Authorize(Policy = AuthzConstants.POLICY_SCOPE_RESOURCEREGISTRY_WRITE)]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> Post([ValidateNever] ServiceResource serviceResource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (serviceResource.Identifier.StartsWith(ResourceConstants.SERVICE_ENGINE_RESOURCE_PREFIX)
                && (serviceResource.ResourceReferences == null
                || !serviceResource.ResourceReferences.Any()
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceCode))
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceEditionCode))))
            {
                // Uses Service engine prefix without it beeing a service engine resource
                return BadRequest("Invalid Prefix");
            }

            if (serviceResource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX)
                && (serviceResource.ResourceReferences == null 
                || !serviceResource.ResourceReferences.Any()
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ApplicationId))))
            {
                // Uses app prefix without it beeing a app resource
                return BadRequest("Invalid Prefix");
            }

            try
            {
                serviceResource.Identifier.AsFilePath();
            }
            catch
            {
                return BadRequest(
                    $"Invalid resource identifier. Cannot be empty or contain any of the characters: {string.Join(", ", Path.GetInvalidFileNameChars())}");
            }

            if (!AuthorizationUtil.HasWriteAccess(serviceResource.HasCompetentAuthority?.Organization, User))
            {
                return Forbid();
            }

            List<string> scopes = MaskinportenSchemaAuthorizer.GetMaskinportenScopesFromServiceResource(serviceResource);

            if (scopes is { Count: > 0 } && !MaskinportenSchemaAuthorizer.IsAuthorizedForChangeResourceWithScopes(scopes, HttpContext.User, out List<string> forbiddenScopes))
            {
                return Unauthorized(MaskinportenSchemaAuthorizer.CreateErrorResponseMissingPrefix(forbiddenScopes));
            }

            try
            {
                await _resourceRegistry.CreateResource(serviceResource, cancellationToken);
                return Created("/resourceregistry/api/v1/resource/" + serviceResource.Identifier, null);
            }
            catch (Exception e)
            {
                return e.Message.Contains("duplicate key value violates unique constraint")
                    ? Conflict($"The Resource already exist: {serviceResource.Identifier}")
                    : StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// Updates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="id">Resource ID</param>
        /// <param name="serviceResource">Service resource model for update in the resource registry</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>ActionResult describing the result of the operation</returns>
        [SuppressModelStateInvalidFilter]
        [HttpPut("{id}")]
        [Authorize(Policy = AuthzConstants.POLICY_SCOPE_RESOURCEREGISTRY_WRITE)]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> Put(string id, ServiceResource serviceResource, CancellationToken cancellationToken)
        {
            ServiceResource currentResource = await _resourceRegistry.GetResource(id, cancellationToken);

            if (currentResource == null)
            {
                return NotFound();
            }

            if (id != serviceResource.Identifier)
            {
                return BadRequest("Id in path does not match ID in resource");
            }
           
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (serviceResource.Identifier.StartsWith(ResourceConstants.SERVICE_ENGINE_RESOURCE_PREFIX)
                && (serviceResource.ResourceReferences == null
                || !serviceResource.ResourceReferences.Any()
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceCode))
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceEditionCode))))
            {
                // Uses Service engine prefix without it beeing a service engine resource
                return BadRequest("Invalid Prefix");
            }

            if (serviceResource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX)
                && (serviceResource.ResourceReferences == null
                || !serviceResource.ResourceReferences.Any()
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ApplicationId))))
            {
                // Uses app prefix without it beeing a app resource
                return BadRequest("Invalid Prefix");
            }

            if (!AuthorizationUtil.HasWriteAccess(serviceResource.HasCompetentAuthority?.Organization, User))
            {
                return Forbid();
            }

            List<string> scopes = MaskinportenSchemaAuthorizer.GetMaskinportenScopesFromServiceResource(serviceResource);

            if (scopes is { Count: > 0 } && !MaskinportenSchemaAuthorizer.IsAuthorizedForChangeResourceWithScopes(scopes, HttpContext.User, out List<string> forbiddenScopes))
            {
                return Unauthorized(MaskinportenSchemaAuthorizer.CreateErrorResponseMissingPrefix(forbiddenScopes));
            }

            try
            {
                await _resourceRegistry.UpdateResource(serviceResource, cancellationToken);
                return Ok();
            }
            catch (Exception e)
            {
                return e.Message.Contains("duplicate key value violates unique constraint") ? BadRequest($"The Resource already exist: {serviceResource.Identifier}") : StatusCode(500, e.Message);
            }
        }

        /// <summary>
        /// Returns the XACML policy for a resource in resource registry.
        /// </summary>
        /// <param name="id">Resource Id</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        [HttpGet("{id}/policy")]
        public async Task<ActionResult> GetPolicy(string id, CancellationToken cancellationToken)
        {
            ServiceResource resource = await _resourceRegistry.GetResource(id, cancellationToken);
            if (resource == null)
            {
                return NotFound("Unable to find resource");
            }

            Stream dataStream = await _resourceRegistry.GetPolicy(resource.Identifier, cancellationToken);

            if (dataStream == null)
            {
                return NotFound("Unable to find requested policy");
            }

            return File(dataStream, "text/xml", "policy.xml");
        }

        /// <summary>
        /// Creates or overwrites the existing XACML policy for the resource, if it pass all validation checks.
        /// The XACML policy must define at least a subject and resource, and will be used to restrict access for the resource.
        /// </summary>
        /// <param name="id">The resource identifier to store the policy for</param>
        /// <param name="policyFile">The XACML policy file</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>ActionResult describing the result of the operation</returns>
        [HttpPost("{id}/policy")]
        [HttpPut("{id}/policy")]
        [Authorize(Policy = AuthzConstants.POLICY_SCOPE_RESOURCEREGISTRY_WRITE)]
        [Produces("application/json")]
        public async Task<ActionResult> WritePolicy(string id, IFormFile policyFile, CancellationToken cancellationToken)
        {
            if (policyFile == null)
            {
                return BadRequest("The policy file can not be null");
            }

            Stream fileStream = policyFile.OpenReadStream();
            if (fileStream == null)
            {
                return BadRequest("The file stream can not be null");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Unknown resource");
            }

            ServiceResource resource = await _resourceRegistry.GetResource(id, cancellationToken);
            if (resource == null)
            {
                return BadRequest("Unknown resource");
            }

            if (!AuthorizationUtil.HasWriteAccess(resource.HasCompetentAuthority?.Organization, User))
            {
                return Forbid();
            }

            try
            {
                using var policyFileContent = await fileStream.ReadToSequenceAsync(cancellationToken);
                bool successfullyStored = await _resourceRegistry.StorePolicy(resource, policyFileContent.AsReadOnlySequence, cancellationToken);
               
                if (successfullyStored)
                {
                    return Created(id + "/policy", null);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode(500);
            }

            return BadRequest("Something went wrong in the upload of file to storage");
        }

        /// <summary>
        /// Deletes a resource from the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to delete</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthzConstants.POLICY_SCOPE_RESOURCEREGISTRY_WRITE)]
        public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            ServiceResource serviceResource = await _resourceRegistry.GetResource(id, cancellationToken);
            string orgClaim = User.GetOrgNumber();
            if (orgClaim != null)
            {
                if (!AuthorizationUtil.HasWriteAccess(serviceResource.HasCompetentAuthority?.Organization, User))
                {
                    return Forbid();
                }
            }

            await _resourceRegistry.Delete(id, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Allows for searching for resources in the resource registry
        /// </summary>
        /// <param name="search">The search model defining the search filter criterias</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A list of service resources found to match the search criterias</returns>
        [HttpGet("Search")]
        [Produces("application/json")]
        public async Task<List<ServiceResource>> Search([FromQuery] ResourceSearch search, CancellationToken cancellationToken)
        {
            return await _resourceRegistry.Search(search, cancellationToken);
        }
     }

    /// <summary>
    /// ToDo: move to a separate class
    /// </summary>
    public class SuppressModelStateInvalidFilterAttribute : Attribute, IActionModelConvention
    {
        private const string FilterTypeName = "ModelStateInvalidFilterFactory";

        /// <inheritdoc/>
        public void Apply(ActionModel action)
        {
            for (var i = 0; i < action.Filters.Count; i++)
            {
                if (action.Filters[i].GetType().Name == FilterTypeName)
                {
                    action.Filters.RemoveAt(i);
                    break;
                }
            }
        }
    }
}

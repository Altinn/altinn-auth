using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Events.Formatters;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Extensions;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
        /// <param name="includeApps">Include App resources</param>
        /// <param name="includeAltinn2">Include Altinn 2 resources</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        [HttpGet("resourcelist")]
        [Produces("application/json")]
        public async Task<List<ServiceResource>> ResourceList(
            bool includeApps = true,
            bool includeAltinn2 = true,
            CancellationToken cancellationToken = default)
        {
            return await _resourceRegistry.GetResourceList(includeApps, includeAltinn2, includeExpired: false,  cancellationToken);
        }

        /// <summary>
        /// List of all resources
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        [HttpGet("export")]
        [Produces(RdfOutputFormatter.RDFMimeType)]
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

            // Validate Resource
            if (!ServiceResourceHelper.ValidateResource(serviceResource, out Dictionary<string, List<string>> message))
            {
                foreach (KeyValuePair<string, List<string>> kvp in message)
                {
                    foreach (string validationMessage in kvp.Value)
                    {
                        ModelState.AddModelError(kvp.Key, validationMessage);
                    }
                }

                return ValidationProblem(ModelState);
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

            // Validate Resource
            // Validate Resource
            if (!ServiceResourceHelper.ValidateResource(serviceResource, out Dictionary<string, List<string>> message))
            {
                foreach (KeyValuePair<string, List<string>> kvp in message)
                {
                    foreach (string validationMessage in kvp.Value)
                    {
                        ModelState.AddModelError(kvp.Key, validationMessage);
                    }
                }

                return ValidationProblem(ModelState);
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
        /// Returns the XACML policy for a resource in resource registry.
        /// </summary>
        /// <param name="id">Resource Id</param>
        /// <param name="reloadFromXacml">Defines if subjects should be reloaded from Xacml</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>Subjects in policy</returns>
        [HttpGet("{id}/policy/subjects")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<Paginated<AttributeMatchV2>>> FindSubjectsInPolicy(string id, bool? reloadFromXacml = null, CancellationToken cancellationToken = default)
        {
            if (reloadFromXacml.HasValue && reloadFromXacml.Value)
            {
                ServiceResource serviceResource = await _resourceRegistry.GetResource(id, cancellationToken);
                if (serviceResource != null)
                {
                    await _resourceRegistry.UpdateResourceSubjectsFromResourcePolicy(serviceResource, cancellationToken);
                }
                else if (id.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX))
                {
                    // Scenario for app not loaded in to resource registry. Need to match pattern app_{org}_{app}
                    string[] idValues = id.Split('_');
                    if (idValues.Length == 3)
                    {
                        string org = idValues[1];
                        string app = idValues[2];
                        await _resourceRegistry.UpdateResourceSubjectsFromAppPolicy(org, app, cancellationToken);
                    }
                }
            }

            List<ResourceSubjects> resourceSubjects = await _resourceRegistry.FindSubjectsForResources([$"{AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute}:{id}"], cancellationToken);
            if (resourceSubjects == null || resourceSubjects.Count == 0)
            {
                return new NotFoundResult();
            }

            return Paginated.Create(resourceSubjects[0].Subjects, null);
        }

        /// <summary>
        /// Returns a list of Subject resources. For each which subject and then a list of all resources that are connected.
        /// </summary>
        /// <param name="subjects">List of subjects for resource information is needed</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>Resources where subjects have rights</returns>
        [HttpPost("bysubjects/")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<Paginated<SubjectResources>> FindResourcesForSubjects(List<string> subjects, CancellationToken cancellationToken)
        {
            List<SubjectResources> resources = await _resourceRegistry.FindResourcesForSubjects(subjects, cancellationToken);
            return Paginated.Create(resources, null);
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
            return await _resourceRegistry.GetSearchResults(search, cancellationToken);
        }

        /// <summary>
        /// Gets the updated resources since the provided last updated time (inclusive)
        /// </summary>
        /// <param name="since">Date time used for filtering</param>
        /// <param name="token">Opaque continuation token containing ResourceUrn,SubjectUrn pair to skip past on rows matching "since" exactly</param>
        /// <param name="limit">Maximum number of pairs returned (1-1000, default: 1000)</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A list of updated subject/resource pairs since provided timestamp (inclusive)</returns>
        [HttpGet("updated", Name = "updated")]
        [Produces("application/json")]
        public async Task<ActionResult<Paginated<UpdatedResourceSubject>>> UpdatedResourceSubjects([FromQuery] DateTimeOffset since, [FromQuery(Name = "token")] Opaque<UpdatedResourceSubjectsContinuationToken> token = null, [FromQuery] int limit = 1000, CancellationToken cancellationToken = default)
        {
            if (limit is < 1 or > 1000)
            {
                return new AltinnValidationProblemDetails([
                    ValidationErrors.UpdatedResourceSubjects_InvalidLimit.ToValidationError(),
                ]).ToActionResult();
            }

            (Uri ResourceUrn, Uri SubjectUrn)? skipPastPair = null;

            if (token is not null)
            {
                skipPastPair = (token.Value.ResourceUrn, token.Value.SubjectUrn);
            }

            // Use maxItems + 1 in order to determine if there are more items to fetch
            List<UpdatedResourceSubject> updatedResourceSubjects = await _resourceRegistry.FindUpdatedResourceSubjects(since.ToUniversalTime(), limit + 1, skipPastPair, cancellationToken);

            if (updatedResourceSubjects.Count != limit + 1)
            {
                return Paginated.Create(updatedResourceSubjects, null);
            }

            updatedResourceSubjects.RemoveAt(limit);
            UpdatedResourceSubject last = updatedResourceSubjects[^1];

            string nextUrl = Url.Link("updated", new
            {
                since = last.UpdatedAt.ToString("O"),
                token = Opaque.Create(new UpdatedResourceSubjectsContinuationToken(last.ResourceUrn, last.SubjectUrn)),
                limit
            });

            return Paginated.Create(updatedResourceSubjects, nextUrl);
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

    /// <summary>
    /// Continuation token for updated resource subjects. Used with "since" value to serve
    /// as tiebreaker when paginating over resource subjects having the same "updatedAt" value
    /// split across pages
    /// </summary>
    /// <param name="ResourceUrn">The resourceUrn.</param>
    /// <param name="SubjectUrn">The subjectUrn.</param>
    public record UpdatedResourceSubjectsContinuationToken(Uri ResourceUrn, Uri SubjectUrn);
}

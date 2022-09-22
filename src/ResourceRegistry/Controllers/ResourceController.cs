using System.Net;
using Altinn.Platform.Authorization.Helpers.Extensions;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ResourceRegistry.Controllers
{
    /// <summary>
    /// Controller responsible for all operations for managing resources in the resource registry
    /// </summary>
    [Route("ResourceRegistry/api/[controller]")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private IResourceRegistry _resourceRegistry;
        private IPolicyRepository _policyRepository;
        private readonly IObjectModelValidator _objectModelValidator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IPRP _prp;
        private readonly ILogger<ResourceController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceController"/> controller.
        /// </summary>
        /// <param name="resourceRegistry">Service implementation for operations on resources in the resource registry</param>
        /// <param name="policyRepository">Repository implementation for operations on policies</param>
        /// <param name="objectModelValidator">Object model validator</param>
        /// <param name="httpContextAccessor">Http context accessor</param>
        /// <param name="prp">Client implementation for policy retireval point</param>
        /// <param name="logger">Logger</param>
        public ResourceController(IResourceRegistry resourceRegistry, IPolicyRepository policyRepository, IObjectModelValidator objectModelValidator, IHttpContextAccessor httpContextAccessor, IPRP prp, ILogger<ResourceController> logger)
        {
            _resourceRegistry = resourceRegistry;
            _policyRepository = policyRepository;
            _objectModelValidator = objectModelValidator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _prp = prp;
        }

        /// <summary>
        /// Gets a single resource by its resource identifier if it exists in the resource registry
        /// </summary>
        /// <param name="id">The resource identifier to retrieve</param>
        /// <returns>ServiceResource</returns>
        [HttpGet("{id}")]
        public async Task<ServiceResource> Get(string id)
        {
            return await _resourceRegistry.GetResource(id);
        }

        /// <summary>
        /// Creates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="serviceResource">Service resource model to create in the resource registry</param>
        /// <returns>ActionResult describing the result of the operation</returns>
        [SuppressModelStateInvalidFilter]
        [HttpPost]
        public async Task<ActionResult> Post([ValidateNever] ServiceResource serviceResource)
        {
            if (serviceResource.IsComplete.HasValue && serviceResource.IsComplete.Value)
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }
            }

            await _resourceRegistry.CreateResource(serviceResource);

            return Created("/ResourceRegistry/api/" + serviceResource.Identifier, null);
        }

        /// <summary>
        /// Updates a service resource in the resource registry if it pass all validation checks
        /// </summary>
        /// <param name="serviceResource">Service resource model for update in the resource registry</param>
        /// <returns>ActionResult describing the result of the operation</returns>
        [SuppressModelStateInvalidFilter]
        [HttpPut]
        public async Task<ActionResult> Put(ServiceResource serviceResource)
        {
            if (serviceResource.IsComplete.HasValue && serviceResource.IsComplete.Value)
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }
            }

            await _resourceRegistry.UpdateResource(serviceResource);

            return Ok();
        }

        /// <summary>
        /// Creates or overwrites the existing XACML policy for the resource, if it pass all validation checks.
        /// The XACML policy must define at least a subject and resource, and will be used to restrict access for the resource.
        /// </summary>
        /// <param name="id">The resource identifier to store the policy for</param>
        /// <param name="policyFile">The XACML policy file</param>
        /// <returns>ActionResult describing the result of the operation</returns>
        [HttpPost("{id}/policy")]
        [HttpPut("{id}/policy")]
        public async Task<ActionResult> WritePolicy(string id, IFormFile policyFile)
        {
            if (policyFile == null)
            {
                throw new ArgumentException("The policy file can not be null");
            }

            Stream fileStream = policyFile.OpenReadStream();
            if (fileStream == null)
            {
                throw new ArgumentException("The file stream can not be null");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Unknown resource");
            }

            ServiceResource resource = await _resourceRegistry.GetResource(id);

            if (resource == null)
            {
                return BadRequest("Unknown resource");
            }

            string filePath = $"{resource.Identifier.AsFileName()}/policy.xml";

            try
            {
                Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(filePath, fileStream);

                bool successfullyStored = response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
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
        [HttpDelete("{id}")]
        public async void Delete(string id)
        {
            await _resourceRegistry.Delete(id);
        }

        /// <summary>
        /// Allows for searching for resources in the resource registry
        /// </summary>
        /// <param name="search">The search model defining the search filter criterias</param>
        /// <returns>A list of service resources found to match the search criterias</returns>
        [HttpGet("Search")]
        public async Task<List<ServiceResource>> Search([FromQuery] ResourceSearch search)
        {
            return await _resourceRegistry.Search(search);
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

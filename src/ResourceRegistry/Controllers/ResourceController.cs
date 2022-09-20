using Altinn.Platform.Authorization.Helpers.Extensions;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Net;
using System.Text;

namespace ResourceRegistry.Controllers
{
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

        public ResourceController(IResourceRegistry resourceRegistry, IPolicyRepository policyRepository, IObjectModelValidator objectModelValidator, IHttpContextAccessor httpContextAccessor, IPRP prp, ILogger<ResourceController> logger)
        {
            _resourceRegistry = resourceRegistry;
            _policyRepository = policyRepository;
            _objectModelValidator = objectModelValidator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _prp = prp;
        }

        [HttpGet("{id}")]
        public async Task<ServiceResource> Get(string id)
        {
            return await _resourceRegistry.GetResource(id);
        }

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

        [HttpPost("{id}/policy")]
        public async Task<ActionResult> WritePolicy(string id, IFormFile policyFile)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Unknown resource"); // ToDo: Need actual verification against existing resources
            }

            if (policyFile == null)
            {
                throw new ArgumentException("The policy file can not be null");
            }

            Stream fileStream = policyFile.OpenReadStream();
            if (fileStream == null)
            {
                throw new ArgumentException("The file stream can not be null");
            }

            string filePath = $"{id.AsFileName()}/policy.xml";

            try
            {
                Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(filePath, fileStream);

                bool successfullyStored = response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
                if (successfullyStored)
                {
                    return Created(id+"/policy", null);
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


        [HttpPut("{id}/policy")]
        public async Task<ActionResult> UpdatePolicy(string id, IFormFile policyFile)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Unknown resource"); // ToDo: Need actual verification against existing resources
            }

            if (policyFile == null)
            {
                throw new ArgumentException("The policy file can not be null");
            }

            Stream fileStream = policyFile.OpenReadStream();
            if (fileStream == null)
            {
                throw new ArgumentException("The file stream can not be null");
            }

            string filePath = $"{id.AsFileName()}/policy.xml";

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



        [HttpDelete("{id}")]
        public async void Delete(string id)
        {
            await _resourceRegistry.Delete(id);
        }

        [HttpGet("Search")]
        public async Task<List<ServiceResource>> Search([FromQuery] ResourceSearch search)
        {
            return await _resourceRegistry.Search(search);
        }
     }

    public class SuppressModelStateInvalidFilterAttribute : Attribute, IActionModelConvention
    {
        private const string FilterTypeName = "ModelStateInvalidFilterFactory";

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

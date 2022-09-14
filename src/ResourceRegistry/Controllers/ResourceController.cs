using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text;

namespace ResourceRegistry.Controllers
{
    [Route("ResourceRegistry/api/[controller]")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private IResourceRegistry _resourceRegistry;
        private readonly IObjectModelValidator _objectModelValidator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IPRP _prp;
        private readonly ILogger<ResourceController> _logger;

        public ResourceController(IResourceRegistry resourceRegistry, IObjectModelValidator objectModelValidator, IHttpContextAccessor httpContextAccessor, IPRP prp, ILogger<ResourceController> logger)
        {
            _resourceRegistry = resourceRegistry;
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
        public async Task<ActionResult> WritePolicy(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Unknown resource");
            }

         
            // Use Request.Body to capture raw data from body to support other format than JSON
            Stream content = Request.Body;

            // Request.Body returns Stream of type FrameRequestStream which can only be read once
            // Copy Request.Body to another stream that supports seeking so the content can be read multiple times
            string contentString = await new StreamReader(content, Encoding.UTF8).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(contentString))
            {
                return BadRequest("Policy file cannot be empty");
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(contentString);
            Stream dataStream = new MemoryStream(byteArray);

            try
            {
                bool successfullyStored = await _prp.WriteResourcePolicyAsync(id, dataStream);

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
        public async Task<ActionResult> UpdatePolicy(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Policy file cannot be empty");
            }


            // Use Request.Body to capture raw data from body to support other format than JSON
            Stream content = Request.Body;

            // Request.Body returns Stream of type FrameRequestStream which can only be read once
            // Copy Request.Body to another stream that supports seeking so the content can be read multiple times
            string contentString = await new StreamReader(content, Encoding.UTF8).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(contentString))
            {
                return BadRequest("Policy file cannot be empty");
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(contentString);
            Stream dataStream = new MemoryStream(byteArray);

            try
            {
                bool successfullyStored = await _prp.WriteResourcePolicyAsync(id, dataStream);

                if (successfullyStored)
                {
                    return Ok();
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

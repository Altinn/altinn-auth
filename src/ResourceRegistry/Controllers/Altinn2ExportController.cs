using System.Net;
using System.Text;
using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services;
using Azure.Core;
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public Altinn2ExportController(IAltinn2Services altinnService2Client)
        {
            _altinn2ServicesClient = altinnService2Client;   
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
                xw.Flush();
                stream.Position = 0;
                xsd = Encoding.UTF8.GetString(stream.ToArray());
            }

            return Ok(xsd);
        }
    }
}

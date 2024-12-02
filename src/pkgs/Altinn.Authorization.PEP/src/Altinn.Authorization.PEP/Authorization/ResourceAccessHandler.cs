using System;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Helpers;
using Altinn.Common.PEP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Altinn.Common.PEP.Authorization
{
    /// <summary>
    /// AuthorizationHandler that is created for handling access to app.
    /// Authorizes based om AppAccessRequirement and app id from route
    /// <see href="https://docs.asp.net/en/latest/security/authorization/policies.html"/> for details about authorization
    /// in asp.net core
    /// </summary>
    public class ResourceAccessHandler : AuthorizationHandler<ResourceAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPDP _pdp;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceAccessHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The http context accessor</param>
        /// <param name="pdp">The pdp</param>
        /// <param name="logger">The logger. </param>
        public ResourceAccessHandler(
            IHttpContextAccessor httpContextAccessor,
            IPDP pdp,
            ILogger<ResourceAccessHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _pdp = pdp;
            _logger = logger;
        }

        /// <summary>
        /// This method authorize access bases on context and requirement
        /// Is triggered by annotation on MVC action and setup in startup.
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="requirement">The requirement</param>
        /// <returns>A Task</returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceAccessRequirement requirement)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            XacmlJsonRequestRoot request = DecisionHelper.CreateDecisionRequest(context, requirement, _httpContextAccessor.HttpContext.GetRouteData(), _httpContextAccessor.HttpContext.Request.Headers);

            XacmlJsonResponse response = await _pdp.GetDecisionForRequest(request);

            if (response?.Response == null)
            {
                throw new ArgumentNullException("response");
            }

            if (!DecisionHelper.ValidatePdpDecision(response.Response, context.User))
            {
                context.Fail();
            }

            context.Succeed(requirement);
            await Task.CompletedTask;
        }
    }
}

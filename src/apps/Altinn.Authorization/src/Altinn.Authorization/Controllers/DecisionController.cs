using System.Text;
using System.Text.Json;
using System.Xml;
using Altinn.Authorization.ABAC;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Authorization.Enums;
using Altinn.Authorization.Models;
using Altinn.Authorization.Models.Register;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.ModelBinding;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Models.External;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authorization.Controllers
{
    /// <summary>
    /// This is the controller responsible for Policy Enformcent Point endpoint.
    /// It returns a Xacml Context Response based on a Context Request
    /// </summary>
    [ApiController]
    public class DecisionController : ControllerBase
    {
        private readonly PolicyDecisionPoint _pdp;
        private readonly IPolicyRetrievalPoint _prp;
        private readonly IContextHandler _contextHandler;
        private readonly IDelegationContextHandler _delegationContextHandler;
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IEventLog _eventLog;
        private readonly IFeatureManager _featureManager;
        private readonly IAccessManagementWrapper _accessManagement;
        private readonly IResourceRegistry _resourceRegistry;
        private readonly IRegisterService _registerService;
        private readonly IAccessListAuthorization _accessListAuthorization;
        private readonly IMapper _mapper;

        private readonly SortedDictionary<string, AuthInfo> _appInstanceInfo = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DecisionController"/> class.
        /// </summary>
        /// <param name="accessManagement">Service for making request the to Access Management API (PIP)</param>
        /// <param name="resourceRegistry">Service for making requests to the Resource Registry API</param>
        /// <param name="registerService">Service for making requests to the Register API</param>
        /// <param name="accessListAuthorization">Service for authorization of subjects based on Resource Registry access lists</param>
        /// <param name="contextHandler">The Context handler</param>
        /// <param name="delegationContextHandler">The delegation context handler</param>
        /// <param name="policyRetrievalPoint">The policy Retrieval point</param>
        /// <param name="delegationRepository">The delegation repository</param>
        /// <param name="logger">the logger</param>
        /// <param name="memoryCache">memory cache</param>
        /// <param name="eventLog">the authorization event logger</param>
        /// <param name="featureManager">the feature manager</param>
        /// <param name="mapper">The model mapper</param>
        public DecisionController(
            IAccessManagementWrapper accessManagement,
            IResourceRegistry resourceRegistry,
            IRegisterService registerService,
            IAccessListAuthorization accessListAuthorization,
            IContextHandler contextHandler,
            IDelegationContextHandler delegationContextHandler,
            IPolicyRetrievalPoint policyRetrievalPoint,
            IDelegationMetadataRepository delegationRepository,
            ILogger<DecisionController> logger,
            IMemoryCache memoryCache,
            IEventLog eventLog,
            IFeatureManager featureManager,
            IMapper mapper)
        {
            _pdp = new PolicyDecisionPoint();
            _prp = policyRetrievalPoint;
            _contextHandler = contextHandler;
            _delegationContextHandler = delegationContextHandler;
            _logger = logger;
            _memoryCache = memoryCache;
            _eventLog = eventLog;
            _featureManager = featureManager;
            _accessManagement = accessManagement;
            _resourceRegistry = resourceRegistry;
            _registerService = registerService;
            _accessListAuthorization = accessListAuthorization;
            _mapper = mapper;
        }

        /// <summary>
        /// Decision Point endpoint to authorize Xacml Context Requests
        /// </summary>
        /// <param name="model">A Generic model</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        [HttpPost]
        [Route("authorization/api/v1/decision")]
        public async Task<ActionResult> Post([FromBody] XacmlRequestApiModel model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (Request.ContentType.Contains("application/json"))
                {
                    return await AuthorizeJsonRequest(model, cancellationToken);
                }
                else
                {
                    return await AuthorizeXmlRequest(model, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "// DecisionController // Decision // Unexpected Exception");

                XacmlContextResult result = new XacmlContextResult(XacmlContextDecision.Indeterminate)
                {
                    Status = new XacmlContextStatus(XacmlContextStatusCode.SyntaxError)
                };

                XacmlContextResponse xacmlContextResponse = new XacmlContextResponse(result);

                if (Request.ContentType.Contains("application/json"))
                {
                    XacmlJsonResponse jsonResult = XacmlJsonXmlConverter.ConvertResponse(xacmlContextResponse);
                    return Ok(jsonResult);
                }
                else
                {
                    return CreateResponse(xacmlContextResponse);
                }
            }
        }

        /// <summary>
        /// External endpoint for autorization 
        /// </summary>
        [HttpPost]
        [Authorize(Policy = AuthzConstants.AUTHORIZESCOPEACCESS)]
        [Route("authorization/api/v1/authorize")]
        public async Task<ActionResult<XacmlJsonResponseExternal>> AuthorizeExternal([FromBody] XacmlJsonRequestRootExternal authorizationRequest, CancellationToken cancellationToken = default)
        {
            try
            {
                XacmlJsonRequestRoot jsonRequest = _mapper.Map<XacmlJsonRequestRoot>(authorizationRequest);
                XacmlJsonResponse xacmlResponse = await Authorize(jsonRequest.Request, true, cancellationToken);
                return _mapper.Map<XacmlJsonResponseExternal>(xacmlResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "// DecisionController // External Decision // Unexpected Exception");

                XacmlContextStatus status = new XacmlContextStatus(XacmlContextStatusCode.SyntaxError);
                if (ex is ArgumentException)
                {
                    status = new XacmlContextStatus(XacmlContextStatusCode.ProcessingError);
                    status.StatusMessage = ex.Message;
                }

                XacmlContextResult result = new XacmlContextResult(XacmlContextDecision.Indeterminate)
                {
                    Status = status
                };

                XacmlContextResponse xacmlContextResponse = new XacmlContextResponse(result);
                XacmlJsonResponse jsonResult = XacmlJsonXmlConverter.ConvertResponse(xacmlContextResponse);
                return _mapper.Map<XacmlJsonResponseExternal>(jsonResult);
            }
        }

        private async Task<XacmlJsonResponse> Authorize(XacmlJsonRequest decisionRequest, bool isExternalRequest = false, CancellationToken cancellationToken = default)
        {
            bool logEvent = true;
            if (decisionRequest.MultiRequests == null || decisionRequest.MultiRequests.RequestReference == null
                || decisionRequest.MultiRequests.RequestReference.Count < 2)
            {
                XacmlContextRequest request = XacmlJsonXmlConverter.ConvertRequest(decisionRequest);
                XacmlContextResponse xmlResponse = await Authorize(request, isExternalRequest, logEvent, cancellationToken);
                return XacmlJsonXmlConverter.ConvertResponse(xmlResponse);
            }
            else
            {
                SortedList<string, XacmlJsonCategory> sortedResources = new();
                foreach (var resource in decisionRequest.Resource)
                {
                    sortedResources[resource.Id] = resource;
                }

                logEvent = false;
                XacmlJsonResponse multiResponse = new XacmlJsonResponse();
                foreach (XacmlJsonRequestReference xacmlJsonRequestReference in decisionRequest.MultiRequests.RequestReference)
                {
                    XacmlJsonRequest jsonMultiRequestPart = new XacmlJsonRequest();

                    foreach (string refer in xacmlJsonRequestReference.ReferenceId)
                    {
                        if (sortedResources.TryGetValue(refer, out XacmlJsonCategory resourceCategory))
                        {
                            if (jsonMultiRequestPart.Resource == null)
                            {
                                jsonMultiRequestPart.Resource = [resourceCategory];
                            }
                            else
                            {
                                jsonMultiRequestPart.Resource.Add(resourceCategory);
                            }
                        }

                        IEnumerable<XacmlJsonCategory> subjectCategoriesPart = decisionRequest.AccessSubject.Where(i => i.Id.Equals(refer));

                        if (subjectCategoriesPart != null && subjectCategoriesPart.Any())
                        {
                            if (jsonMultiRequestPart.AccessSubject == null)
                            {
                                jsonMultiRequestPart.AccessSubject = new List<XacmlJsonCategory>();
                            }

                            jsonMultiRequestPart.AccessSubject.AddRange(subjectCategoriesPart);
                        }

                        IEnumerable<XacmlJsonCategory> actionCategoriesPart = decisionRequest.Action.Where(i => i.Id.Equals(refer));

                        if (actionCategoriesPart != null && actionCategoriesPart.Any())
                        {
                            if (jsonMultiRequestPart.Action == null)
                            {
                                jsonMultiRequestPart.Action = new List<XacmlJsonCategory>();
                            }

                            jsonMultiRequestPart.Action.AddRange(actionCategoriesPart);
                        }
                    }

                    XacmlContextResponse partResponse = await Authorize(XacmlJsonXmlConverter.ConvertRequest(jsonMultiRequestPart), isExternalRequest, logEvent, cancellationToken);
                    XacmlJsonResponse xacmlJsonResponsePart = XacmlJsonXmlConverter.ConvertResponse(partResponse);

                    if (multiResponse.Response == null)
                    {
                        multiResponse.Response = new List<XacmlJsonResult>();
                    }

                    multiResponse.Response.Add(xacmlJsonResponsePart.Response[0]);
                }

                return multiResponse;
            }
        }

        private async Task<ActionResult> AuthorizeXmlRequest(XacmlRequestApiModel model, CancellationToken cancellationToken = default)
        {
            XacmlContextRequest request;
            using (XmlReader reader = XmlReader.Create(new StringReader(model.BodyContent)))
            {
                request = XacmlParser.ReadContextRequest(reader);
            }

            XacmlContextResponse xacmlContextResponse = await Authorize(request, false, true, cancellationToken);

            return CreateResponse(xacmlContextResponse);
        }

        private async Task<ActionResult> AuthorizeJsonRequest(XacmlRequestApiModel model, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            XacmlJsonRequestRoot jsonRequest = JsonSerializer.Deserialize<XacmlJsonRequestRoot>(model.BodyContent, options);

            XacmlJsonResponse jsonResponse = await Authorize(jsonRequest.Request, cancellationToken: cancellationToken);

            return Ok(jsonResponse);
        }

        private ActionResult CreateResponse(XacmlContextResponse xacmlContextResponse)
        {
            StringBuilder builder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(builder))
            {
                XacmlSerializer.WriteContextResponse(writer, xacmlContextResponse);
            }

            string xml = builder.ToString();

            return Content(xml);
        }

        private async Task<XacmlContextResponse> Authorize(XacmlContextRequest decisionRequest, bool isExernalRequest, bool logEvent = true, CancellationToken cancellationToken = default)
        {
            decisionRequest = await this._contextHandler.Enrich(decisionRequest, isExernalRequest, _appInstanceInfo);

            XacmlPolicy policy = await _prp.GetPolicyAsync(decisionRequest);

            XacmlContextResponse rolesContextResponse = _pdp.Authorize(decisionRequest, policy);
            XacmlContextResult roleResult = rolesContextResponse.Results.First();

            XacmlContextResponse delegationContextResponse = null;
            if (roleResult.Decision.Equals(XacmlContextDecision.NotApplicable))
            {
                try
                {
                    delegationContextResponse = await AuthorizeUsingDelegations(decisionRequest, policy, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "// DecisionController // Authorize // Delegation // Unexpected Exception");
                }
            }

            XacmlContextResponse finalResponse = delegationContextResponse ?? rolesContextResponse;
            XacmlContextResult finalResult = finalResponse.Results.First();
            if (finalResult.Decision.Equals(XacmlContextDecision.Permit) && !await IsAccessListAuthorized(decisionRequest, cancellationToken))
            {
                return new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Deny)
                {
                    Status = new XacmlContextStatus(XacmlContextStatusCode.Success)
                    {
                        StatusMessage = "Access list authorization of resource party required. Access list authorization is controlled by the service owner of the resource/service of the authorization request.",
                    }
                });
            }

            // If the delegation context response is NOT permit, the final response should be the roles context response
            finalResponse = finalResult.Decision.Equals(XacmlContextDecision.Permit) ? finalResponse : rolesContextResponse;
            if (logEvent)
            {
                await _eventLog.CreateAuthorizationEvent(_featureManager, decisionRequest, HttpContext, finalResponse, cancellationToken);
            }

            return finalResponse;
        }

        private async Task<bool> IsAccessListAuthorized(XacmlContextRequest decisionRequest, CancellationToken cancellationToken = default)
        {
            PolicyResourceType policyResourceType = PolicyHelper.GetPolicyResourceType(decisionRequest, out string resourceId, out _, out _);
            if (!policyResourceType.Equals(PolicyResourceType.ResourceRegistry))
            {
                return true;
            }

            ServiceResource resource = await _resourceRegistry.GetResourceAsync(resourceId, cancellationToken);
            if (resource != null && resource.AccessListMode == ResourceAccessListMode.Enabled)
            {
                var resourceAttributes = _delegationContextHandler.GetResourceAttributes(decisionRequest);

                Party party = await _registerService.GetParty(int.Parse(resourceAttributes.ResourcePartyValue), cancellationToken);
                if (party?.PartyTypeName != Register.Enums.PartyType.Organisation)
                {
                    // Currently only Organization support in AccessLists
                    return false;
                }

                resourceAttributes.OrganizationNumber = party.OrgNumber;
                AccessListAuthorizationRequest accessListAuthorizationRequest = new AccessListAuthorizationRequest
                {
                    Subject = PartyUrn.OrganizationIdentifier.Create(OrganizationNumber.CreateUnchecked(resourceAttributes.OrganizationNumber)),
                    Resource = ResourceIdUrn.ResourceId.Create(Altinn.Authorization.Models.ResourceRegistry.ResourceIdentifier.CreateUnchecked(resourceId)),
                    Action = ActionUrn.ActionId.Create(ActionIdentifier.CreateUnchecked(_delegationContextHandler.GetActionString(decisionRequest)))
                };
                Result<AccessListAuthorizationResponse> result = await _accessListAuthorization.Authorize(accessListAuthorizationRequest, cancellationToken);

                if (result.IsProblem)
                {
                    return false;
                }

                return result.Value.Result == AccessListAuthorizationResult.Authorized;
            }

            return true;
        }

        private static string CreateCacheKey(params string[] cacheKeys) =>
            string.Join("-", cacheKeys.Where(c => c != null && (c != string.Empty || !c.EndsWith(':'))));

        private static bool IsTypeApp(XacmlResourceAttributes resourceAttributes) =>
            !string.IsNullOrEmpty(resourceAttributes.OrgValue) && !string.IsNullOrEmpty(resourceAttributes.AppValue);

        private static bool IsTypeResource(XacmlResourceAttributes resourceAttributes) =>
            !string.IsNullOrEmpty(resourceAttributes.ResourceRegistryId);

        private bool IsIncompleteRequestForDelegation(XacmlResourceAttributes resourceAttributes, XacmlContextRequest decisionRequest) =>
            resourceAttributes == null ||
            (_delegationContextHandler.GetSubjectAttributeMatch(decisionRequest, [XacmlRequestAttribute.UserAttribute, XacmlRequestAttribute.PartyAttribute, XacmlRequestAttribute.SystemUserIdAttribute]) == null) ||
            !int.TryParse(resourceAttributes.ResourcePartyValue, out var _) ||
            !(IsTypeApp(resourceAttributes) || IsTypeResource(resourceAttributes));

        private Action<DelegationChangeInput> WithDefaultGetAllDelegationChangesInput(XacmlResourceAttributes resourceAttributes, XacmlContextRequest decisionRequest) => (input) =>
        {
            input.Subject = _delegationContextHandler.GetSubjectAttributeMatch(decisionRequest, [XacmlRequestAttribute.UserAttribute, XacmlRequestAttribute.PartyAttribute, XacmlRequestAttribute.SystemUserIdAttribute]);
            input.Party = new(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, resourceAttributes.ResourcePartyValue);
        };

        private async Task<XacmlContextResponse> AuthorizeUsingDelegations(XacmlContextRequest decisionRequest, XacmlPolicy resourcePolicy, CancellationToken cancellationToken = default)
        {
            var resourceAttributes = _delegationContextHandler.GetResourceAttributes(decisionRequest);

            if (IsIncompleteRequestForDelegation(resourceAttributes, decisionRequest))
            {
                return new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Indeterminate)
                {
                    Status = new XacmlContextStatus(XacmlContextStatusCode.MissingAttribute) { StatusMessage = "Request not complete for authorization based on delegations." },
                });
            }

            // Look up delegations from (cached) AccessManagement PIP API
            IEnumerable<DelegationChangeExternal> delegations = new List<DelegationChangeExternal>();
            if (IsTypeApp(resourceAttributes))
            {
                delegations = await GetAllCachedDelegationChanges(cancellationToken, WithDefaultGetAllDelegationChangesInput(resourceAttributes, decisionRequest), input => input.Resource = new List<AttributeMatch>()
                {
                    new(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, resourceAttributes.OrgValue),
                    new(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, resourceAttributes.AppValue),
                });
            }

            if (IsTypeResource(resourceAttributes))
            {
                delegations = await GetAllCachedDelegationChanges(cancellationToken, WithDefaultGetAllDelegationChangesInput(resourceAttributes, decisionRequest), input => input.Resource = new List<AttributeMatch>()
                {
                    new(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistry, resourceAttributes.ResourceRegistryId)
                });
            }

            bool isInstanceAccessRequest = false;
            if (!string.IsNullOrEmpty(resourceAttributes.ResourceInstanceValue))
            {
                // If request has an instance id, only non-instance delegations or instance delegations with the same instance id should be considered
                delegations = delegations.Where(d => d.InstanceId == null || d.InstanceId == resourceAttributes.ResourceInstanceValue);
                isInstanceAccessRequest = true;
            }
            else
            {
                // If request does not have an instance id, only non-instance delegations should be considered
                delegations = delegations.Where(d => string.IsNullOrWhiteSpace(d.InstanceId));
            }

            if (!delegations.Any())
            {
                return new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.NotApplicable)
                {
                    Status = new XacmlContextStatus(XacmlContextStatusCode.Success)
                });
            }

            XacmlContextAttributes subjectContextAttributes = decisionRequest.GetSubjectAttributes();
            XacmlContextAttributes resourceContextAttributes = decisionRequest.GetResourceAttributes();
            await _delegationContextHandler.EnrichRequestSubjectAttributes(subjectContextAttributes, isInstanceAccessRequest, cancellationToken);
            _delegationContextHandler.EnrichRequestResourceAttributes(resourceContextAttributes, resourceAttributes, isInstanceAccessRequest);

            return await ProcessDelegationResult(decisionRequest, delegations, resourcePolicy, cancellationToken);
        }

        private async Task<IEnumerable<DelegationChangeExternal>> GetAllCachedDelegationChanges(CancellationToken cancellationToken = default, params Action<DelegationChangeInput>[] actions)
        {
            var delegation = new DelegationChangeInput();
            foreach (var action in actions)
            {
                action(delegation);
            }

            var cacheKey = CreateCacheKey(
                $"s:{delegation.Subject.Id}:{delegation.Subject.Value}",
                $"p:{delegation.Party.Value}",
                $"a:{delegation.Resource.FirstOrDefault(r => r.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute)?.Value}/{delegation.Resource.FirstOrDefault(r => r.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute)?.Value}",
                $"r:{delegation.Resource.FirstOrDefault(r => r.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistry)?.Value}");

            if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<DelegationChangeExternal> result))
            {
                result = await _accessManagement.GetAllDelegationChanges(cancellationToken, actions);
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.High)
                    .SetAbsoluteExpiration(new TimeSpan(0, 0, 5, 0));

                _memoryCache.Set(cacheKey, result, cacheEntryOptions);
            }

            return result;
        }

        private async Task<XacmlContextResponse> ProcessDelegationResult(XacmlContextRequest decisionRequest, IEnumerable<DelegationChangeExternal> delegations, XacmlPolicy resourcePolicy, CancellationToken cancellationToken = default)
        {
            foreach (DelegationChangeExternal delegation in delegations.Where(d => d.DelegationChangeType != DelegationChangeType.RevokeLast))
            {
                XacmlPolicy delegationPolicy = await _prp.GetPolicyVersionAsync(delegation.BlobStoragePolicyPath, delegation.BlobStorageVersionId, cancellationToken);
                if (delegationPolicy.ObligationExpressions.Count == 0)
                {
                    foreach (XacmlObligationExpression obligationExpression in resourcePolicy.ObligationExpressions)
                    {
                        delegationPolicy.ObligationExpressions.Add(obligationExpression);
                    }
                }

                XacmlContextResponse delegationContextResponse = _pdp.Authorize(decisionRequest, delegationPolicy);
                if (delegationContextResponse.Results.Any(r => r.Decision == XacmlContextDecision.Permit))
                {
                    return delegationContextResponse;
                }
            }

            return new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.NotApplicable)
            {
                Status = new XacmlContextStatus(XacmlContextStatusCode.Success)
            });
        }
    }
}

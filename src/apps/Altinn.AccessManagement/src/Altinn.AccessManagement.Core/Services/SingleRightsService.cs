using Altinn.AccessManagement.Core.Asserters;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Enums.Profile;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.AccessList;
using Altinn.AccessManagement.Core.Models.Authentication;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.Rights;
using Altinn.AccessManagement.Core.Resolvers;
using Altinn.AccessManagement.Core.Resolvers.Extensions;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Core.Services
{
    /// <inheritdoc/>
    public class SingleRightsService : ISingleRightsService
    {
        private readonly IContextRetrievalService _contextRetrievalService;
        private readonly IPolicyInformationPoint _pip;
        private readonly IPolicyAdministrationPoint _pap;
        private readonly IAltinn2RightsClient _altinn2RightsClient;
        private readonly IAccessListsAuthorizationClient _accessListsAuthorizationClient;
        private readonly IProfileClient _profile;
        private readonly IUserProfileLookupService _profileLookup;
        private readonly IAttributeResolver _resolver;
        private readonly IAssert<AttributeMatch> _asserter;
        private readonly IAMPartyService _aMParty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleRightsService"/> class.
        /// </summary>
        /// <param name="resolver">a</param>
        /// <param name="asserter">b</param>
        /// <param name="contextRetrievalService">Service for retrieving context information</param>
        /// <param name="pip">Service implementation for policy information point</param>
        /// <param name="pap">Service implementation for policy administration point</param>
        /// <param name="altinn2RightsClient">SBL Bridge client implementation for rights operations on Altinn 2 services</param>
        /// <param name="accessListsAuthorizationClient">A client for access list authorization actions.</param>
        /// <param name="profile">Client implementation for getting user profile</param>
        /// <param name="profileLookup">Service implementation for lookup of userprofile with lastname verification</param>
        /// <param name="AMParty">AMPartyService</param>
        public SingleRightsService(IAttributeResolver resolver, IAssert<AttributeMatch> asserter, IContextRetrievalService contextRetrievalService, IPolicyInformationPoint pip, IPolicyAdministrationPoint pap, IAltinn2RightsClient altinn2RightsClient, IAccessListsAuthorizationClient accessListsAuthorizationClient, IProfileClient profile, IUserProfileLookupService profileLookup, IAMPartyService AMParty)
        {
            _resolver = resolver;
            _asserter = asserter;
            _contextRetrievalService = contextRetrievalService;
            _pip = pip;
            _pap = pap;
            _altinn2RightsClient = altinn2RightsClient;
            _accessListsAuthorizationClient = accessListsAuthorizationClient;
            _profile = profile;
            _profileLookup = profileLookup;
            _aMParty = AMParty;
        }

        /// <inheritdoc/>
        public async Task<DelegationCheckResponse> RightsDelegationCheck(int authenticatedUserId, int authenticatedUserAuthlevel, RightsDelegationCheckRequest request)
        {
            (DelegationCheckResponse result, ServiceResource resource, Party fromParty) = await ValidateRightDelegationCheckRequest(request);
            if (!result.IsValid)
            {
                return result;
            }

            RightsQuery rightsQuery;
            DelegationHelper.TryGetResourceFromAttributeMatch(request.Resource, out ResourceAttributeMatchType resourceMatchType, out string resourceRegistryId, out string org, out string app, out string serviceCode, out string serviceEditionCode);
            if (resource.ResourceType == Models.ResourceRegistry.ResourceType.Altinn2Service)
            {
                return await _altinn2RightsClient.PostDelegationCheck(authenticatedUserId, fromParty.PartyId, serviceCode, serviceEditionCode);
            }

            rightsQuery = RightsHelper.GetRightsQuery(authenticatedUserId, fromParty.PartyId, resource);

            List<Right> allDelegableRights = await _pip.GetRights(rightsQuery, getDelegableRights: true, returnAllPolicyRights: true);
            if (allDelegableRights == null || allDelegableRights.Count == 0)
            {
                result.Errors.Add("right[0].Resource", $"No delegable rights could be found for the resource: {resource}");
                return result;
            }

            // Build result model with status
            foreach (Right right in allDelegableRights)
            {
                if (!RightsHelper.CheckIfRuleIsAnEndUserRule(right))
                {
                    continue;
                }

                AccessListAuthorizationResult accessListAuthorizationResult = AccessListAuthorizationResult.NotApplicable;

                if (DelegationHelper.IsAccessListModeEnabledAndApplicable(right, resource, fromParty))
                {
                    AccessListAuthorizationRequest accessListAuthorizationRequest = new AccessListAuthorizationRequest
                    {
                        Subject = PartyUrn.OrganizationIdentifier.Create(OrganizationNumber.CreateUnchecked(fromParty.OrgNumber)),
                        Resource = ResourceIdUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(resource.Identifier)),
                        Action = ActionUrn.ActionId.Create(ActionIdentifier.CreateUnchecked(right.Action.Value))
                    };

                    AccessListAuthorizationResponse accessListAuthorizationResponse = await _accessListsAuthorizationClient.AuthorizePartyForAccessList(accessListAuthorizationRequest);
                    accessListAuthorizationResult = accessListAuthorizationResponse.Result;
                }

                RightDelegationCheckResult rightDelegationStatus = new()
                {
                    RightKey = right.RightKey,
                    Resource = right.Resource,
                    Action = right.Action,
                    Details = RightsHelper.AnalyzeDelegationAccessReason(right, accessListAuthorizationResult)
                };

                rightDelegationStatus.Status = (right.CanDelegate.HasValue && right.CanDelegate.Value) ? DelegableStatus.Delegable : DelegableStatus.NotDelegable;

                result.RightDelegationCheckResults.Add(rightDelegationStatus);
            }

            return result;
        }

        public async Task<List<Rule>> EnrichAndTryWriteDelegationPolicyRules(List<Rule> rules, CancellationToken cancellationToken)
        {
            rules = await EnrichRulesWithUuidInformation(rules, cancellationToken);
            return await _pap.TryWriteDelegationPolicyRules(rules, cancellationToken);
        }

        public async Task<List<Rule>> TryWriteDelegationPolicyRules(Entity from, Entity to, Resource resource, List<string> actionIds, Entity performedBy, CancellationToken cancellationToken)
        {
            var rules = GenerateRules(from, to, resource, actionIds, performedBy).ToList();
            return await _pap.TryWriteDelegationPolicyRules(rules, cancellationToken);
        }

        private IEnumerable<Rule> GenerateRules(Entity from, Entity to, Resource resource, List<string> actionIds, Entity performedBy)
        {
            var coveredBy = to;
            var offeredBy = from;

            var coveredByUuidType = DelegationHelper.GetUuidTypeFromEntityType(coveredBy.TypeId);
            var offeredByUuidType = DelegationHelper.GetUuidTypeFromEntityType(offeredBy.TypeId);
            var performedByUuidType = DelegationHelper.GetUuidTypeFromEntityType(performedBy.TypeId);

            var result = actionIds.Select(action =>
                new Rule
                {
                    RuleId = Guid.NewGuid().ToString(),
                    Type = RuleType.None,

                    CoveredBy = ConvertEntityToAttributeMatch(coveredBy),
                    Resource = ConvertResourceToAttributeMatches(resource),
                    Action = ConvertActionToAttributeMatch(action),

                    OfferedByPartyId = offeredBy.PartyId.HasValue ? offeredBy.PartyId.Value : 0,
                    OfferedByPartyUuid = offeredBy.Id,
                    OfferedByPartyType = offeredByUuidType,

                    PerformedBy = ConvertEntityToAttributeMatch(performedBy),
                    DelegatedByUserId = performedBy.UserId,
                    DelegatedByPartyId = performedBy.PartyId,
                });

            return result;
        }

        private static AttributeMatch ConvertActionToAttributeMatch(string action)
        {
            return new AttributeMatch
            {
                Id = AltinnXacmlConstants.MatchAttributeIdentifiers.ActionId,
                Value = action
            };
        }

        private static List<AttributeMatch> ConvertEntityToAttributeMatch(Entity entity)
        {
            var matches = new List<AttributeMatch>();

            // UUID
            var uuidType = DelegationHelper.GetUuidTypeFromEntityType(entity.TypeId);
            matches.Add(new AttributeMatch
            {
                Id = uuidType.EnumMemberAttributeValueOrName(),
                Value = entity.Id.ToString()
            });

            // User
            if (!matches.Any() && entity.UserId.HasValue)
            {
                matches.Add(new AttributeMatch
                {
                    Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute,
                    Value = entity.UserId.Value.ToString()
                });
            }

            // Party
            if (!matches.Any() && entity.PartyId.HasValue)
            {
                matches.Add(new AttributeMatch
                {
                    Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute,
                    Value = entity.PartyId.Value.ToString()
                });
            }

            return matches;
        }

        private static List<AttributeMatch> ConvertResourceToAttributeMatches(Resource resource)
        {
            /*
            |GenericAccessResource| ResourceRegistry | urn:altinn:resource
            |Systemresource       | ResourceRegistry | urn:altinn:resource
            |MaskinportenSchema   | ResourceRegistry | urn:altinn:resource
            |CorrespondenceService| ResourceRegistry | urn:altinn:resource
            |Consent              | ResourceRegistry | urn:altinn:resource 
            |AltinnApp            | AltinnApp  | urn:altinn:org + urn:altinn:app
            |AltinnApp            | AltinnApp  | urn:altinn:resource with value/resourceId app_{orgcode}_{appname}
            dbo.Resource.RefId : app_ttd_signering-brukerstyrt
            */

            switch (resource.Type.Name)
            {
                default:
                case "GenericAccessResource":
                case "Systemresource":
                case "MaskinportenSchema":
                case "CorrespondenceService":
                case "Consent":
                    return
                    [
                        new AttributeMatch
                        {
                            Id = AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute,
                            Value = resource.RefId
                        }
                    ];
                case "AltinnApp":
                    return
                    [
                        new AttributeMatch
                        {
                            Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute,
                            Value = resource.Provider.Code
                        },
                        new AttributeMatch
                        {
                            // TODO: Verify
                            Id = AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute,
                            Value = resource.RefId.Replace("app_", string.Empty).Replace('_', '/')
                        }
                    ];
            }
        }

        private async Task<List<Rule>> EnrichRulesWithUuidInformation(List<Rule> rules, CancellationToken cancellationToken)
        {
            Dictionary<int, (UuidType, Guid)> partyIds = [];
            Dictionary<int, (UuidType, Guid)> userIds = [];
            (UuidType Type, Guid Uuid) partyIdentifier = (UuidType.NotSpecified, Guid.Empty);

            foreach (Rule rule in rules)
            {
                AttributeMatch coveredByUuid = null;
                int partyId = 0;
                int userId = 0;

                // Handle CoveredBy/To
                foreach (AttributeMatch item in rule.CoveredBy)
                {
                    switch (item.Id)
                    {
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute:
                            int.TryParse(item.Value, out partyId);
                            break;
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute:
                            int.TryParse(item.Value, out userId);
                            break;
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid:
                            coveredByUuid = item;
                            break;
                        default:
                            break;
                    }
                }

                if (userId != 0)
                {
                    if (userIds.ContainsKey(userId))
                    {
                        partyIdentifier = userIds[userId];
                    }
                    else
                    {
                        MinimalParty userProfile = await _aMParty.GetByUserId(userId, cancellationToken);

                        if (userProfile != null)
                        {
                            partyIdentifier = DelegationHelper.GetUserUuidFromUserProfile(userProfile);
                            userIds[userId] = partyIdentifier;
                        }
                        else
                        {
                            partyIdentifier = (UuidType.NotSpecified, Guid.Empty);
                        }
                    }
                }
                else if (partyId != 0)
                {
                    if (partyIds.ContainsKey(partyId))
                    {
                        partyIdentifier = partyIds[partyId];
                    }
                    else
                    {
                        MinimalParty toParty = await _aMParty.GetByPartyId(partyId, cancellationToken);
                        if (toParty != null)
                        {
                            partyIdentifier = (DelegationHelper.GetUuidTypeFromPartyType(toParty.PartyType), toParty.PartyUuid);
                            partyIds[partyId] = partyIdentifier;
                        }
                        else
                        {
                            partyIdentifier = (UuidType.NotSpecified, Guid.Empty);
                        }
                    }
                }
                else
                {
                    // Should not be posble as partyid or userid must be set
                    partyIdentifier = (UuidType.NotSpecified, Guid.Empty);
                }

                if (partyIdentifier.Type != UuidType.NotSpecified)
                { 
                    if (coveredByUuid != null)
                    {
                        coveredByUuid.Id = partyIdentifier.Type.EnumMemberAttributeValueOrName();
                        coveredByUuid.Value = partyIdentifier.Uuid.ToString(); // The offered by uuid
                    }
                    else
                    {
                        rule.CoveredBy.Add(new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() });
                    }
                }

                // Handle PerformedBy
                if (rule.DelegatedByUserId.HasValue)
                {
                    if (userIds.ContainsKey(rule.DelegatedByUserId.Value))
                    {
                        partyIdentifier = userIds[rule.DelegatedByUserId.Value];
                        rule.PerformedBy = [new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() }, new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = rule.DelegatedByUserId.Value.ToString() }];
                    }
                    else
                    {
                        MinimalParty userProfile = await _aMParty.GetByUserId(rule.DelegatedByUserId.Value);
                        if (userProfile != null)
                        {
                            partyIdentifier = DelegationHelper.GetUserUuidFromUserProfile(userProfile);
                            userIds[rule.DelegatedByUserId.Value] = partyIdentifier;
                            rule.PerformedBy = [new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() }, new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = rule.DelegatedByUserId.Value.ToString() }];
                        }
                        else
                        {
                            rule.PerformedBy = [new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = rule.DelegatedByUserId.Value.ToString() }];
                        }
                    }
                }
                else if (rule.DelegatedByPartyId.HasValue)
                {
                    if (partyIds.ContainsKey(rule.DelegatedByPartyId.Value))
                    {
                        partyIdentifier = partyIds[rule.DelegatedByPartyId.Value];
                        rule.PerformedBy = [new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() }, new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = rule.DelegatedByPartyId.Value.ToString() }];
                    }
                    else
                    {
                        MinimalParty performedByParty = await _aMParty.GetByPartyId(rule.DelegatedByPartyId.Value, cancellationToken);
                        if (performedByParty != null)
                        {
                            partyIdentifier = (DelegationHelper.GetUuidTypeFromPartyType(performedByParty.PartyType), performedByParty.PartyUuid);
                            partyIds[rule.DelegatedByPartyId.Value] = partyIdentifier;
                            rule.PerformedBy = [new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() }, new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = rule.DelegatedByPartyId.Value.ToString() }];
                        }
                        else
                        {
                            rule.PerformedBy = [new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = rule.DelegatedByPartyId.Value.ToString() }];
                        }
                    }                    
                }

                // Handle OfferedBy/From
                if (partyIds.ContainsKey(rule.OfferedByPartyId))
                {
                    partyIdentifier = partyIds[rule.OfferedByPartyId];
                    rule.OfferedByPartyType = partyIdentifier.Type;
                    rule.OfferedByPartyUuid = partyIdentifier.Uuid;
                }
                else
                {
                    MinimalParty fromParty = await _aMParty.GetByPartyId(rule.OfferedByPartyId, cancellationToken);
                    if (fromParty != null)
                    {
                        partyIdentifier = (DelegationHelper.GetUuidTypeFromPartyType(fromParty.PartyType), fromParty.PartyUuid);
                        partyIds[rule.OfferedByPartyId] = partyIdentifier;
                        rule.OfferedByPartyType = partyIdentifier.Type;
                        rule.OfferedByPartyUuid = partyIdentifier.Uuid;
                    }    
                }                                
            }

            return rules;
        }
        
        /// <inheritdoc/>
        public async Task<List<Rule>> EnrichAndTryDeleteDelegationPolicyRules(List<RequestToDelete> rulesToDelete, CancellationToken cancellationToken = default)
        {
            rulesToDelete = await EnrichDeleteRequest(rulesToDelete, cancellationToken);
            return await _pap.TryDeleteDelegationPolicyRules(rulesToDelete, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Rule>> EnrichAndTryDeleteDelegationPolicies(List<RequestToDelete> policiesToDelete, CancellationToken cancellationToken = default)
        {
            policiesToDelete = await EnrichDeleteRequest(policiesToDelete, cancellationToken);
            return await _pap.TryDeleteDelegationPolicies(policiesToDelete, cancellationToken);
        }

        private async Task<List<RequestToDelete>> EnrichDeleteRequest(List<RequestToDelete> requestToDelete, CancellationToken cancellationToken = default)
        {
            Dictionary<int, (UuidType, Guid)> userIds = [];
            Dictionary<int, (UuidType, Guid)> partyIds = [];
            (UuidType Type, Guid Uuid) partyIdentifier = (UuidType.NotSpecified, Guid.Empty);

            AttributeMatch coveredByUuid = null;
            int partyId = 0;
            int userId = 0;

            foreach (RequestToDelete request in requestToDelete)
            {
                foreach (AttributeMatch item in request.PolicyMatch.CoveredBy)
                {
                    switch (item.Id)
                    {
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute:
                            int.TryParse(item.Value, out partyId);
                            break;
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute:
                            int.TryParse(item.Value, out userId);
                            break;
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid:
                        case AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid:
                            coveredByUuid = item;
                            break;
                        default:
                            break;
                    }
                }

                if (userId != 0)
                {
                    if (userIds.ContainsKey(userId))
                    {
                        partyIdentifier = userIds[userId];
                    }
                    else
                    {
                        MinimalParty userProfile = await _aMParty.GetByUserId(userId, cancellationToken);
                        if (userProfile != null)
                        {
                            partyIdentifier = DelegationHelper.GetUserUuidFromUserProfile(userProfile);
                            userIds[userId] = partyIdentifier;                            
                        }
                        else
                        {
                            partyIdentifier = (UuidType.NotSpecified, Guid.Empty);
                        }
                    }
                }
                else if (partyId != 0)
                {
                    if (partyIds.ContainsKey(partyId))
                    {
                        partyIdentifier = partyIds[partyId];
                    }
                    else
                    {
                        MinimalParty toParty = await _aMParty.GetByPartyId(partyId);
                        if (toParty != null)
                        {
                            partyIdentifier = (DelegationHelper.GetUuidTypeFromPartyType(toParty.PartyType), toParty.PartyUuid);
                            partyIds[partyId] = partyIdentifier;
                        }
                        else
                        {
                            partyIdentifier = (UuidType.NotSpecified, Guid.Empty);
                        }
                    }
                }
                else
                {
                    // Should not be posble as partyid or userid must be set
                    partyIdentifier = (UuidType.NotSpecified, Guid.Empty);
                }

                if (partyIdentifier.Type != UuidType.NotSpecified)
                {                
                    if (coveredByUuid != null)
                    {
                        coveredByUuid.Id = partyIdentifier.Type.EnumMemberAttributeValueOrName();
                        coveredByUuid.Value = partyIdentifier.Uuid.ToString(); // The offered by uuid
                    }
                    else
                    {
                        request.PolicyMatch.CoveredBy.Add(new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() });
                    }
                }

                if (userIds.ContainsKey(request.DeletedByUserId))
                {
                    partyIdentifier = userIds[request.DeletedByUserId];
                    request.PerformedBy = [new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() }, new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = request.DeletedByUserId.ToString() }];
                }
                else
                {
                    MinimalParty userProfile = await _aMParty.GetByUserId(request.DeletedByUserId, cancellationToken);
                    if (userProfile != null)
                    {
                        partyIdentifier = DelegationHelper.GetUserUuidFromUserProfile(userProfile);
                        userIds[request.DeletedByUserId] = partyIdentifier;
                        request.PerformedBy = [new AttributeMatch { Id = partyIdentifier.Type.EnumMemberAttributeValueOrName(), Value = partyIdentifier.Uuid.ToString() }, new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = request.DeletedByUserId.ToString() }];
                    }
                    else
                    {
                        request.PerformedBy = [new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = request.DeletedByUserId.ToString() }];
                    }                    
                }                

                // Handle OfferedBy/From
                if (partyIds.ContainsKey(request.PolicyMatch.OfferedByPartyId))
                {
                    partyIdentifier = partyIds[request.PolicyMatch.OfferedByPartyId];
                    request.PolicyMatch.FromUuidType = partyIdentifier.Type;
                    request.PolicyMatch.FromUuid = partyIdentifier.Uuid;
                }
                else
                {
                    MinimalParty fromParty = await _aMParty.GetByPartyId(request.PolicyMatch.OfferedByPartyId, cancellationToken);
                    if (fromParty != null)
                    {
                        partyIdentifier = (DelegationHelper.GetUuidTypeFromPartyType(fromParty.PartyType), fromParty.PartyUuid);
                        partyIds[request.PolicyMatch.OfferedByPartyId] = partyIdentifier;
                        request.PolicyMatch.FromUuidType = partyIdentifier.Type;
                        request.PolicyMatch.FromUuid = partyIdentifier.Uuid;
                    }
                }                
            }

            return requestToDelete;
        }

        /// <inheritdoc/>
        public async Task<DelegationActionResult> DelegateRights(int authenticatedUserId, Guid authenticatedUserPartyUuid, int authenticatedUserAuthlevel, DelegationLookup delegation, CancellationToken cancellationToken = default)
        {
            (DelegationActionResult result, ServiceResource resource, Party fromParty, List<AttributeMatch> to) = await ValidateDelegationLookupModel(DelegationActionType.Delegation, delegation, authenticatedUserId);
            if (!result.IsValid)
            {
                return result;
            }

            // Altinn 2 service delegation is handled by SBL Bridge
            if (resource.ResourceType == Models.ResourceRegistry.ResourceType.Altinn2Service)
            {
                SblRightDelegationRequest sblRightDelegationRequest = new SblRightDelegationRequest { To = to.FirstOrDefault(), Rights = delegation.Rights };
                DelegationActionResult sblResult = await _altinn2RightsClient.PostDelegation(authenticatedUserId, fromParty.PartyId, sblRightDelegationRequest);

                sblResult.To = result.To; // Set result.To to match original input
                return sblResult;
            }

            // Verify authenticated users delegable rights
            RightsQuery rightsQuery = RightsHelper.GetRightsQuery(authenticatedUserId, fromParty.PartyId, resource);
            List<Right> usersDelegableRights = await _pip.GetRights(rightsQuery, getDelegableRights: true, cancellationToken: cancellationToken);
            if (usersDelegableRights == null || usersDelegableRights.Count == 0)
            {
                result.Errors.Add("right[0].Resource", $"Authenticated user does not have any delegable rights for the resource: {resource.Identifier}");
                return result;
            }

            // Perform delegation
            List<Rule> rulesToDelegate = new List<Rule>();
            List<Right> rightsUserCantDelegate = new List<Right>();
            foreach (Right rightToDelegate in delegation.Rights)
            {
                Right delegableRight = usersDelegableRights.Find(r => r.RightKey == rightToDelegate.RightKey);
                if (delegableRight != null)
                {
                    // If delegable and serviceResource.AccessListMode is enabled, call accessListAuthorizationClient
                    AccessListAuthorizationResult accessListAuthorizationResult = AccessListAuthorizationResult.NotApplicable;
                    if (DelegationHelper.IsAccessListModeEnabledAndApplicable(delegableRight, resource, fromParty))
                    {
                        AccessListAuthorizationRequest accessListAuthorizationRequest = new AccessListAuthorizationRequest
                        {
                            Subject = PartyUrn.OrganizationIdentifier.Create(OrganizationNumber.CreateUnchecked(fromParty.OrgNumber)),
                            Resource = ResourceIdUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(resource.Identifier)),
                            Action = ActionUrn.ActionId.Create(ActionIdentifier.CreateUnchecked(rightToDelegate.Action.Value))
                        };

                        AccessListAuthorizationResponse accessListAuthorizationResponse = await _accessListsAuthorizationClient.AuthorizePartyForAccessList(accessListAuthorizationRequest);
                        accessListAuthorizationResult = accessListAuthorizationResponse.Result;
                    }

                    if (resource.AccessListMode == Enums.ResourceRegistry.ResourceAccessListMode.Enabled && accessListAuthorizationResult != AccessListAuthorizationResult.Authorized)
                    {
                        rightToDelegate.CanDelegate = false;
                        rightsUserCantDelegate.Add(rightToDelegate);
                    }
                    else
                    {
                        rulesToDelegate.Add(new Rule
                        {
                            DelegatedByUserId = authenticatedUserId,
                            PerformedBy = new AttributeMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute, authenticatedUserPartyUuid.ToString()).SingleToList(),
                            OfferedByPartyId = fromParty.PartyId,
                            OfferedByPartyUuid = fromParty.PartyUuid,
                            OfferedByPartyType = fromParty.Person != null ? UuidType.Person : UuidType.Organization,
                            CoveredBy = to,
                            Resource = rightToDelegate.Resource,
                            Action = rightToDelegate.Action
                        });
                    }
                }
                else
                {
                    rightsUserCantDelegate.Add(rightToDelegate);
                }
            }

            List<Rule> delegationResult = await _pap.TryWriteDelegationPolicyRules(rulesToDelegate);
            result.Rights = DelegationHelper.GetRightDelegationResultsFromRules(delegationResult);

            if (rightsUserCantDelegate.Any())
            {
                result.Rights.AddRange(DelegationHelper.GetRightDelegationResultsFromFailedRights(rightsUserCantDelegate));
            }

            return await Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<RightDelegation>> GetOfferedRights(AttributeMatch reportee, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<RightDelegation>> GetReceivedRights(AttributeMatch reportee, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ValidationProblemDetails> RevokeRightsDelegation(int authenticatedUserId, Guid authenticatedUserPartyUuid, DelegationLookup delegation, CancellationToken cancellationToken)
        {
            var assertion = AssertRevokeDelegationInput(delegation);
            if (assertion != null)
            {
                return assertion;
            }

            var fromAttribute = await _resolver.Resolve(delegation.From, [AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid, AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid], cancellationToken);
            var toAttribute = await _resolver.Resolve(delegation.To, BaseUrn.RevokeInternalToIds, cancellationToken);
                        
            var to = FilterRequiredAttributeMatchesFromAttributeMatchList(toAttribute);
            DelegationHelper.TryGetUuidFromAttributeMatch(fromAttribute.ToList(), out Guid fromUuid, out UuidType fromUuidType);

            var policiesToDelete = DelegationHelper.GetRequestToDeleteResource(authenticatedUserId, authenticatedUserPartyUuid, delegation.Rights[0].Resource, fromAttribute.GetRequiredInt(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute), to, fromUuidType, fromUuid);

            await _pap.TryDeleteDelegationPolicies(policiesToDelete, cancellationToken);
            return assertion;
        }

        /// <summary>
        /// Fetch the actual internal id from the attribute match list
        /// </summary>
        /// <param name="attributeMatches">the list to fetch from</param>
        /// <returns>The identified internal id</returns>
        private static List<AttributeMatch> FilterRequiredAttributeMatchesFromAttributeMatchList(IEnumerable<AttributeMatch> attributeMatches)
        {
            List<AttributeMatch> attributeMatchList = [];
            bool userSet = false;

            if (attributeMatches.Any(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute))
            {
                attributeMatchList.Add(new AttributeMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, attributeMatches.First(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute).Value));
                userSet = true;
            }
            
            if (attributeMatches.Any(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute) && !userSet)
            {
                attributeMatchList.Add(new AttributeMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, attributeMatches.First(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute).Value));
            }
            
            if (attributeMatches.Any(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid))
            {
                attributeMatchList.Add(new AttributeMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid, attributeMatches.First(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid).Value));
            }

            if (attributeMatches.Any(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid))
            {
                attributeMatchList.Add(new AttributeMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid, attributeMatches.First(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid).Value));
            }

            if (attributeMatches.Any(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid))
            {
                attributeMatchList.Add(new AttributeMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid, attributeMatches.First(p => p.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid).Value));
            }

            return attributeMatchList;
        }

        /// <summary>
        /// Ensures that given input for revoking a delegations contains a combination of attributes that
        /// the service layer can process. If the method return null then input should be processable.
        /// </summary>
        /// <param name="delegation">input parameters from API callee</param>
        private ValidationProblemDetails AssertRevokeDelegationInput(DelegationLookup delegation) =>
            _asserter.Join(
                _asserter.Evaluate(
                    delegation.From,
                    _asserter.RevokeInternalFromIds),
                _asserter.Evaluate(
                    delegation.To,
                    _asserter.RevokeInternalToIds),
                _asserter.Evaluate(
                    delegation.Rights?.FirstOrDefault()?.Resource ?? [],
                    _asserter.DefaultResource));

        private async Task<(DelegationCheckResponse Result, ServiceResource Resource, Party FromParty)> ValidateRightDelegationCheckRequest(RightsDelegationCheckRequest request)
        {
            DelegationCheckResponse result = new DelegationCheckResponse { From = request.From, RightDelegationCheckResults = new() };

            DelegationHelper.TryGetResourceFromAttributeMatch(request.Resource, out ResourceAttributeMatchType resourceMatchType, out string resourceRegistryId, out string org, out string app, out string serviceCode, out string serviceEditionCode);

            if (resourceMatchType == ResourceAttributeMatchType.None)
            {
                result.Errors.Add("right[0].Resource", $"The specified resource is not recognized. The operation only support requests for a single resource from either the Altinn Resource Registry identified by using the {AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute} attribute id, Altinn Apps identified by using {AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute} and {AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute}, or Altinn 2 services identified by using {AltinnXacmlConstants.MatchAttributeIdentifiers.ServiceCodeAttribute}");
                return (result, null, null);
            }

            // Verify resource is valid
            ServiceResource resource = await _contextRetrievalService.GetResourceFromResourceList(resourceRegistryId, org, app, serviceCode, serviceEditionCode);
            if (resource == null || !resource.Delegable)
            {
                result.Errors.Add("right[0].Resource", $"The resource does not exist or is not available for delegation");
                return (result, resource, null);
            }

            if (resource.ResourceType == Models.ResourceRegistry.ResourceType.MaskinportenSchema)
            {
                result.Errors.Add("right[0].Resource", $"This operation does not support MaskinportenSchema resources. Please use the MaskinportenSchema DelegationCheck API. Invalid resource: {resourceRegistryId}. Invalid resource type: {resource.ResourceType}");
                return (result, resource, null);
            }

            // Verify and get From reportee party of the delegation
            Party fromParty = null;
            if (DelegationHelper.TryGetOrganizationNumberFromAttributeMatch(request.From, out string fromOrgNo))
            {
                fromParty = await _contextRetrievalService.GetPartyForOrganization(fromOrgNo);
            }
            else if (DelegationHelper.TryGetSocialSecurityNumberAttributeMatch(request.From, out string fromSsn))
            {
                // ToDo: Can we do this based on SSN alone for the Reportee or do we need last name?
                fromParty = await _contextRetrievalService.GetPartyForPerson(fromSsn);
            }
            else if (DelegationHelper.TryGetPartyIdFromAttributeMatch(request.From, out int fromPartyId))
            {
                List<Party> fromPartyLookup = await _contextRetrievalService.GetPartiesAsync(fromPartyId.SingleToList());
                fromParty = fromPartyLookup.FirstOrDefault();
            }

            if (fromParty == null)
            {
                // This shouldn't really happen, as to get here the request must have been authorized for the From reportee, but the register integration could fail.
                result.Errors.Add("From", $"Could not identify the From party. Please try again.");
                return (result, resource, null);
            }

            return (result, resource, fromParty);
        }

        private async Task<(DelegationActionResult Result, ServiceResource Resource, Party FromParty, List<AttributeMatch> To)> ValidateDelegationLookupModel(DelegationActionType delegationAction, DelegationLookup delegation, int authenticatedUserId)
        {
            DelegationActionResult result = new DelegationActionResult { From = delegation.From, To = delegation.To, Rights = new List<RightDelegationResult>() };

            if (delegation.Rights.Count == 0)
            {
                result.Errors.Add("Rights", "Request must contain at least one right to be delegated");
                return (result, null, null, null);
            }

            // Verify request is for single resource, app or Altinn 2 service
            if (delegation.Rights.Count > 1 && !ValidateAllRightsAreForTheSameResource(delegation.Rights))
            {
                result.Errors.Add("Rights", "Rights delegation only support requests where all rights are for the same resource, app or Altinn 2 service");
                return (result, null, null, null);
            }

            DelegationHelper.TryGetResourceFromAttributeMatch(delegation.Rights[0].Resource, out ResourceAttributeMatchType _, out string resourceRegistryId, out string org, out string app, out string serviceCode, out string serviceEditionCode);

            ServiceResource resource = await _contextRetrievalService.GetResourceFromResourceList(resourceRegistryId, org, app, serviceCode, serviceEditionCode);
            if (resource == null || !resource.Delegable)
            {
                result.Errors.Add("right[0].Resource", $"The resource does not exist or is not available for delegation");
                return (result, resource, null, null);
            }

            if (resource.ResourceType == Models.ResourceRegistry.ResourceType.MaskinportenSchema)
            {
                result.Errors.Add("right[0].Resource", $"This operation does not support delegations for MaskinportenSchema resources. Please use the MaskinportenSchema Delegations API. Invalid resource: {resourceRegistryId}. Invalid resource type: {resource.ResourceType}");
                return (result, resource, null, null);
            }

            // Verify and get From reportee party of the delegation
            Party fromParty = null;
            if (DelegationHelper.TryGetOrganizationNumberFromAttributeMatch(delegation.From, out string fromOrgNo))
            {
                fromParty = await _contextRetrievalService.GetPartyForOrganization(fromOrgNo);
            }
            else if (DelegationHelper.TryGetSocialSecurityNumberAttributeMatch(delegation.From, out string fromSsn))
            {
                fromParty = await _contextRetrievalService.GetPartyForPerson(fromSsn);
            }
            else if (DelegationHelper.TryGetPartyIdFromAttributeMatch(delegation.From, out int fromPartyId))
            {
                List<Party> fromPartyLookup = await _contextRetrievalService.GetPartiesAsync(fromPartyId.SingleToList());
                fromParty = fromPartyLookup.FirstOrDefault();
            }

            if (fromParty == null)
            {
                // This shouldn't really happen, as to get here the request must have been authorized for the From reportee, but the register integration could fail.
                result.Errors.Add("From", $"Could not identify the From party. Please try again.");
                return (result, resource, null, null);
            }

            // Verify and get To recipient party of the delegation
            Party toParty = null;
            NewUserProfile toUser = null;
            SystemUser toSystemUser = null;

            if (DelegationHelper.TryGetOrganizationNumberFromAttributeMatch(delegation.To, out string toOrgNo))
            {
                toParty = await _contextRetrievalService.GetPartyForOrganization(toOrgNo);
            }
            else if (DelegationHelper.TryGetSocialSecurityNumberAndLastNameAttributeMatch(delegation.To, out string toSsn, out string toLastName))
            {
                toUser = await _profileLookup.GetUserProfile(authenticatedUserId, new UserProfileLookup { Ssn = toSsn }, toLastName);
            }
            else if (DelegationHelper.TryGetUsernameAndLastNameAttributeMatch(delegation.To, out string toUsername, out toLastName))
            {
                toUser = await _profileLookup.GetUserProfile(authenticatedUserId, new UserProfileLookup { Username = toUsername }, toLastName);
            }
            else if (DelegationHelper.TryGetEnterpriseUserNameAttributeMatch(delegation.To, out string enterpriseUserName))
            {
                toUser = await _profile.GetUser(new UserProfileLookup { Username = enterpriseUserName });

                if (toUser != null && toUser.Party.PartyTypeName != PartyType.Organisation && toUser.Party.PartyId != fromParty.PartyId)
                {
                    result.Errors.Add("To", $"Enterpriseuser either does not exist or does not belong to the From party and cannot be delegated to.");
                    return (result, resource, null, null);
                }
            }
            else if (DelegationHelper.TryGetSingleAttributeMatchValue(delegation.To, AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid, out string toPersonUuidAttrValue))
            {
                if (Guid.TryParse(toPersonUuidAttrValue, out Guid toPersonUuid))
                {
                    toUser = await _profile.GetUser(new UserProfileLookup { UserUuid = toPersonUuid });
                }

                if (toUser != null && toUser.Party.PartyTypeName != PartyType.Person)
                {
                    result.Errors.Add("To", $"The provided To attribute value could not be found as a valid person.");
                    return (result, resource, null, null);
                }
            }
            else if (DelegationHelper.TryGetSingleAttributeMatchValue(delegation.To, AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid, out string toEnterpriseUserUuidAttrValue))
            {
                if (Guid.TryParse(toEnterpriseUserUuidAttrValue, out Guid toEnterpriseUserUuid))
                {
                    toUser = await _profile.GetUser(new UserProfileLookup { UserUuid = toEnterpriseUserUuid });
                }

                if (toUser != null && toUser.Party.PartyTypeName != PartyType.Organisation && toUser.Party.PartyId != fromParty.PartyId)
                {
                    result.Errors.Add("To", $"Enterpriseuser either does not exist or does not belong to the From party and cannot be delegated to.");
                    return (result, resource, null, null);
                }
            }
            else if (DelegationHelper.TryGetSingleAttributeMatchValue(delegation.To, AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid, out string toOrganizationUuidAttrValue))
            {
                if (Guid.TryParse(toOrganizationUuidAttrValue, out Guid toOrganizationUuid))
                {
                    toParty = await _contextRetrievalService.GetPartyByUuid(toOrganizationUuid);
                }

                if (toParty != null && toParty.PartyTypeName != PartyType.Organisation)
                {
                    result.Errors.Add("To", $"The provided To attribute value could not be found as a valid organization.");
                    return (result, resource, null, null);
                }
            }
            else if (DelegationHelper.TryGetSingleAttributeMatchValue(delegation.To, AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid, out string toSystemUserUuidAttrValue))
            {
                toSystemUser = await _contextRetrievalService.GetSystemUserById(fromParty.PartyId, toSystemUserUuidAttrValue);

                if (toSystemUser == null)
                {
                    result.Errors.Add("To", $"The provided To attribute value could not be found as a valid systemuser.");
                    return (result, resource, null, null);
                }

                // Verify the resource is on the list of resources for system if this is a delegation
                if (delegationAction == DelegationActionType.Delegation)
                {
                    List<DefaultRight> defaultRights = await _contextRetrievalService.GetDefaultRightsForRegisteredSystem(toSystemUser.SystemId);
                    bool resourceValid = DelegationHelper.CheckResourceIsInListOfDefaultRights(defaultRights, delegation.Rights[0].Resource);

                    if (!resourceValid)
                    {
                        result.Errors.Add("right[0].Resource", $"The resource does not exist or is not available for delegation to this systemuser");
                        return (result, resource, null, null);
                    }
                }
            }

            if (toParty == null && toUser == null && toSystemUser == null)
            {
                result.Errors.Add("To", $"A distinct recipient party for the delegation, could not be identified by the supplied attributes. A recipient can be identified by either a single {AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationNumberAttribute} or {AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserName} attribute, or a combination of {AltinnXacmlConstants.MatchAttributeIdentifiers.PersonId} and {AltinnXacmlConstants.MatchAttributeIdentifiers.PersonLastName} attributes, {AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUserName} and {AltinnXacmlConstants.MatchAttributeIdentifiers.PersonLastName} attributes or {AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid} attribute.");
                return (result, resource, null, null);
            }

            // Verify delegation From and To is not the same party (with exception for Altinn 2 Enterprise users)
            if (fromParty.PartyId == toParty?.PartyId || (toUser != null && fromParty.PartyId == toUser.PartyId && toUser.Party.PartyTypeName != PartyType.Organisation))
            {
                result.Errors.Add("To", $"The From party and the To recipient are the same. Self-delegation is not supported as it serves no purpose.");
                return (result, resource, null, null);
            }

            // Build To AttributeMatch to be used for the delegation rules
            List<AttributeMatch> to = new List<AttributeMatch>();
            if (toParty != null)
            {
                to.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, Value = toParty.PartyId.ToString() });
                if (toParty.PartyTypeName == PartyType.Organisation && toParty.PartyUuid != null)
                {
                    to.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid, Value = toParty.PartyUuid.ToString() });
                }
            }

            if (toUser != null)
            {
                to.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, Value = toUser.UserId.ToString() });
                if (toUser.UserType == UserType.EnterpriseIdentified && toUser.UserUuid != null)
                {
                    to.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid, Value = toUser.UserUuid.ToString() });
                }

                if (toUser.UserType == UserType.SSNIdentified && toUser.UserUuid != null)
                {
                    to.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid, Value = toUser.UserUuid.ToString() });
                }
            }

            if (toSystemUser != null)
            {
                to.Add(new AttributeMatch { Id = AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid, Value = toSystemUser.Id });
            }

            return (result, resource, fromParty, to);
        }

        private static bool ValidateAllRightsAreForTheSameResource(List<Right> rights)
        {
            string firstResourceKey = string.Empty;
            foreach (Right right in rights)
            {
                DelegationHelper.TryGetResourceFromAttributeMatch(right.Resource, out ResourceAttributeMatchType resourceMatchType, out string resourceRegistryId, out string org, out string app, out string serviceCode, out string serviceEditionCode);
                string currentResourceKey = $"{resourceMatchType}{resourceRegistryId}{org}{app}{serviceCode}{serviceEditionCode}";

                if (firstResourceKey == string.Empty)
                {
                    firstResourceKey = currentResourceKey;
                }

                if (firstResourceKey != currentResourceKey)
                {
                    return false;
                }
            }

            return true;
        }
    }
}


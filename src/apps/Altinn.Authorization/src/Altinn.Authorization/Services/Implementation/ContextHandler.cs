using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Models;
using Altinn.Authorization.Models.Register;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.Oed;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Models;
using Authorization.Platform.Authorization.Models;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// The context handler is responsible for updating a context request
    /// From XACML standard
    ///
    /// Context Handler
    /// The system entity that converts decision requests in the native request format to the XACML canonical form, coordinates with Policy
    /// Information Points to add attribute values to the request context, and converts authorization decisions in the XACML canonical form to
    /// the native response format
    /// </summary>
    public class ContextHandler : IContextHandler
    {
        private readonly string _uidUserProfileCacheKeyPrefix = "profile:uid:";
        private readonly string _pidUserProfileCacheKeyPrefix = "profile:pid:";

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1600 // Elements should be documented
        protected readonly IInstanceMetadataRepository _policyInformationRepository;
        protected readonly IRoles _rolesWrapper;
        protected readonly IOedRoleAssignmentWrapper _oedRolesWrapper;
        protected readonly IParties _partiesWrapper;
        protected readonly IProfile _profileWrapper;
        protected readonly IMemoryCache _memoryCache;
        protected readonly GeneralSettings _generalSettings;
        protected readonly IRegisterService _registerService;
        protected readonly IPolicyRetrievalPoint _prp;
        protected readonly IAccessManagementWrapper _accessManagementWrapper;
        protected readonly IFeatureManager _featureManager;
        protected readonly IResourceRegistry _resourceRegistry;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1600 // Elements should be documented

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextHandler"/> class
        /// </summary>
        /// <param name="policyInformationRepository">the policy information repository handler</param>
        /// <param name="rolesWrapper">the roles handler</param>
        /// <param name="oedRolesWrapper">service handling oed role retireval</param>
        /// <param name="partiesWrapper">the party information handler</param>
        /// <param name="profileWrapper">the user profile information handler</param>
        /// <param name="memoryCache">The cache handler </param>
        /// <param name="settings">The app settings</param>
        /// <param name="registerService">Register service</param>
        /// <param name="prp">service handling policy retireval</param>
        /// <param name="accessManagementWrapper">accessmanagement pip api wrapper</param>
        /// <param name="featureManager">Feature manager</param>
        public ContextHandler(
            IInstanceMetadataRepository policyInformationRepository, IRoles rolesWrapper, IOedRoleAssignmentWrapper oedRolesWrapper, IParties partiesWrapper, IProfile profileWrapper, IMemoryCache memoryCache, IOptions<GeneralSettings> settings, IRegisterService registerService, IPolicyRetrievalPoint prp, IAccessManagementWrapper accessManagementWrapper, IFeatureManager featureManager, IResourceRegistry resourceRegistry)
        {
            _policyInformationRepository = policyInformationRepository;
            _rolesWrapper = rolesWrapper;
            _oedRolesWrapper = oedRolesWrapper;
            _partiesWrapper = partiesWrapper;
            _profileWrapper = profileWrapper;
            _memoryCache = memoryCache;
            _generalSettings = settings.Value;
            _registerService = registerService;
            _prp = prp;
            _accessManagementWrapper = accessManagementWrapper;
            _featureManager = featureManager;
            _resourceRegistry = resourceRegistry;
        }

        /// <inheritdoc/>
        public async Task<XacmlContextRequest> Enrich(XacmlContextRequest request, bool isExternalRequest, SortedDictionary<string, AuthInfo> appInstanceInfo, CancellationToken cancellationToken = default)
        {
            await EnrichResourceAttributes(request, isExternalRequest, appInstanceInfo);
            return await Task.FromResult(request);
        }

        /// <summary>
        /// Enriches the resource attribute collection with additional attributes retrieved based on the instance on the request
        /// </summary>
        /// <param name="request">The original Xacml Context Request</param>
        /// <param name="isExternalRequest">Defines if request comes </param>
        /// <param name="appInstanceInfo">Cache of auto info for this request</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        protected async Task EnrichResourceAttributes(XacmlContextRequest request, bool isExternalRequest, SortedDictionary<string, AuthInfo> appInstanceInfo, CancellationToken cancellationToken = default)
        {
            XacmlContextAttributes resourceContextAttributes = request.GetResourceAttributes();
            XacmlResourceAttributes resourceAttributes = GetResourceAttributeValues(resourceContextAttributes);
            await EnrichResourceParty(resourceContextAttributes, resourceAttributes, isExternalRequest, cancellationToken);

            bool resourceAttributeComplete = IsResourceComplete(resourceAttributes);

            if (!resourceAttributeComplete && !string.IsNullOrEmpty(resourceAttributes.InstanceValue))
            {
                Instance instanceData = null;
                if (!_generalSettings.UseStorageApiForInstanceAuthInfo)
                {
                    instanceData = await _policyInformationRepository.GetInstance(resourceAttributes.InstanceValue);
                }
                else
                {
                    instanceData = new();
                    if (!appInstanceInfo.TryGetValue(resourceAttributes.InstanceValue, out AuthInfo authInfo))
                    {
                        authInfo = await _policyInformationRepository.GetAuthInfo(resourceAttributes.InstanceValue);
                        appInstanceInfo[resourceAttributes.InstanceValue] = authInfo;
                    }

                    instanceData.Process = authInfo.Process;
                    instanceData.AppId = authInfo.AppId;
                    instanceData.Org = instanceData.AppId.Split('/')[0];
                }

                if (instanceData != null)
                {
                    AddIfValueDoesNotExist(resourceContextAttributes, XacmlRequestAttribute.OrgAttribute, resourceAttributes.OrgValue, instanceData.Org);
                    string app = instanceData.AppId.Split("/")[1];
                    AddIfValueDoesNotExist(resourceContextAttributes, XacmlRequestAttribute.AppAttribute, resourceAttributes.AppValue, app);
                    if (instanceData.Process?.CurrentTask != null)
                    {
                        AddIfValueDoesNotExist(resourceContextAttributes, XacmlRequestAttribute.TaskAttribute, resourceAttributes.TaskValue, instanceData.Process.CurrentTask.ElementId);
                    }
                    else if (instanceData.Process?.EndEvent != null)
                    {
                        AddIfValueDoesNotExist(resourceContextAttributes, XacmlRequestAttribute.EndEventAttribute, null, instanceData.Process.EndEvent);
                    }

                    string partyId = resourceAttributes.InstanceValue.Split('/')[0];
                    AddIfValueDoesNotExist(resourceContextAttributes, XacmlRequestAttribute.PartyAttribute, resourceAttributes.ResourcePartyValue, partyId);
                    resourceAttributes.ResourcePartyValue = partyId;
                }
            }

            await EnrichSubjectAttributes(request, resourceAttributes, isExternalRequest, cancellationToken);
        }

        private static bool IsResourceComplete(XacmlResourceAttributes resourceAttributes)
        {
            bool resourceAttributeComplete = false;
            if (!string.IsNullOrEmpty(resourceAttributes.OrgValue) &&
                !string.IsNullOrEmpty(resourceAttributes.AppValue) &&
                !string.IsNullOrEmpty(resourceAttributes.InstanceValue) &&
                !string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue) &&
                (!string.IsNullOrEmpty(resourceAttributes.TaskValue) || !string.IsNullOrEmpty(resourceAttributes.EndEventValue)))
            {
                // The resource attributes are complete
                resourceAttributeComplete = true;
            }
            else if (!string.IsNullOrEmpty(resourceAttributes.OrgValue) &&
                !string.IsNullOrEmpty(resourceAttributes.AppValue) &&
                string.IsNullOrEmpty(resourceAttributes.InstanceValue) &&
                !string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue) &&
                string.IsNullOrEmpty(resourceAttributes.TaskValue))
            {
                // The resource attributes are complete
                resourceAttributeComplete = true;
            }
            else if (!string.IsNullOrEmpty(resourceAttributes.OrgValue) &&
            !string.IsNullOrEmpty(resourceAttributes.AppValue) &&
            !string.IsNullOrEmpty(resourceAttributes.InstanceValue) &&
            !string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue) &&
            !string.IsNullOrEmpty(resourceAttributes.AppResourceValue) &&
            resourceAttributes.AppResourceValue.Equals("events"))
            {
                // The resource attributes are complete
                resourceAttributeComplete = true;
            }
            else if (!string.IsNullOrEmpty(resourceAttributes.ResourceRegistryId) &&
           !string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue))
            {
                // The resource attributes are complete
                resourceAttributeComplete = true;
            }

            return resourceAttributeComplete;
        }

        /// <summary>
        /// Method that adds information about the resource party 
        /// </summary>
        /// <returns></returns>
        protected async Task EnrichResourceParty(XacmlContextAttributes requestResourceAttributes, XacmlResourceAttributes resourceAttributes, bool isExternalRequest, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue) && !string.IsNullOrEmpty(resourceAttributes.OrganizationNumber))
            {
                Party party = await _registerService.PartyLookup(resourceAttributes.OrganizationNumber, null, cancellationToken);
                if (party != null)
                {
                    resourceAttributes.ResourcePartyValue = party.PartyId.ToString();
                    requestResourceAttributes.Attributes.Add(GetPartyIdsAttribute(new List<int> { party.PartyId }));
                }
            }
            else if (string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue) && !string.IsNullOrEmpty(resourceAttributes.PersonId))
            {
                if (!isExternalRequest)
                {
                    throw new ArgumentException("Not allowed to use ssn for internal API");
                }

                Party party = await _registerService.PartyLookup(null, resourceAttributes.PersonId, cancellationToken);
                if (party != null)
                {
                    resourceAttributes.ResourcePartyValue = party.PartyId.ToString();
                    requestResourceAttributes.Attributes.Add(GetPartyIdsAttribute(new List<int> { party.PartyId }));
                }
            }
            else if (string.IsNullOrEmpty(resourceAttributes.ResourcePartyValue) && resourceAttributes.PartyUuid != Guid.Empty)
            {
                List<Party> parties = await _registerService.GetPartiesAsync(new List<Guid> { resourceAttributes.PartyUuid }, cancellationToken: cancellationToken);
                if (parties != null && parties.Count == 1)
                {
                    Party party = parties.FirstOrDefault();
                    resourceAttributes.ResourcePartyValue = party.PartyId.ToString();
                    requestResourceAttributes.Attributes.Add(GetPartyIdsAttribute(new List<int> { party.PartyId }));
                }
            }
        }

        /// <summary>
        /// Maps the XacmlContextAttributes for the Xacml Resource category to the Altinn XacmlResourceAttributes model
        /// </summary>
        /// <param name="resourceContextAttributes">XacmlContextAttributes for mapping of resource attribute values</param>
        /// <returns>XacmlResourceAttributes</returns>
        protected XacmlResourceAttributes GetResourceAttributeValues(XacmlContextAttributes resourceContextAttributes)
        {
            Guard.IsNotNull(resourceContextAttributes);

            XacmlResourceAttributes resourceAttributes = new XacmlResourceAttributes();

            foreach (XacmlAttribute attribute in resourceContextAttributes.Attributes)
            {
                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.OrgAttribute))
                {
                    resourceAttributes.OrgValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.AppAttribute))
                {
                    resourceAttributes.AppValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.InstanceAttribute))
                {
                    resourceAttributes.InstanceValue = attribute.AttributeValues.First().Value;
                    string[] instanceValues = resourceAttributes.InstanceValue.Split('/');
                    resourceAttributes.ResourceInstanceValue = instanceValues[1];
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.ResourceRegistryInstanceAttribute))
                {
                    resourceAttributes.ResourceInstanceValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.PartyAttribute))
                {
                    resourceAttributes.ResourcePartyValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.PartyUuidAttribute) && Guid.TryParse(attribute.AttributeValues.First().Value, out Guid partyUuid))
                {
                    resourceAttributes.PartyUuid = partyUuid;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.TaskAttribute))
                {
                    resourceAttributes.TaskValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.EndEventAttribute))
                {
                    resourceAttributes.EndEventValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.AppResourceAttribute))
                {
                    resourceAttributes.AppResourceValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.ResourceRegistryAttribute))
                {
                    string resourceValue = attribute.AttributeValues.First().Value;
                    if (resourceValue.StartsWith("app_"))
                    {
                        string[] orgAppValues = resourceValue.Split('_');
                        resourceAttributes.OrgValue = orgAppValues[1];
                        resourceAttributes.AppValue = orgAppValues[2];
                    }
                    else
                    {
                        resourceAttributes.ResourceRegistryId = resourceValue;
                    }
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.OrganizationNumberAttribute))
                {
                    resourceAttributes.OrganizationNumber = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.LegacyOrganizationNumberAttribute))
                {
                    // For supporting legacy use of this attribute. (old PEPS)
                    if (string.IsNullOrEmpty(resourceAttributes.OrganizationNumber))
                    {
                        resourceAttributes.OrganizationNumber = attribute.AttributeValues.First().Value;
                    }
                }

                if (attribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.PersonIdAttribute))
                {
                    resourceAttributes.PersonId = attribute.AttributeValues.First().Value;
                }
            }

            return resourceAttributes;
        }

        /// <summary>
        /// Add a XacmlAttribute to the resourceAttributes collection, if the existing value is empty
        /// </summary>
        /// <param name="resourceAttributes">The collection of resource attribues</param>
        /// <param name="attributeId">The attribute id</param>
        /// <param name="attributeValue">The existing attribute value</param>
        /// <param name="newAttributeValue">The new attribute value</param>
        protected void AddIfValueDoesNotExist(XacmlContextAttributes resourceAttributes, string attributeId, string attributeValue, string newAttributeValue)
        {
            if (string.IsNullOrEmpty(attributeValue))
            {
                resourceAttributes.Attributes.Add(GetAttribute(attributeId, newAttributeValue));
            }
        }

        /// <summary>
        /// Gets a XacmlAttribute model for the specified attribute id and value
        /// </summary>
        /// <param name="attributeId">The attribute id</param>
        /// <param name="attributeValue">The attribute value</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetAttribute(string attributeId, string attributeValue)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(attributeId), false);
            if (attributeId.Equals(XacmlRequestAttribute.PartyAttribute))
            {
                // When Party attribute is missing from input it is good to return it so PEP can get this information
                attribute.IncludeInResult = true;
            }

            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), attributeValue));
            return attribute;
        }

        /// <summary>
        /// Enriches the XacmlContextRequest with the Roles the subject user has for the resource reportee
        /// </summary>
        /// <param name="request">The original Xacml Context Request</param>
        /// <param name="resourceAttr">The resource reportee attributes</param>
        /// <param name="isExternalRequest">Used to enforce stricter requirements</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        protected async Task EnrichSubjectAttributes(XacmlContextRequest request, XacmlResourceAttributes resourceAttr, bool isExternalRequest, CancellationToken cancellationToken)
        {
            XacmlContextAttributes subjectContextAttributes = request.GetSubjectAttributes();

            Guard.IsNotNull(subjectContextAttributes);
            Guard.IsNotNull(subjectContextAttributes.Attributes, $"Subject attributes missing");
            Guard.IsNotEmpty(subjectContextAttributes.Attributes, $"Subject attributes empty");

            int subjectUserId = 0;
            int.TryParse(resourceAttr.ResourcePartyValue, out int resourcePartyId);
            string subjectSsn = string.Empty;
            string subjectOrgnNo = string.Empty;
            Guid subjectSystemUser = Guid.Empty;
            Guid subjectPartyUuid = Guid.Empty;
            bool foundLegacyOrgNoAttribute = false;

            if (subjectContextAttributes.Attributes.Any(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.SystemUserIdAttribute)) && subjectContextAttributes.Attributes.Count > 1)
            {
                throw new ArgumentException($"Subject attribute {XacmlRequestAttribute.SystemUserIdAttribute} can only be used by itself and not in combination with other subject identifiers.");
            }

            foreach (XacmlAttribute xacmlAttribute in subjectContextAttributes.Attributes)
            {
                if (xacmlAttribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.UserAttribute))
                {
                    subjectUserId = Convert.ToInt32(xacmlAttribute.AttributeValues.First().Value);
                }

                if (xacmlAttribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.PersonIdAttribute))
                {
                    if (!isExternalRequest)
                    {
                        throw new ArgumentException($"Not allowed to use attribute {XacmlRequestAttribute.PersonIdAttribute} for internal API");
                    }

                    subjectSsn = xacmlAttribute.AttributeValues.First().Value;
                }

                if (xacmlAttribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.LegacyOrganizationNumberAttribute))
                {
                    foundLegacyOrgNoAttribute = true;
                    subjectOrgnNo = xacmlAttribute.AttributeValues.First().Value;
                }

                if (xacmlAttribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.OrganizationNumberAttribute))
                {
                    subjectOrgnNo = xacmlAttribute.AttributeValues.First().Value;
                }

                if (xacmlAttribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.PartyUuidAttribute) && !Guid.TryParse(xacmlAttribute.AttributeValues.First().Value, out subjectPartyUuid))
                {
                    throw new ArgumentException($"{XacmlRequestAttribute.PartyUuidAttribute}: Not a valid uuid");
                }

                if (xacmlAttribute.AttributeId.OriginalString.Equals(XacmlRequestAttribute.SystemUserIdAttribute) && !Guid.TryParse(xacmlAttribute.AttributeValues.First().Value, out subjectSystemUser))
                {
                    throw new ArgumentException($"{XacmlRequestAttribute.SystemUserIdAttribute}: Not a valid uuid");
                }
            }

            if (foundLegacyOrgNoAttribute)
            {
                subjectContextAttributes.Attributes.Add(GetOrganizationIdentifierAttribute(subjectOrgnNo));
            }

            if (!string.IsNullOrEmpty(subjectSsn) && subjectUserId != 0)
            {
                throw new ArgumentException("Not allowed to set userid and person-id for subject at the same time");
            }

            if (isExternalRequest && !string.IsNullOrEmpty(subjectOrgnNo) && (subjectUserId != 0 || !string.IsNullOrEmpty(subjectSsn)))
            {
                throw new ArgumentException("Not allowed to set organization number and person-id or userid for subject at the same time");
            }

            if (!string.IsNullOrEmpty(subjectSsn))
            {
                UserProfile subjectProfile = await GetUserProfileByPersonId(subjectSsn, cancellationToken);
                if (subjectProfile != null)
                {
                    subjectUserId = subjectProfile.UserId;
                    subjectPartyUuid = subjectProfile.Party.PartyUuid.HasValue ? subjectProfile.Party.PartyUuid.Value : Guid.Empty;
                    subjectContextAttributes.Attributes.Add(GetUserIdAttribute(subjectUserId));
                }
                else
                {
                    throw new ArgumentException("Invalid person-id");
                }
            }
            else if (subjectUserId != 0)
            {
                UserProfile subjectProfile = await GetUserProfileByUserId(subjectUserId, cancellationToken);
                if (subjectProfile != null)
                {
                    subjectSsn = subjectProfile.Party.PartyTypeName == PartyType.Person ? subjectProfile.Party.SSN : null;
                    subjectPartyUuid = subjectProfile.Party.PartyUuid.HasValue ? subjectProfile.Party.PartyUuid.Value : Guid.Empty;
                }
            }

            if (isExternalRequest && !string.IsNullOrEmpty(subjectOrgnNo))
            {
                Party party = await _registerService.PartyLookup(subjectOrgnNo, null, cancellationToken);
                subjectContextAttributes.Attributes.Add(GetPartyIdsAttribute(new List<int> { party.PartyId }));
            }

            XacmlPolicy xacmlPolicy = await _prp.GetPolicyAsync(request);
            if (xacmlPolicy == null)
            {
                return;
            }

            // Get all subject attribute types used in the policy
            IDictionary<string, ICollection<string>> policySubjectAttributes = xacmlPolicy.GetAttributeDictionaryByCategory(XacmlConstants.MatchAttributeCategory.Subject);

            // Enrich with access package attributes if policy contains rules for access packages and request has a specified resource party id
            if (policySubjectAttributes.ContainsKey(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute) && resourcePartyId != 0)
            {
                if (resourceAttr.PartyUuid == Guid.Empty)
                {
                    List<Party> party = await _registerService.GetPartiesAsync(new List<int> { resourcePartyId }, cancellationToken: cancellationToken);

                    if (party.Count == 1 && party[0].PartyUuid.HasValue)
                    {
                        resourceAttr.PartyUuid = party[0].PartyUuid.Value;
                    }
                    else
                    {
                        return;
                    }
                }

                if (await _featureManager.IsEnabledAsync(FeatureFlags.SystemUserAccessPackageAuthorization) && subjectSystemUser != Guid.Empty)
                {
                    await AddAccessPackageAttributes(subjectContextAttributes, subjectSystemUser, resourceAttr.PartyUuid);
                }
                else if (await _featureManager.IsEnabledAsync(FeatureFlags.UserAccessPackageAuthorization) && subjectPartyUuid != Guid.Empty)
                {
                    await AddAccessPackageAttributes(subjectContextAttributes, subjectPartyUuid, resourceAttr.PartyUuid);
                }
            }

            // Enrich with party type if rule defines that and request only contains resource id. This is special handling for consent. Before opening more widely this needs more consideration.
            if (policySubjectAttributes.ContainsKey(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyTypeAttribute)
                && resourceAttr.InstanceValue == null && resourceAttr.ResourceInstanceValue == null && resourceAttr.AppValue == null && !string.IsNullOrEmpty(resourceAttr.ResourceRegistryId)
                && string.IsNullOrEmpty(resourceAttr.ResourcePartyValue))
            {
                if (subjectPartyUuid == Guid.Empty && !string.IsNullOrEmpty(subjectOrgnNo))
                {
                    Party party = await _registerService.PartyLookup(subjectOrgnNo, null, cancellationToken);
                    subjectContextAttributes.Attributes.Add(GetPartyTypeAttribute(party.PartyTypeName));
                }
                else if (subjectPartyUuid != Guid.Empty)
                {
                    List<Party> partyList = await _registerService.GetPartiesAsync([subjectPartyUuid], false, cancellationToken);
                    subjectContextAttributes.Attributes.Add(GetPartyTypeAttribute(partyList[0].PartyTypeName));
                }
            }

            // Enrich with access lists if rule defines that and request only contains resource id. Before opening more widely this needs more consideration.
            if (policySubjectAttributes.ContainsKey(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessListAttribute)
                && resourceAttr.InstanceValue == null && resourceAttr.ResourceInstanceValue == null && resourceAttr.AppValue == null && !string.IsNullOrEmpty(resourceAttr.ResourceRegistryId)
                && string.IsNullOrEmpty(resourceAttr.ResourcePartyValue))
            {
                if (subjectPartyUuid == Guid.Empty && !string.IsNullOrEmpty(subjectOrgnNo))
                {
                    Party party = await _registerService.PartyLookup(subjectOrgnNo, null, cancellationToken);
                    subjectPartyUuid = party.PartyUuid.HasValue ? party.PartyUuid.Value : Guid.Empty;
                }

                if (subjectPartyUuid != Guid.Empty)
                {
                    PartyUrn.PartyUuid partyUrn = PartyUrn.PartyUuid.Create(subjectPartyUuid);
                    IEnumerable<AccessListInfoDto> memberShip = await _resourceRegistry.GetMembershipsForParty(partyUrn, cancellationToken);
                    if (memberShip != null && memberShip.Count() > 0)
                    {
                        subjectContextAttributes.Attributes.Add(GetAccessListAttributes(memberShip));
                    }
                }
            }

            // Further enrichment of roles can/must be skipped if no subject userId or resource partyId exists
            if (subjectUserId == 0 || resourcePartyId == 0)
            {
                return;
            }

            if (policySubjectAttributes.ContainsKey(AltinnXacmlConstants.MatchAttributeIdentifiers.OedRoleAttribute))
            {
                if (string.IsNullOrEmpty(subjectSsn))
                {
                    subjectSsn = await GetPersonIdForUser(subjectUserId, cancellationToken);
                }

                string resourceSsn = await GetSSnForParty(resourcePartyId);

                if (!string.IsNullOrWhiteSpace(subjectSsn) && !string.IsNullOrWhiteSpace(resourceSsn))
                {
                    List<OedRoleAssignment> oedRoleAssignments = await GetOedRoleAssignments(resourceSsn, subjectSsn);
                    if (oedRoleAssignments.Count != 0)
                    {
                        subjectContextAttributes.Attributes.Add(GetOedRoleAttributes(oedRoleAssignments));
                    }
                }
            }

            if (policySubjectAttributes.ContainsKey(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute))
            {
                List<Role> roleList = await GetRoles(subjectUserId, resourcePartyId);
                if (roleList.Count != 0)
                {
                    subjectContextAttributes.Attributes.Add(GetRoleAttribute(roleList));
                }
            }
        }

        /// <summary>
        /// Enriches the context with all access package attributes the given subject has access to on behalf of the party
        /// </summary>
        /// <param name="subjectContextAttributes">The subject attribute collection to enrich with access packages (if any) the subject user has for the party</param>
        /// <param name="toSubjectPartyUuid">The subject party uuid to check if has any access packages for the party</param>
        /// <param name="resourceParty">The party to check if subject party has any access packages for.</param>
        protected async Task AddAccessPackageAttributes(XacmlContextAttributes subjectContextAttributes, Guid toSubjectPartyUuid, Guid resourceParty)
        {
            IEnumerable<AccessPackageUrn> accessPackages = await _accessManagementWrapper.GetAccessPackages(toSubjectPartyUuid, resourceParty);
            foreach (AccessPackageUrn accessPackage in accessPackages)
            {
                subjectContextAttributes.Attributes.Add(GetStringAttribute(accessPackage.PrefixSpan.ToString(), accessPackage.ValueSpan.ToString()));
            }
        }

        /// <summary>
        /// Gets a XacmlAttribute model for a list of party ids
        /// </summary>
        /// <param name="attributeId">The attribute id for the type of value(s)</param>
        /// <param name="values">The collection of values</param>
        /// <param name="dataType">Optional: specify datatype. Default: XMLString</param>
        /// <returns>XacmlAttribute</returns>
        protected static XacmlAttribute GetStringAttribute(string attributeId, IEnumerable<string> values, string dataType = XacmlConstants.DataTypes.XMLString)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(attributeId), false);
            foreach (string value in values)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(dataType), value));
            }

            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for a list of party ids
        /// </summary>
        /// <param name="attributeId">The attribute id for the type of value(s)</param>
        /// <param name="value">The value</param>
        /// <param name="dataType">Optional: specify datatype. Default: XMLString</param>
        /// <returns>XacmlAttribute</returns>
        protected static XacmlAttribute GetStringAttribute(string attributeId, string value, string dataType = XacmlConstants.DataTypes.XMLString)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(attributeId), false);
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(dataType), value));
            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for the list of roletype codes
        /// </summary>
        /// <param name="roles">The list of roletype codes</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetRoleAttribute(List<Role> roles)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.RoleAttribute), false);
            foreach (Role role in roles)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), role.Value));
            }

            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for the list of oed role attributes
        /// </summary>
        /// <param name="oedRoleAssignments">The list of oedRoleAssignments</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetOedRoleAttributes(List<OedRoleAssignment> oedRoleAssignments)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.OedRoleAttribute), false);
            foreach (OedRoleAssignment oedRoleAssignment in oedRoleAssignments)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), oedRoleAssignment.OedRoleCode));
            }

            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAtribute model for the list of accessList memberships
        /// </summary>
        /// <param name="accessListMemberships">a list of accesslist memberships</param>
        /// <returns></returns>
        protected XacmlAttribute GetAccessListAttributes(IEnumerable<AccessListInfoDto> accessListMemberships)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.AccessListAttribute), false);
            foreach (AccessListInfoDto memberShip in accessListMemberships)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), memberShip.Urn.ValueSpan.ToString()));
            }

            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for a list of party ids
        /// </summary>
        /// <param name="partyType">The partyType</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetPartyTypeAttribute(PartyType partyType)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.PartyTypeAttribute), false);
           
            if (partyType == PartyType.Organisation)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), XacmlRequestAttribute.PartyTypeOrganizationValue));
            }
            else if (partyType == PartyType.Person)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), XacmlRequestAttribute.PartyTypePersonValue));
            }

            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for a list of party ids
        /// </summary>
        /// <param name="partyIds">The list of party ids</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetPartyIdsAttribute(List<int> partyIds)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.PartyAttribute), false);
            foreach (int partyId in partyIds)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), partyId.ToString()));
            }

            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for a userId
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetUserIdAttribute(int userId)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.UserAttribute), false);
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), userId.ToString()));
            return attribute;
        }

        /// <summary>
        /// Gets a XacmlAttribute model for a organization identifier (organization number)
        /// </summary>
        /// <param name="orgNo">The organization number</param>
        /// <returns>XacmlAttribute</returns>
        protected XacmlAttribute GetOrganizationIdentifierAttribute(string orgNo)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(XacmlRequestAttribute.OrganizationNumberAttribute), false);
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), orgNo));
            return attribute;
        }

        /// <summary>
        /// Gets the list of roletype codes the subject user has for the resource reportee
        /// </summary>
        /// <param name="subjectUserId">The user id of the subject</param>
        /// <param name="resourcePartyId">The party id of the reportee</param>
        /// <returns>List of roles</returns>
        protected async Task<List<Role>> GetRoles(int subjectUserId, int resourcePartyId)
        {
            string cacheKey = GetCacheKey(subjectUserId, resourcePartyId);

            if (!_memoryCache.TryGetValue(cacheKey, out List<Role> roles))
            {
                // Key not in cache, so get data.
                roles = await _rolesWrapper.GetDecisionPointRolesForUser(subjectUserId, resourcePartyId) ?? new List<Role>();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.RoleCacheTimeout, 0));

                _memoryCache.Set(cacheKey, roles, cacheEntryOptions);
            }

            return roles;
        }

        /// <summary>
        /// Gets the list of mainunits for a subunit
        /// </summary>
        /// <param name="subUnitPartyId">The subunit partyId to check and retrieve mainunits for</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>List of mainunits</returns>
        protected async Task<List<MainUnit>> GetMainUnits(int subUnitPartyId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"GetMainUnitsFor:{subUnitPartyId}";

            if (!_memoryCache.TryGetValue(cacheKey, out List<MainUnit> mainUnits))
            {
                // Key not in cache, so get data.
                mainUnits = await _partiesWrapper.GetMainUnits(new MainUnitQuery { PartyIds = new List<int> { subUnitPartyId } }, cancellationToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.MainUnitCacheTimeout, 0));

                _memoryCache.Set(cacheKey, mainUnits, cacheEntryOptions);
            }

            return mainUnits;
        }

        /// <summary>
        /// Gets the list of keyrole unit partyIds for a user
        /// </summary>
        /// <param name="subjectUserId">The userid to retrieve keyrole unit for</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>List of partyIds for units where user has keyrole</returns>
        protected async Task<List<int>> GetKeyRolePartyIds(int subjectUserId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"GetKeyRolePartyIdsFor:{subjectUserId}";

            if (!_memoryCache.TryGetValue(cacheKey, out List<int> keyrolePartyIds))
            {
                // Key not in cache, so get data.
                keyrolePartyIds = await _partiesWrapper.GetKeyRoleParties(subjectUserId, cancellationToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.MainUnitCacheTimeout, 0));

                _memoryCache.Set(cacheKey, keyrolePartyIds, cacheEntryOptions);
            }

            return keyrolePartyIds;
        }

        /// <summary>
        /// Gets a list of role assignments between to persons (if exists) from the OED Authz PIP API
        /// </summary>
        /// <param name="from">the party which the role assignment provides access on behalf of</param>
        /// <param name="to">the role assignment recipient party</param>
        /// <returns>list of OED/Digitalt dodsbo Role Assignments</returns>
        protected async Task<List<OedRoleAssignment>> GetOedRoleAssignments(string from, string to)
        {
            string cacheKey = GetOedRoleassignmentCacheKey(from, to);

            if (!_memoryCache.TryGetValue(cacheKey, out List<OedRoleAssignment> oedRoles))
            {
                oedRoles = await _oedRolesWrapper.GetOedRoleAssignments(from, to);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.RoleCacheTimeout, 0));

                _memoryCache.Set(cacheKey, oedRoles, cacheEntryOptions);
            }

            return oedRoles;
        }

        /// <summary>
        /// Method that fetches the user profile for a given user id
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The user profile</returns>
        protected async Task<UserProfile> GetUserProfileByUserId(int userId, CancellationToken cancellationToken = default)
        {
            string uidCacheKey = $"{_uidUserProfileCacheKeyPrefix}{userId}";

            if (!_memoryCache.TryGetValue(uidCacheKey, out UserProfile userProfile))
            {
                userProfile = await _profileWrapper.GetUserProfile(userId, cancellationToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.RoleCacheTimeout, 0));

                _memoryCache.Set(uidCacheKey, userProfile, cacheEntryOptions);

                if (!string.IsNullOrWhiteSpace(userProfile?.Party?.SSN))
                {
                    _memoryCache.Set($"{_pidUserProfileCacheKeyPrefix}{userProfile.Party.SSN}", userProfile, cacheEntryOptions);
                }
            }

            return userProfile;
        }

        /// <summary>
        /// Method that fetches the user profile for a given person id (aka national identity number or ssn)
        /// </summary>
        /// <param name="personId">The person id</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The user profile</returns>
        protected async Task<UserProfile> GetUserProfileByPersonId(string personId, CancellationToken cancellationToken = default)
        {
            string pidCacheKey = $"{_pidUserProfileCacheKeyPrefix}{personId}";

            if (!_memoryCache.TryGetValue(pidCacheKey, out UserProfile userProfile))
            {
                userProfile = await _profileWrapper.GetUserProfileByPersonId(personId, cancellationToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.RoleCacheTimeout, 0));

                _memoryCache.Set(pidCacheKey, userProfile, cacheEntryOptions);

                if (userProfile?.UserId > 0)
                {
                    _memoryCache.Set($"{_uidUserProfileCacheKeyPrefix}{userProfile.UserId}", userProfile, cacheEntryOptions);
                }
            }

            return userProfile;
        }

        private string GetCacheKey(int userId, int partyId)
        {
            return "rolelist_" + userId + "_" + partyId;
        }

        private string GetOedRoleassignmentCacheKey(string from, string to)
        {
            return $"oed{from}_{to}";
        }

        private async Task<string> GetPersonIdForUser(int userId, CancellationToken cancellationToken = default)
        {
            UserProfile userProfile = await GetUserProfileByUserId(userId, cancellationToken);
            return userProfile?.Party?.SSN;
        }

        private async Task<string> GetSSnForParty(int partyId)
        {
            string cacheKey = $"p:{partyId}";

            if (!_memoryCache.TryGetValue(cacheKey, out Party party))
            {
                party = await _partiesWrapper.GetParty(partyId);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.RoleCacheTimeout, 0));

                _memoryCache.Set(cacheKey, party, cacheEntryOptions);
            }

            return party?.SSN;
        }
    }
}

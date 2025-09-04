using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// The delegationg context handler is responsible for updating a context request based on information from informationpoint for a specific delegation.
    /// In order for the context request to be passed to decision point to be checked for authorization against the delegation policy
    /// </summary>
    public class DelegationContextHandler : ContextHandler, IDelegationContextHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DelegationContextHandler"/> class
        /// </summary>
        /// <param name="policyInformationRepository">the policy information repository handler</param>
        /// <param name="rolesWrapper">the roles handler</param>
        /// <param name="oedRolesWrapper">the oed roles handler</param>
        /// <param name="partiesWrapper">the party information handler</param>
        /// <param name="profileWrapper">the user profile information handler</param>
        /// <param name="memoryCache">The cache handler </param>
        /// <param name="settings">The app settings</param>
        /// <param name="registerService">Register service</param>
        /// <param name="prp">the policy retrieval point service</param>
        /// <param name="accMgmt">Access Management PIP API wrapper</param>
        /// <param name="featureManager">Feature manager for reading feature flags.</param>

        public DelegationContextHandler(IInstanceMetadataRepository policyInformationRepository, IRoles rolesWrapper, IOedRoleAssignmentWrapper oedRolesWrapper, IParties partiesWrapper, IProfile profileWrapper, IMemoryCache memoryCache, IOptions<GeneralSettings> settings, IRegisterService registerService, IPolicyRetrievalPoint prp, IAccessManagementWrapper accMgmt, IFeatureManager featureManager, IResourceRegistry resourceRegistry)
            : base(policyInformationRepository, rolesWrapper, oedRolesWrapper, partiesWrapper, profileWrapper, memoryCache, settings, registerService, prp, accMgmt, featureManager, resourceRegistry)
        {
        }

        /// <summary>
        /// Updates needed subject information for the Context Request for a specific delegation
        /// </summary>
        /// <param name="requestSubjectAttributes">The current collection of subject attributes on the request to be enriched</param>
        /// <param name="isInstanceAccessRequest">Whether the request is for a specific instance, which needs additional uuid information</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        public async Task EnrichRequestSubjectAttributes(XacmlContextAttributes requestSubjectAttributes, bool isInstanceAccessRequest, CancellationToken cancellationToken)
        {
            int subjectUserId = GetSubjectUserId(requestSubjectAttributes);
            if (subjectUserId > 0)
            {
                if (isInstanceAccessRequest)
                {
                    // Instance delegation policies use uuid as subject, meaning the request needs to be enriched with the users uuid
                    UserProfile userProfile = await GetUserProfileByUserId(subjectUserId, cancellationToken);
                    if (userProfile != null && userProfile.Party != null &&
                        userProfile.Party.PartyTypeName == PartyType.Person && userProfile.Party.PartyUuid.HasValue)
                    {
                        requestSubjectAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.PersonUuidAttribute, userProfile.Party.PartyUuid.Value.ToString()));
                    }
                }

                List<int> keyRolePartyIds = await GetKeyRolePartyIds(subjectUserId, cancellationToken);
                if (keyRolePartyIds.Count > 0)
                {
                    requestSubjectAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.PartyAttribute, keyRolePartyIds.Select(s => s.ToString())));

                    if (isInstanceAccessRequest)
                    {
                        // Instance delegation policies use uuid as subject, meaning the request needs to be enriched with the uuids of all keyrole parties
                        IEnumerable<Party> parties = await _registerService.GetPartiesAsync(keyRolePartyIds, cancellationToken: cancellationToken);
                        requestSubjectAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.OrganizationUuidAttribute, parties.Select(p => p.PartyUuid.ToString())));
                    }
                }
            }

            int subjectPartyId = GetSubjectPartyId(requestSubjectAttributes);
            if (subjectPartyId > 0 && isInstanceAccessRequest)
            {
                // Instance delegation policies use uuid as subject, meaning the request needs to be enriched with the party uuid
                Party subjectParty = await _registerService.GetParty(subjectPartyId, cancellationToken);
                if (subjectParty != null &&
                    subjectParty.PartyTypeName == PartyType.Person && subjectParty.PartyUuid.HasValue)
                {
                    requestSubjectAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.PersonUuidAttribute, subjectParty.PartyUuid.Value.ToString()));
                }
                else if (subjectParty != null && subjectParty.PartyTypeName == PartyType.Organisation && subjectParty.PartyUuid.HasValue)
                {
                    requestSubjectAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.OrganizationUuidAttribute, subjectParty.PartyUuid.Value.ToString()));
                }
            }
        }

        /// <summary>
        /// Updates needed resource information for the Context Request for a specific delegation
        /// </summary>
        /// <param name="requestResourceAttributes">The current collection of resource attributes on the request to be enriched</param>
        /// <param name="resourceAttributes">Preprocessed collection of resource attributes based on the input request <see cref="ContextHandler.GetResourceAttributeValues(XacmlContextAttributes)"/></param>
        /// <param name="isInstanceAccessRequest">Whether the request is for a specific instance, which needs additional resource information</param>
        public void EnrichRequestResourceAttributes(XacmlContextAttributes requestResourceAttributes, XacmlResourceAttributes resourceAttributes, bool isInstanceAccessRequest)
        {
            if (isInstanceAccessRequest)
            {
                XacmlAttribute resourceAttribute = requestResourceAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.ResourceRegistryAttribute));
                XacmlAttribute resourceInstanceAttribute = requestResourceAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.ResourceRegistryInstanceAttribute));
                XacmlAttribute orgAttribute = requestResourceAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.OrgAttribute));
                XacmlAttribute appAttribute = requestResourceAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.AppAttribute));
                if (resourceAttribute != null && orgAttribute == null && appAttribute == null)
                {
                    string resourceId = resourceAttribute.AttributeValues.FirstOrDefault()?.Value;
                    if (resourceId != null && resourceId.StartsWith("app_"))
                    {
                        // Missing resource attribute for Altinn App
                        requestResourceAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.OrgAttribute, resourceAttributes.OrgValue));
                        requestResourceAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.AppAttribute, resourceAttributes.AppValue));
                    }
                }

                if (resourceAttribute == null && orgAttribute != null && appAttribute != null)
                {
                    // Missing org and app attribute for Altinn App
                    requestResourceAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.ResourceRegistryAttribute, $"app_{orgAttribute.AttributeValues.FirstOrDefault()?.Value}_{appAttribute.AttributeValues.FirstOrDefault()?.Value}"));
                }

                if (resourceInstanceAttribute == null)
                {
                    // Missing resource instanceId attribute
                    requestResourceAttributes.Attributes.Add(GetStringAttribute(XacmlRequestAttribute.ResourceRegistryInstanceAttribute, resourceAttributes.ResourceInstanceValue));
                }
            }
        }

        /// <summary>
        /// Gets the user id from the XacmlContextRequest subject attribute
        /// </summary>
        /// <param name="subjectAttributes">The Xacml Context Request subject attributes</param>
        /// <returns>The user id of the subject</returns>
        public int GetSubjectUserId(XacmlContextAttributes subjectAttributes)
        {
            XacmlAttribute subjectAttribute = subjectAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.UserAttribute));
            return Convert.ToInt32(subjectAttribute?.AttributeValues.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Gets the party id from the XacmlContextRequest subject attribute
        /// </summary>
        /// <param name="subjectAttributes">The Xacml Context Request subject attributes</param>
        /// <returns>The party id of the subject</returns>
        public int GetSubjectPartyId(XacmlContextAttributes subjectAttributes)
        {
            XacmlAttribute subjectAttribute = subjectAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlRequestAttribute.PartyAttribute));
            return Convert.ToInt32(subjectAttribute?.AttributeValues.FirstOrDefault()?.Value);
        }

        /// <summary>
        /// Gets the value of the first found attribute matching the prioritized order of xacmlRequestAttributes provided, from the XacmlContextRequest subjects.
        /// </summary>
        /// <param name="request">The Xacml Context Request</param>
        /// <param name="xacmlRequestAttributeIds">Array of xacml request urn attribute identifiers to look for, in prioritized order. First found matching attribute is returned.</param>
        /// <returns>The value of the first found matching subject attribute if any exists</returns>
        public AttributeMatch GetSubjectAttributeMatch(XacmlContextRequest request, string[] xacmlRequestAttributeIds)
        {
            XacmlContextAttributes subjectContextAttributes = request.GetSubjectAttributes();
            foreach (var attributeId in xacmlRequestAttributeIds)
            {
                XacmlAttribute subjectAttribute = subjectContextAttributes.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(attributeId, StringComparison.OrdinalIgnoreCase));

                if (subjectAttribute != null)
                {
                    return new(attributeId, subjectAttribute.AttributeValues.FirstOrDefault()?.Value);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a XacmlResourceAttributes model from the XacmlContextRequest
        /// </summary>
        /// <param name="request">The Xacml Context Request</param>
        /// <returns>XacmlResourceAttributes model</returns>
        public XacmlResourceAttributes GetResourceAttributes(XacmlContextRequest request)
        {
            XacmlContextAttributes resourceContextAttributes = request.GetResourceAttributes();
            return GetResourceAttributeValues(resourceContextAttributes);
        }

        /// <inheritdoc/>
        public string GetActionString(XacmlContextRequest request)
        {
            XacmlContextAttributes actionAttributes = request.Attributes.FirstOrDefault(a => a.Category.OriginalString.Equals(XacmlConstants.MatchAttributeCategory.Action));
            return actionAttributes?.Attributes.FirstOrDefault(a => a.AttributeId.OriginalString.Equals(XacmlConstants.MatchAttributeIdentifiers.ActionId))?.AttributeValues.FirstOrDefault()?.Value;
        }

        /// <summary>
        /// Gets the list of mainunits for a subunit
        /// </summary>
        /// <param name="subUnitPartyId">The subunit partyIds to check and retrieve mainunits for</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>List of mainunits</returns>
        public async new Task<List<MainUnit>> GetMainUnits(int subUnitPartyId, CancellationToken cancellationToken = default)
        {
            return await base.GetMainUnits(subUnitPartyId, cancellationToken);
        }

        /// <summary>
        /// Gets the list of keyrole unit partyIds for a user
        /// </summary>
        /// <param name="subjectUserId">The userid to retrieve keyrole unit for</param>
        /// <param name="cancellationToken">The cancellationToken</param>
        /// <returns>List of partyIds for units where user has keyrole</returns>
        public async new Task<List<int>> GetKeyRolePartyIds(int subjectUserId, CancellationToken cancellationToken = default)
        {
            return await base.GetKeyRolePartyIds(subjectUserId, cancellationToken);
        }
    }
}

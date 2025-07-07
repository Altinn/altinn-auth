#nullable enable

using System.Text.Json;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Models.EventLog;

namespace Altinn.Platform.Authorization.Helpers
{
    /// <summary>
    /// Helper class for event logging
    /// </summary>
    public static class EventLogHelper
    {
        /// <summary>
        /// Maps the user, resource information from
        /// </summary>
        /// <param name="contextRequest">the context request</param>
        /// <param name="context">the http context</param>
        /// <param name="contextRespsonse">the http context response</param>
        /// <param name="currentDateTime">the current date time</param>
        /// <returns></returns>
        public static AuthorizationEvent MapAuthorizationEventFromContextRequest(XacmlContextRequest contextRequest, HttpContext context, XacmlContextResponse contextRespsonse, DateTimeOffset currentDateTime)
        {
            (string resource, string instanceId, int? resourcePartyId) = GetResourceAttributes(contextRequest);
            (int? userId, int? partyId, string org, int? orgNumber, string? sessionId, string? partyUuid) = GetSubjectInformation(contextRequest);
            AuthorizationEvent authorizationEvent = new AuthorizationEvent
            {
                SessionId = sessionId,
                Created = currentDateTime,
                Resource = resource,
                SubjectUserId = userId,
                SubjectOrgCode = org,
                SubjectOrgNumber = orgNumber,
                InstanceId = instanceId,
                SubjectParty = partyId,
                ResourcePartyId = resourcePartyId,
                Operation = GetActionInformation(contextRequest),
                IpAdress = GetClientIpAddress(context),
                ContextRequestJson = JsonSerializer.Serialize(contextRequest),
                Decision = contextRespsonse.Results?.FirstOrDefault()?.Decision,
                SubjectPartyUuid = partyUuid,
            };

            return authorizationEvent;
        }

        /// <summary>
        /// Returens the policy resource type based on XacmlContextRequest
        /// </summary>
        /// <param name="request">The requestId</param>
        /// <returns></returns>
        public static (string Resource, string InstanceId, int? ResourcePartyId) GetResourceAttributes(XacmlContextRequest request)
        {
            string resource = string.Empty;
            string instanceId = string.Empty;
            int? resourcePartyId = null;
            string org = string.Empty;
            string app = string.Empty;

            if (request != null)
            {
                foreach (XacmlContextAttributes attr in request.Attributes.Where(attr => attr.Category.OriginalString.Equals(XacmlConstants.MatchAttributeCategory.Resource)))
                {
                    foreach (XacmlAttribute xacmlAtr in attr.Attributes)
                    {
                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistry))
                        {
                            resource = xacmlAtr.AttributeValues.First().Value;
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute))
                        {
                            app = xacmlAtr.AttributeValues.First().Value;
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute))
                        {
                            org = xacmlAtr.AttributeValues.First().Value;
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.InstanceAttribute))
                        {
                            instanceId = xacmlAtr.AttributeValues.First().Value;
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute))
                        {
                            resourcePartyId = Convert.ToInt32(xacmlAtr.AttributeValues.First().Value);
                        }
                    }
                }
            }

            resource = string.IsNullOrEmpty(resource) ? $"app_{org}_{app}" : resource;
            return (resource, instanceId, resourcePartyId);
        }

        /// <summary>
        /// Returens the policy resource type based on XacmlContextRequest
        /// </summary>
        /// <param name="request">The requestId</param>
        /// <returns></returns>
        public static (int? UserId, int? PartyId, string Org, int? OrgNumber, string? SessionId, string? PartyUuid) GetSubjectInformation(XacmlContextRequest request)
        {
            int? userId = null;
            int? partyId = null;
            string org = string.Empty;
            int? orgNumber = null;
            string? sessionId = null;
            string? partyUuid = null;

            if (request != null)
            {
                foreach (XacmlContextAttributes attr in request.Attributes.Where(attr => attr.Category.OriginalString.Equals(XacmlConstants.MatchAttributeCategory.Subject)))
                {
                    foreach (XacmlAttribute xacmlAtr in attr.Attributes)
                    {
                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute))
                        {
                            userId = Convert.ToInt32(xacmlAtr.AttributeValues.First().Value);
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute))
                        {
                            partyId = Convert.ToInt32(xacmlAtr.AttributeValues.First().Value);
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute))
                        {
                            org = xacmlAtr.AttributeValues.First().Value;
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgNumberAttribute))
                        {
                            orgNumber = Convert.ToInt32(xacmlAtr.AttributeValues.First().Value);
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.SessionIdAttribute))
                        {
                            sessionId = xacmlAtr.AttributeValues.First().Value;
                        }

                        if (xacmlAtr.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserIdAttribute))
                        {
                            partyUuid = xacmlAtr.AttributeValues.First().Value;
                        }
                    }
                }
            }

            return (userId, partyId, org, orgNumber, sessionId, partyUuid);
        }

        /// <summary>
        /// Returens the policy resource type based on XacmlContextRequest
        /// </summary>
        /// <param name="request">The requestId</param>
        /// <returns></returns>
        public static string GetActionInformation(XacmlContextRequest request)
        {
            string actionId = string.Empty;

            if (request != null)
            {
                foreach (XacmlContextAttributes attr in request.Attributes.Where(attr => attr.Category.OriginalString.Equals(XacmlConstants.MatchAttributeCategory.Action)))
                {
                    foreach (XacmlAttribute xacmlAtr in attr.Attributes.Where(attr => attr.AttributeId.OriginalString.Equals(XacmlConstants.MatchAttributeIdentifiers.ActionId, StringComparison.Ordinal)))
                    {
                        actionId = xacmlAtr.AttributeValues.First().Value;
                    }
                }
            }

            return actionId;
        }

        /// <summary>
        /// Get the client ip address
        /// </summary>
        /// <param name="context">the http request context</param>
        /// <returns></returns>
        public static string? GetClientIpAddress(HttpContext context)
        {
            string[] clientIpList = context?.Request?.Headers?.GetCommaSeparatedValues("x-forwarded-for") ?? [];
            return clientIpList.FirstOrDefault() ?? null;
        }
    }
}

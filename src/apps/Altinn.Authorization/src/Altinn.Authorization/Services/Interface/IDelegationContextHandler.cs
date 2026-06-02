using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Models;

namespace Altinn.Platform.Authorization.Services.Interface
{
    /// <summary>
    /// Defines Interface for the Delegation Context Handler.
    /// </summary>
    public interface IDelegationContextHandler : IContextHandler
    {
        /// <summary>
        /// Updates needed subject information for the Context Request for a specific delegation
        /// </summary>
        /// <param name="requestSubjectAttributes">The current collection of subject attributes on the request to be enriched</param>
        /// <param name="keyRolePartyIds">The list of key role party IDs</param>
        /// <param name="keyRolePartyUuids">The list of key role party UUIDs</param>
        /// <param name="isInstanceAccessRequest">Whether the request is for a specific instance, which needs additional uuid information</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        public Task EnrichRequestSubjectAttributes(XacmlContextAttributes requestSubjectAttributes, List<int> keyRolePartyIds, List<Guid> keyRolePartyUuids, bool isInstanceAccessRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the value of the first found attribute matching the prioritized order of xacmlRequestAttributes provided, from the XacmlContextRequest subjects.
        /// </summary>
        /// <param name="request">The Xacml Context Request</param>
        /// <param name="xacmlRequestAttributeIds">Array of xacml request urn attribute identifiers to look for, in prioritized order. First found matching attribute is returned.</param>
        /// <returns>The value of the first found matching subject attribute if any exists</returns>
        public AttributeMatch GetSubjectAttributeMatch(XacmlContextRequest request, string[] xacmlRequestAttributeIds);

        /// <summary>
        /// Gets the user id from the XacmlContextRequest subject attribute
        /// </summary>
        /// <param name="subjectAttributes">The Xacml Context Request subject attributes</param>
        /// <returns>The user id of the subject</returns>
        public int GetSubjectUserId(XacmlContextAttributes subjectAttributes);

        /// <summary>
        /// Gets the party id from the XacmlContextRequest subject attribute
        /// </summary>
        /// <param name="subjectAttributes">The Xacml Context Request subject attributes</param>
        /// <returns>The party id of the subject</returns>
        public int GetSubjectPartyId(XacmlContextAttributes subjectAttributes);

        /// <summary>
        /// Gets a XacmlResourceAttributes model from the XacmlContextRequest
        /// </summary>
        /// <param name="request">The Xacml Context Request</param>
        /// <returns>XacmlResourceAttributes model</returns>
        public XacmlResourceAttributes GetResourceAttributes(XacmlContextRequest request);

        /// <summary>
        /// Gets a the Action string from the XacmlContextRequest
        /// </summary>
        /// <param name="request">The Xacml Context Request</param>
        /// <returns>Action attribute string value</returns>
        public string GetActionString(XacmlContextRequest request);
    }
}

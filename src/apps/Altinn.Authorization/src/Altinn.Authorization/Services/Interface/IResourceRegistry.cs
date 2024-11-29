using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Models.Register;
using Altinn.Authorization.Models.ResourceRegistry;

namespace Altinn.Platform.Authorization.Services.Interface;

/// <summary>
/// Interface for resource registry
/// </summary>
public interface IResourceRegistry
{
    /// <summary>
    /// Returns the service resource based on the resourceId, if it exists.
    /// </summary>
    /// <param name="resourceId">the policyid</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>ServiceResource</returns>
    Task<ServiceResource> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a policy based on the resourceId
    /// </summary>
    /// <param name="resourceId">the policyid</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>XacmlPolicy</returns>
    Task<XacmlPolicy> GetResourcePolicyAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all memberships a given party has access to through access lists, for a given resource.
    /// </summary>
    /// <param name="partyUrn">Urn identifying the party</param>
    /// <param name="resourceIdUrn">Urn identifying the resource</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>List of memberships</returns>
    Task<IEnumerable<AccessListResourceMembershipWithActionFilterDto>> GetMembershipsForResourceForParty(PartyUrn partyUrn, ResourceIdUrn resourceIdUrn, CancellationToken cancellationToken = default);
}

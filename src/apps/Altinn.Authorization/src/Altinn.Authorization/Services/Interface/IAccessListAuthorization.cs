using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Authorization.Models;

namespace Altinn.Platform.Authorization.Services.Interface;

/// <summary>
/// The service used to map internal delegation change to delegation change events and push them to the event queue.
/// </summary>
public interface IAccessListAuthorization
{
    /// <summary>
    /// Authorization of a given party for a resource, through RRR access lists
    /// </summary>
    /// <param name="request">Accesslist authorization request model</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>Boolean whether the access list authorization check passes or not</returns>
    public Task<Result<AccessListAuthorizationResponse>> Authorize(AccessListAuthorizationRequest request, CancellationToken cancellationToken = default);
}

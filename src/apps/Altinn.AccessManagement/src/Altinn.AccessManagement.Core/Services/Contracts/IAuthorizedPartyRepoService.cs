using Altinn.AccessManagement.Core.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Services.Contracts;

/// <summary>
/// Interface for managing authorized party connection lookup
/// </summary>
public interface IAuthorizedPartyRepoService
{
    /// <summary>
    /// Gets the authorized party connections provided to the specified entity.
    /// </summary>
    /// <param name="toId">The identifier of the entity access has been provided to.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of authorized party connections.</returns>
    Task<Result<IEnumerable<AuthorizedParty>>> Get(Guid toId, CancellationToken cancellationToken = default);
}

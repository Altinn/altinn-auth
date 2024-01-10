#nullable enable

using Altinn.ResourceRegistry.JsonPatch;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.ResourceRegistry.Controllers;

/// <summary>
/// Controller for access lists apis.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("resourceregistry/api/v1/access-lists")]
public class AccessListsController 
    : Controller
{
    /// <summary>
    /// Get all access lists for a given resource owner.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated set of <see cref="AccessListInfoDto"/></returns>
    [HttpGet("{owner:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    public Task<ActionResult<Paginated<AccessListInfoDto>>> GetAccessListsByOwner(string owner, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets an access list by owner and identifier.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="AccessListInfoDto"/></returns>
    [HttpGet("{owner:required}/{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    public Task<ActionResult<AccessListInfoDto>> GetAccessList(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create or update an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="model">Information about the access list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="AccessListInfoDto"/></returns>
    [HttpPut("{owner:required}/{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    public Task<ActionResult<AccessListInfoDto>> UpsertAccessList(string owner, string identifier, [FromBody] CreateAccessListModel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Update an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="patch">The patch document containing what to update</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="AccessListInfoDto"/></returns>
    [HttpPatch("{owner:required}/{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    [Consumes("application/json-patch+json")]
    public Task<ActionResult<AccessListInfoDto>> UpdateAccessList(string owner, string identifier, [FromBody] JsonPatchDocument patch, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get access list members.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpGet("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    public Task<ActionResult<Paginated<AccessListMembershipDto>>> GetAccessListMembers(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Replace access list members.
    /// </summary>
    /// <remarks>
    /// This effectively overwrites all members with the ones included in the request. It should not be used for lists with more than 100 members.
    /// </remarks>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="members">The new members-list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpPut("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    public Task<ActionResult<Paginated<AccessListMembershipDto>>> ReplaceAccessListMembers(string owner, string identifier, [FromBody] UpsertAccessListPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        if (members.Count > 100)
        {
            ActionResult<Paginated<AccessListMembershipDto>> result = BadRequest("Cannot replace more than 100 members at a time. Use POST and DELETE methods instead.");
            return Task.FromResult(result);
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Add new members to an access list.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a member already exists, it will not be added again.
    /// </remarks>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="members">The new members-list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpPost("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    public Task<ActionResult<Paginated<AccessListMembershipDto>>> AddAccessListMembers(string owner, string identifier, [FromBody] UpsertAccessListPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Remove members from an access list if they exist.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a member does not exist, it will not be removed.
    /// </remarks>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="members">The new members-list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpDelete("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    public Task<ActionResult<Paginated<AccessListMembershipDto>>> RemoveAccessListMembers(string owner, string identifier, [FromBody] UpsertAccessListPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get all resource connections for an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A parinated list of <see cref="AccessListResourceConnectionDto"/></returns>
    [HttpGet("{owner:required}/{identifier:required}/resource-connections")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    public Task<ActionResult<Paginated<AccessListResourceConnectionDto>>> GetAccessListResourceConnections(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates or update a resource connection to an access list.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a resource connection already exists, it will be updated.
    /// </remarks>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="model">The resource connection info</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The newly created/updated <see cref="AccessListResourceConnectionDto"/></returns>
    [HttpPut("{owner:required}/{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    public Task<ActionResult<AccessListResourceConnectionDto>> UpsertAccessListResourceConnection(string owner, string identifier, string resourceIdentifier, [FromBody] UpsertAccessListResourceConnectionDto model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes a resource connection from an access list if it exists.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a resource connection does not exist, it will not be removed.
    /// </remarks>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The newly removed <see cref="AccessListResourceConnectionDto"/>, if it existed, otherwize returns no content</returns>
    [HttpDelete("{owner:required}/{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    [SwaggerResponse(StatusCodes.Status200OK, description: "The resource connection was removed", type: typeof(AccessListResourceConnectionDto))]
    [SwaggerResponse(StatusCodes.Status204NoContent, description: "The resource connection did not exist")]
    public Task<ActionResult<AccessListResourceConnectionDto?>> DeleteAccessListResourceConnection(string owner, string identifier, string resourceIdentifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
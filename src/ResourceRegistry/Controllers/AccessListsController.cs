#nullable enable

using System.Net;
using Altinn.ResourceRegistry.Auth;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.JsonPatch;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Altinn.ResourceRegistry.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.ResourceRegistry.Controllers;

/// <summary>
/// Controller for access lists apis.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("resourceregistry/api/v1/access-lists/{owner:required}")]
[NotImplementedFilter]
[ResourceOwnerFromRouteValue("owner")]
[Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_READ)]
public class AccessListsController 
    : Controller
{
    /// <summary>
    /// Route name for <see cref="GetAccessListsByOwner"/>.
    /// </summary>
    public const string ROUTE_GET_BY_OWNER = "access-lists/get-by-owner";

    /// <summary>
    /// Route name for <see cref="GetAccessListResourceConnections"/>.
    /// </summary>
    public const string ROUTE_GET_RESOURCE_CONNECTIONS = "access-lists/get-resource-connections";

    private readonly IAccessListService _service;

    /// <summary>
    /// Constructs a new <see cref="AccessListsController"/>.
    /// </summary>
    /// <param name="service">A <see cref="IAccessListService"/></param>
    public AccessListsController(IAccessListService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get all access lists for a given resource owner.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="token">Optional continuation token</param>
    /// <param name="include">What additional information to include in the response</param>
    /// <param name="resourceIdentifier">
    /// Optional resource identifier. Required if <paramref name="include"/> has flag <see cref="AccessListIncludes.ResourceConnections"/>
    /// set. This is used to filter the resource connections included in the access lists to only the provided resource.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated set of <see cref="AccessListInfoDto"/></returns>
    [HttpGet("", Name = ROUTE_GET_BY_OWNER)]
    [SwaggerOperation(Tags = ["Access List"])]
    public async Task<ActionResult<Paginated<AccessListInfoDto>>> GetAccessListsByOwner(
        string owner,
        [FromQuery(Name = "token")] Opaque<string>? token = null,
        [FromQuery(Name = "include")] AccessListIncludes include = AccessListIncludes.None,
        [FromQuery(Name = "resource")] string? resourceIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        if (include.HasFlag(AccessListIncludes.ResourceConnections) && string.IsNullOrWhiteSpace(resourceIdentifier))
        {
            ModelState.AddModelError("resource", "Resource identifier is required when including resource connections");
            return BadRequest(ModelState);
        }

        var page = await _service.GetAccessListsByOwner(owner, Page.ContinueFrom(token?.Value), include, resourceIdentifier, cancellationToken);
        if (page == null)
        {
            return NotFound();
        }

        var nextLink = page.ContinuationToken.HasValue
            ? Url.Link(ROUTE_GET_BY_OWNER, new
            {
                owner,
                token = Opaque.Create(page.ContinuationToken.Value),
                includes = AccessListIncludesModelBinder.Stringify(include),
            })
            : null;

        return Paginated.Create(page.Items.Select(AccessListInfoDto.From), nextLink);
    }

    /// <summary>
    /// Gets an access list by owner and identifier.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="conditions">Request conditions</param>
    /// <param name="includes">What additional information to include in the response</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>An <see cref="AccessListInfoDto"/></returns>
    [HttpGet("{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    public async Task<ConditionalResult<AccessListInfoDto, AggregateVersion>> GetAccessList(
        string owner, 
        string identifier, 
        RequestConditionCollection<AggregateVersion> conditions,
        [FromQuery(Name = "include")] AccessListIncludes includes = AccessListIncludes.None,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAccessList(owner, identifier, includes, conditions.Select(v => v.Version), cancellationToken);

        return result.Select(AccessListInfoDto.From, AggregateVersion.From);
    }

    /// <summary>
    /// Deletes an access list by owner and identifier.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="conditions">Request conditions</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>An <see cref="AccessListInfoDto"/></returns>
    [HttpDelete("{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    [SwaggerResponse(StatusCodes.Status200OK, description: "The list was deleted", type: typeof(ConditionalResult<AccessListInfoDto, AggregateVersion>))]
    [SwaggerResponse(StatusCodes.Status204NoContent, description: "The access list did not exist or was already deleted")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<AccessListInfoDto, AggregateVersion>> DeleteAccessList(
        string owner,
        string identifier,
        RequestConditionCollection<AggregateVersion> conditions,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAccessList(owner, identifier, conditions.Select(v => v.Version), cancellationToken);

        if (result.IsNotFound)
        {
            return NoContent();
        }

        return result.Select(AccessListInfoDto.From, AggregateVersion.From);
    }

    /// <summary>
    /// Create or update an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="model">Information about the access list</param>
    /// <param name="conditions">Request conditions</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="AccessListInfoDto"/></returns>
    [HttpPut("{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<AccessListInfoDto, AggregateVersion>> UpsertAccessList(
        string owner, 
        string identifier, 
        [FromBody] CreateAccessListModel model,
        RequestConditionCollection<AggregateVersion> conditions,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateOrUpdateAccessList(
            owner,
            identifier,
            model.Name,
            model.Description ?? string.Empty,
            conditions.Select(v => v.Version),
            cancellationToken);

        return result.Select(AccessListInfoDto.From, AggregateVersion.From);
    }

    /// <summary>
    /// Update an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="patch">The patch document containing what to update</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="AccessListInfoDto"/></returns>
    [HttpPatch("{identifier:required}")]
    [SwaggerOperation(Tags = ["Access List"])]
    [Consumes("application/json-patch+json")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ActionResult<AccessListInfoDto>> UpdateAccessList(string owner, string identifier, [FromBody] JsonPatchDocument patch, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        throw new NotImplementedException();
    }

    /// <summary>
    /// Get access list members.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpGet("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    public async Task<ActionResult<Paginated<AccessListMembershipDto>>> GetAccessListMembers(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

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
    [HttpPut("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ActionResult<Paginated<AccessListMembershipDto>>> ReplaceAccessListMembers(string owner, string identifier, [FromBody] UpsertAccessListPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        if (members.Count > 100)
        {
            return BadRequest("Cannot replace more than 100 members at a time. Use POST and DELETE methods instead.");
        }

        await Task.Yield();

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
    [HttpPost("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ActionResult<Paginated<AccessListMembershipDto>>> AddAccessListMembers(string owner, string identifier, [FromBody] UpsertAccessListPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

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
    [HttpDelete("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ActionResult<Paginated<AccessListMembershipDto>>> RemoveAccessListMembers(string owner, string identifier, [FromBody] UpsertAccessListPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        throw new NotImplementedException();
    }

    /// <summary>
    /// Get all resource connections for an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="requestConditions">Request conditions</param>
    /// <param name="token">Optional continuation token</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A parinated list of <see cref="AccessListResourceConnectionDto"/></returns>
    [HttpGet("{identifier:required}/resource-connections", Name = ROUTE_GET_RESOURCE_CONNECTIONS)]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    public async Task<ConditionalResult<VersionedPaginated<AccessListResourceConnectionDto, AggregateVersion>, AggregateVersion>> GetAccessListResourceConnections(
        string owner,
        string identifier,
        RequestConditionCollection<AggregateVersion> requestConditions,
        [FromQuery(Name = "token")] Opaque<AccessListResourceConnectionContinuationToken>? token = null,
        CancellationToken cancellationToken = default)
    {
        IVersionedEntityCondition<AggregateVersion> conditions = requestConditions;
        if (token?.Value.Version is { } version)
        {
            conditions = conditions.Concat(RequestCondition.IsMatch(AggregateVersion.From(version)));
        }

        var result = await _service.GetAccessListResourceConnections(
            owner,
            identifier,
            Page.ContinueFrom(token?.Value.ContinueFrom),
            conditions.Select(v => v.Version),
            cancellationToken);

        return result.Select(
            page =>
            {
                var nextLink = page.ContinuationToken.HasValue
                    ? Url.Link(ROUTE_GET_RESOURCE_CONNECTIONS, new
                    {
                        owner,
                        identifier,
                        token = Opaque.Create(new AccessListResourceConnectionContinuationToken(page.Version, page.ContinuationToken.Value)),
                    })
                    : null;

                return Paginated.Create(page.Items.Select(AccessListResourceConnectionDto.From), nextLink)
                    .WithVersion(page.ModifiedAt, AggregateVersion.From(page.Version));
            },
            AggregateVersion.From);
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
    [HttpPut("{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ActionResult<AccessListResourceConnectionDto>> UpsertAccessListResourceConnection(string owner, string identifier, string resourceIdentifier, [FromBody] UpsertAccessListResourceConnectionDto model, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

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
    [HttpDelete("{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    [SwaggerResponse(StatusCodes.Status200OK, description: "The resource connection was removed", type: typeof(AccessListResourceConnectionDto))]
    [SwaggerResponse(StatusCodes.Status204NoContent, description: "The resource connection did not exist")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ActionResult<AccessListResourceConnectionDto?>> DeleteAccessListResourceConnection(string owner, string identifier, string resourceIdentifier, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        throw new NotImplementedException();
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    private sealed class NotImplementedFilterAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is NotImplementedException)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.NotImplemented);
            }
        }
    }

    /// <summary>
    /// Continuation token for access list resource connections.
    /// </summary>
    /// <param name="Version">The access list version.</param>
    /// <param name="ContinueFrom">What resource identifier to continue from.</param>
    public record AccessListResourceConnectionContinuationToken(ulong Version, string ContinueFrom);
}

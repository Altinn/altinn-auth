#nullable enable

using System.Diagnostics;
using Altinn.Authorization.ProblemDetails;
using Altinn.ResourceRegistry.Auth;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Filters;
using Altinn.ResourceRegistry.JsonPatch;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Altinn.ResourceRegistry.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
[OwnerMustBeOrgcodeFilter]
public class AccessListsController 
    : ControllerBase
{
    /// <summary>
    /// Route name for <see cref="GetAccessListsByOwner"/>.
    /// </summary>
    public const string ROUTE_GET_BY_OWNER = "access-lists/get-by-owner";

    /// <summary>
    /// Route name for <see cref="GetAccessListResourceConnections"/>.
    /// </summary>
    public const string ROUTE_GET_RESOURCE_CONNECTIONS = "access-lists/get-resource-connections";

    /// <summary>
    /// Route name for <see cref="GetAccessListMembers"/>.
    /// </summary>
    public const string ROUTE_GET_MEMBERS = "access-lists/get-members";

    private readonly IAccessListService _service;
    private readonly IResourceRegistry _resources;
    private readonly IAuthorizationService _authorization;

    /// <summary>
    /// Constructs a new <see cref="AccessListsController"/>.
    /// </summary>
    public AccessListsController(IAccessListService service, IAuthorizationService authorization, IResourceRegistry resources)
    {
        _service = service;
        _authorization = authorization;
        _resources = resources;
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
        ValidationErrorBuilder errors = default;

        if (include.HasFlag(AccessListIncludes.Members))
        {
            return Problems.AccessList_IncludeMembers_NotImplemented.ToActionResult();
        }

        if (include.HasFlag(AccessListIncludes.ResourceConnections) && string.IsNullOrWhiteSpace(resourceIdentifier))
        {
            errors.Add(ValidationErrors.AccessList_IncludeResourceConnections_MissingResourceIdentifier, [
                "/$QUERY/include",
                "/$QUERY/resource",
            ]);
        }

        if (errors.TryToActionResult(out var errorResult))
        {
            return errorResult;
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
    /// <remarks>This method is not implemented yet. See the put method instead.</remarks>
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
    /// <param name="requestConditions">Request conditions</param>
    /// <param name="token">Optional continuation token</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpGet("{identifier:required}/members", Name = ROUTE_GET_MEMBERS)]
    [SwaggerOperation(Tags = ["Access List Members"])]
    public async Task<ConditionalResult<VersionedPaginated<AccessListMembershipDto, AggregateVersion>, AggregateVersion>> GetAccessListMembers(
        string owner,
        string identifier,
        RequestConditionCollection<AggregateVersion> requestConditions,
        [FromQuery(Name = "token")] Opaque<AccessListMembersContinuationToken>? token = null,
        CancellationToken cancellationToken = default)
    {
        IVersionedEntityCondition<AggregateVersion> conditions = requestConditions;
        if (token?.Value.Version is { } version)
        {
            conditions = conditions.Concat(RequestCondition.IsMatch(AggregateVersion.From(version)));
        }

        var result = await _service.GetAccessListMembers(
            owner,
            identifier,
            Page.ContinueFrom(token?.Value.ContinueFrom),
            conditions.Select(v => v.Version),
            cancellationToken);

        return result.Select(
            page =>
            {
                var nextLink = page.ContinuationToken.HasValue
                    ? Url.Link(ROUTE_GET_MEMBERS, new
                    {
                        owner,
                        identifier,
                        token = Opaque.Create(new AccessListMembersContinuationToken(page.Version, page.ContinuationToken.Value)),
                    })
                    : null;

                return Paginated.Create(page.Items.Select(AccessListMembershipDto.From), nextLink)
                    .WithVersion(page.ModifiedAt, AggregateVersion.From(page.Version));
            },
            AggregateVersion.From);
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
    /// <param name="conditions">Request conditions</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpPut("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<VersionedPaginated<AccessListMembershipDto, AggregateVersion>, AggregateVersion>> ReplaceAccessListMembers(
        string owner, 
        string identifier, 
        [FromBody] UpsertAccessListPartyMembersListDto members,
        RequestConditionCollection<AggregateVersion> conditions,
        CancellationToken cancellationToken = default)
    {
        if (members.Count > 100)
        {
            return new AltinnValidationProblemDetails([
                ValidationErrors.AccessList_ReplaceMembers_TooMany.ToValidationError("/data"),
            ]).ToActionResult();
        }

        var result = await _service.ReplaceAccessListMembers(
            owner,
            identifier,
            members,
            conditions.Select(v => v.Version),
            cancellationToken);

        if (result.IsNotFound && result.NotFoundType == nameof(PartyUrn))
        {
            return Problems.PartyReference_NotFound.ToActionResult();
        }

        return result.Select(
            page =>
            {
                var nextLink = page.ContinuationToken.HasValue
                    ? Url.Link(ROUTE_GET_MEMBERS, new
                    {
                        owner,
                        identifier,
                        token = Opaque.Create(new AccessListMembersContinuationToken(page.Version, page.ContinuationToken.Value)),
                    })
                    : null;

                return Paginated.Create(page.Items.Select(AccessListMembershipDto.From), nextLink)
                    .WithVersion(page.ModifiedAt, AggregateVersion.From(page.Version));
            },
            AggregateVersion.From);
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
    /// <param name="conditions">Request conditions</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpPost("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<VersionedPaginated<AccessListMembershipDto, AggregateVersion>, AggregateVersion>> AddAccessListMembers(
        string owner,
        string identifier,
        [FromBody] UpsertAccessListPartyMembersListDto members,
        RequestConditionCollection<AggregateVersion> conditions,
        CancellationToken cancellationToken = default)
    {
        if (members.Count > 100)
        {
            return new AltinnValidationProblemDetails([
                ValidationErrors.AccessList_AddRemoveMembers_TooMany.ToValidationError("/data"),
            ]).ToActionResult();
        }

        var result = await _service.AddAccessListMembers(
            owner,
            identifier,
            members,
            conditions.Select(v => v.Version),
            cancellationToken);

        if (result.IsNotFound && result.NotFoundType == nameof(PartyUrn))
        {
            return Problems.PartyReference_NotFound.ToActionResult();
        }

        return result.Select(
            page =>
            {
                var nextLink = page.ContinuationToken.HasValue
                    ? Url.Link(ROUTE_GET_MEMBERS, new
                    {
                        owner,
                        identifier,
                        token = Opaque.Create(new AccessListMembersContinuationToken(page.Version, page.ContinuationToken.Value)),
                    })
                    : null;

                return Paginated.Create(page.Items.Select(AccessListMembershipDto.From), nextLink)
                    .WithVersion(page.ModifiedAt, AggregateVersion.From(page.Version));
            },
            AggregateVersion.From);
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
    /// <param name="conditions">Request conditions</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListMembershipDto"/></returns>
    [HttpDelete("{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Access List Members"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<VersionedPaginated<AccessListMembershipDto, AggregateVersion>, AggregateVersion>> RemoveAccessListMembers(
        string owner,
        string identifier,
        [FromBody] UpsertAccessListPartyMembersListDto members,
        RequestConditionCollection<AggregateVersion> conditions,
        CancellationToken cancellationToken = default)
    {
        if (members.Count > 100)
        {
            return new AltinnValidationProblemDetails([
                ValidationErrors.AccessList_AddRemoveMembers_TooMany.ToValidationError("/data"),
            ]).ToActionResult();
        }

        var result = await _service.RemoveAccessListMembers(
            owner,
            identifier,
            members,
            conditions.Select(v => v.Version),
            cancellationToken);

        if (result.IsNotFound && result.NotFoundType == nameof(PartyUrn))
        {
            return Problems.PartyReference_NotFound.ToActionResult();
        }

        return result.Select(
            page =>
            {
                var nextLink = page.ContinuationToken.HasValue
                    ? Url.Link(ROUTE_GET_MEMBERS, new
                    {
                        owner,
                        identifier,
                        token = Opaque.Create(new AccessListMembersContinuationToken(page.Version, page.ContinuationToken.Value)),
                    })
                    : null;

                return Paginated.Create(page.Items.Select(AccessListMembershipDto.From), nextLink)
                    .WithVersion(page.ModifiedAt, AggregateVersion.From(page.Version));
            },
            AggregateVersion.From);
    }

    /// <summary>
    /// Get all resource connections for an access list.
    /// </summary>
    /// <param name="owner">The resource owner</param>
    /// <param name="identifier">The resource owner-unique identifier</param>
    /// <param name="requestConditions">Request conditions</param>
    /// <param name="token">Optional continuation token</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="AccessListResourceConnectionDto"/></returns>
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
    /// <param name="conditions">Request conditions</param>
    /// <param name="model">The resource connection info</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The newly created/updated <see cref="AccessListResourceConnectionDto"/></returns>
    [HttpPut("{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<AccessListResourceConnectionWithVersionDto, AggregateVersion>> UpsertAccessListResourceConnection(
        string owner,
        string identifier,
        string resourceIdentifier,
        RequestConditionCollection<AggregateVersion> conditions,
        [FromBody] UpsertAccessListResourceConnectionDto model,
        CancellationToken cancellationToken = default)
    {
        var resourceOwnerResult = await _resources.GetResourceOwner(resourceIdentifier, cancellationToken);
        if (resourceOwnerResult.IsProblem)
        {
            return resourceOwnerResult.Problem.ToActionResult();
        }

        var resourceOwner = resourceOwnerResult.Value;
        var authorizationResult = await _authorization.AuthorizeAsync(User, new ResourceOwner(resourceOwner), UserOwnsResourceRequirement.Instance);
        if (!authorizationResult.Succeeded)
        {
            return Problems.AccessList_References_OtherServiceOwners_Resource.ToActionResult();
        }

        var result = await _service.UpsertAccessListResourceConnection(
            owner,
            identifier,
            resourceIdentifier,
            model.ActionFilters ?? [],
            conditions.Select(v => v.Version),
            cancellationToken);

        return result.Select(AccessListResourceConnectionWithVersionDto.From, AggregateVersion.From);
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
    /// <param name="conditions">Request conditions</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The newly removed <see cref="AccessListResourceConnectionDto"/>, if it existed, otherwise returns no content</returns>
    [HttpDelete("{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Access List Resource Connections"])]
    [SwaggerResponse(StatusCodes.Status200OK, description: "The resource connection was removed", type: typeof(ConditionalResult<AccessListResourceConnectionWithVersionDto, AggregateVersion>))]
    [SwaggerResponse(StatusCodes.Status204NoContent, description: "The resource connection did not exist")]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_LIST_WRITE)]
    public async Task<ConditionalResult<AccessListResourceConnectionWithVersionDto, AggregateVersion>> DeleteAccessListResourceConnection(
        string owner, 
        string identifier, 
        string resourceIdentifier,
        RequestConditionCollection<AggregateVersion> conditions,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAccessListResourceConnection(owner, identifier, resourceIdentifier, conditions.Select(v => v.Version), cancellationToken);

        if (result.IsNotFound && result.NotFoundType == nameof(AccessListResourceConnection))
        {
            return NoContent();
        }

        return result.Select(AccessListResourceConnectionWithVersionDto.From, AggregateVersion.From);
    }

    /// <summary>
    /// Continuation token for access list resource connections.
    /// </summary>
    /// <param name="Version">The access list version.</param>
    /// <param name="ContinueFrom">What resource identifier to continue from.</param>
    public sealed record AccessListResourceConnectionContinuationToken(ulong Version, string ContinueFrom);

    /// <summary>
    /// Continuation token for access list members.
    /// </summary>
    /// <param name="Version">The access list version.</param>
    /// <param name="ContinueFrom">What member to continue from.</param>
    public sealed record AccessListMembersContinuationToken(ulong Version, Guid ContinueFrom);

    private sealed record ResourceOwner(CompetentAuthorityReference Owner)
        : IHasResourceOwner
    {
        string IHasResourceOwner.ResourceOwner => Owner.Orgcode;
    }
}

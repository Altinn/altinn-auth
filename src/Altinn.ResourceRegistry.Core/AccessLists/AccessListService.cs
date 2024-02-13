#nullable enable

using System.Data;
using System.Diagnostics;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Implementation of <see cref="IAccessListService"/>.
/// </summary>
internal class AccessListService
    : IAccessListService
{
    private const int SMALL_PAGE_SIZE = 20;
    private const int LARGE_PAGE_SIZE = 100;

    private readonly IAccessListsRepository _repository;

    /// <summary>
    /// Constructs a new instance of <see cref="AccessListService"/>.
    /// </summary>
    /// <param name="repository">A <see cref="IAccessListsRepository"/></param>
    public AccessListService(IAccessListsRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Page<AccessListInfo, string>> GetAccessListsByOwner(
        string owner,
        Page<string>.Request request,
        AccessListIncludes includes = default,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(request);

        // request 1 more than page size to determine if there are more pages
        var accessLists = await _repository.GetAccessListsByOwner(
            owner,
            continueFrom: request.ContinuationToken,
            count: SMALL_PAGE_SIZE + 1,
            includes,
            cancellationToken);

        return Page.Create(accessLists, SMALL_PAGE_SIZE, static list => list.Identifier);
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> GetAccessList(
        string owner,
        string identifier,
        AccessListIncludes includes = default,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);

        var accessList = await _repository.LookupInfo(owner, identifier, includes, cancellationToken);
        if (accessList is null)
        {
            return Conditional.NotFound();
        }

        if (condition is not null)
        {
            var result = condition.Validate(accessList);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            if (result == VersionedEntityConditionResult.Unmodified)
            {
                return Conditional.Unmodified(accessList.Version, accessList.UpdatedAt);
            }
        }

        return accessList;
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> DeleteAccessList(
        string owner,
        string identifier,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);
        if (aggregate is null)
        {
            return Conditional.NotFound();
        }

        if (condition is not null)
        {
            var info = aggregate.AsAccessListInfo();
            var result = condition.Validate(info);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when deleting");
        }

        aggregate.Delete();
        await aggregate.SaveChanged(cancellationToken);

        return aggregate.AsAccessListInfo();
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> CreateOrUpdateAccessList(
        string owner,
        string identifier,
        string name,
        string description,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(name);
        Guard.IsNotNull(description);

        // First check if we're allowed to create a new list. This has to happen first,
        // because the list needs to be created in the same transaction that did not find
        // it to avoid race conditions.
        var allowCreate = condition?.AllowsCreatingNewEntity() ?? true;

        IAccessListAggregate aggregate;
        if (!allowCreate)
        {
            // This is effectively just an update, not an upsert at this point.
            var existing = await _repository.LoadAccessList(owner, identifier, cancellationToken);
            if (existing is null)
            {
                // If the aggregate was not found, we're not allowed to create it by a condition.
                return Conditional.ConditionFailed();
            }

            aggregate = existing;
        }
        else
        {
            var result = await _repository.LoadOrCreateAccessList(owner, identifier, name, description, cancellationToken);
            aggregate = result.Aggregate;

            // If the list was created, we can simply return it.
            if (result.IsNew)
            {
                Debug.Assert(aggregate.Name == name, "The name should have been set on the aggregate");
                Debug.Assert(aggregate.Description == description, "The description should have been set on the aggregate");

                // The aggregate was just created (and saved), and we already checked
                // that the condition was met, so we can return it as is.
                return aggregate!.AsAccessListInfo();
            }
        }
        
        // If we had an existing list - we need to validate it against the condition.
        if (condition is not null)
        {
            var info = aggregate.AsAccessListInfo();
            var result = condition.Validate(info);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when creating");
        }

        // If the name and/or description is different in the list from what was requested, we need to update it.
        var newName = aggregate.Name == name ? null : name;
        var newDescription = aggregate.Description == description ? null : description;

        if (newName is not null || newDescription is not null)
        {
            aggregate.Update(name: newName, description: newDescription);
            await aggregate.SaveChanged(cancellationToken);
        }

        // Return the maybe updated list.
        return aggregate.AsAccessListInfo();
    }

    /// <inheritdoc/>
    public async Task<Conditional<VersionedPage<AccessListResourceConnection, string, ulong>, ulong>> GetAccessListResourceConnections(
        string owner,
        string identifier,
        Page<string>.Request request,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(request);

        var data = await _repository.GetAccessListResourceConnections(
            owner,
            identifier,
            continueFrom: request.ContinuationToken,
            count: LARGE_PAGE_SIZE + 1,
            includeActions: true,
            cancellationToken);

        if (data is null)
        {
            return Conditional.NotFound();
        }

        if (condition is not null)
        {
            var result = condition.Validate(data);

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            if (result == VersionedEntityConditionResult.Unmodified)
            {
                return Conditional.Unmodified(data.Version, data.UpdatedAt);
            }
        }

        return Page.Create(data.Value, LARGE_PAGE_SIZE, static resource => resource.ResourceIdentifier)
            .WithVersion(data.UpdatedAt, data.Version);
    }
}

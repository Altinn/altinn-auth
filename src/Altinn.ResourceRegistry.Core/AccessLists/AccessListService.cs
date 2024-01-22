#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Implementation of <see cref="IAccessListService"/>.
/// </summary>
internal class AccessListService
    : IAccessListService
{
    private const int LISTS_PAGE_SIZE = 20;

    private readonly IAccessListsRepository _repository;

    /// <summary>
    /// Constructs a new instance of <see cref="AccessListService"/>.
    /// </summary>
    /// <param name="repository">A <see cref="IAccessListsRepository"/></param>
    public AccessListService(IAccessListsRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets access lists by owner, limited by <see cref="LISTS_PAGE_SIZE"/> and optionally starting from <paramref name="request"/>.ContinuationToken.
    /// </summary>
    /// <param name="owner">The resource owner.</param>
    /// <param name="request">The page request metadata.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Page{TItem, TToken}"/> of <see cref="AccessListInfo"/></returns>
    public async Task<Page<AccessListInfo, string>> GetAccessListsByOwner(
        string owner,
        Page<string>.Request request,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(request);

        // request 1 more than page size to determine if there are more pages
        var accessLists = await _repository.GetAccessListsByOwner(
            owner,
            continueFrom: request.ContinuationToken,
            count: LISTS_PAGE_SIZE + 1,
            cancellationToken);

        return Page.Create(accessLists, LISTS_PAGE_SIZE, static list => list.Identifier);
    }
}

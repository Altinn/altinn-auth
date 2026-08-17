using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Searches entities by name, organization number, username, party id, or person identifier.
/// </summary>
public sealed class EntitySearchService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>Default maximum number of hits returned; the search page tells the user to narrow the search.</summary>
    public const int MaxResults = 50;

    public async Task<List<Entity>> SearchAsync(string environment, string term, bool includeDeleted, int maxResults = MaxResults, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var query = db.Entities
            .AsNoTracking()
            .Include(e => e.Type)
            .Include(e => e.Variant)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(e => !e.IsDeleted);
        }

        query = query.Where(e =>
            EF.Functions.ILike(e.Name, $"%{term}%") ||
            e.OrganizationIdentifier == term ||
            (e.Username != null && EF.Functions.ILike(e.Username, $"%{term}%")) ||
            (e.PartyId != null && e.PartyId.ToString() == term) ||
            (e.PersonIdentifier != null && e.PersonIdentifier.StartsWith(term)));

        return await query
            .OrderBy(e => e.Name)
            .Take(maxResults)
            .ToListAsync(ct);
    }
}

using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// The specific entity field a search term is matched against.
/// </summary>
public enum EntitySearchKind
{
    /// <summary>Exact match on Entity.Id.</summary>
    Uuid,

    /// <summary>Exact match on OrganizationIdentifier (9 digits).</summary>
    OrganizationNumber,

    /// <summary>Exact match on PersonIdentifier (11 digits).</summary>
    PersonIdentifier,

    /// <summary>Exact match on PartyId.</summary>
    PartyId,

    /// <summary>Substring match on Name or Username.</summary>
    Name,
}

/// <summary>
/// Looks up entities by matching the term against one specific field — uuid, organization
/// number, person identifier, party id, or name — so exact lookups hit the unique indexes
/// instead of scanning with a combined OR predicate.
/// </summary>
public sealed class EntitySearchService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>Default maximum number of hits returned; the search page tells the user to narrow the search.</summary>
    public const int MaxResults = 50;

    /// <summary>
    /// Detects which field a term should be matched against: parseable guid = uuid,
    /// 11 digits = person identifier, 9 digits = organization number, other digits-only
    /// strings = party id, anything else = name.
    /// </summary>
    public static EntitySearchKind DetectKind(string term)
    {
        term = Normalize(term);

        if (Guid.TryParse(term, out _))
        {
            return EntitySearchKind.Uuid;
        }

        if (term.Length > 0 && term.All(char.IsAsciiDigit))
        {
            return term.Length switch
            {
                11 => EntitySearchKind.PersonIdentifier,
                9 => EntitySearchKind.OrganizationNumber,
                _ when int.TryParse(term, out _) => EntitySearchKind.PartyId,
                _ => EntitySearchKind.Name,
            };
        }

        return EntitySearchKind.Name;
    }

    /// <summary>
    /// Searches entities in the environment. When <paramref name="kind"/> is null the term
    /// type is auto-detected with <see cref="DetectKind"/>.
    /// </summary>
    public async Task<List<Entity>> SearchAsync(
        string environment,
        string term,
        bool includeDeleted,
        EntitySearchKind? kind = null,
        int maxResults = MaxResults,
        CancellationToken ct = default)
    {
        term = Normalize(term);
        kind ??= DetectKind(term);

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

        query = kind switch
        {
            EntitySearchKind.Uuid => Guid.TryParse(term, out var id)
                ? query.Where(e => e.Id == id)
                : query.Where(e => false),
            EntitySearchKind.OrganizationNumber => query.Where(e => e.OrganizationIdentifier == term),
            EntitySearchKind.PersonIdentifier => query.Where(e => e.PersonIdentifier == term),
            EntitySearchKind.PartyId => int.TryParse(term, out var partyId)
                ? query.Where(e => e.PartyId == partyId)
                : query.Where(e => false),
            _ => query.Where(e =>
                EF.Functions.ILike(e.Name, $"%{term}%") ||
                (e.Username != null && EF.Functions.ILike(e.Username, $"%{term}%"))),
        };

        return await query
            .OrderBy(e => e.Name)
            .Take(maxResults)
            .ToListAsync(ct);
    }

    /// <summary>Trims the term, and strips spaces from digit groups like "123 456 789".</summary>
    private static string Normalize(string term)
    {
        term = term.Trim();

        if (term.Length > 0 && term.All(c => char.IsAsciiDigit(c) || c == ' '))
        {
            term = term.Replace(" ", string.Empty);
        }

        return term;
    }
}

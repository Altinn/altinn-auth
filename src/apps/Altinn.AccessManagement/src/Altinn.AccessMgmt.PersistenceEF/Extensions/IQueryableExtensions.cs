using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<Connection> IncludeExtendedEntities(this IQueryable<Connection> queryable)
    {
        return queryable
            .Include(c => c.From)
            .ThenInclude(c => c.Variant)
            .Include(c => c.From)
            .ThenInclude(c => c.Type)

            .Include(c => c.From)
            .ThenInclude(c => c.Parent)
            .ThenInclude(c => c.Variant)
            .Include(c => c.From)
            .ThenInclude(c => c.Parent)
            .ThenInclude(c => c.Type)

            .Include(c => c.To)
            .ThenInclude(c => c.Variant)
            .Include(c => c.To)
            .ThenInclude(c => c.Type)
            .Include(c => c.To)
            .ThenInclude(c => c.Parent)

            .Include(c => c.To)
            .ThenInclude(c => c.Parent)
            .ThenInclude(c => c.Variant)
            .Include(c => c.To)
            .ThenInclude(c => c.Parent)
            .ThenInclude(c => c.Type)

            .Include(c => c.Via)
            .ThenInclude(c => c.Variant)
            .Include(c => c.Via)
            .ThenInclude(c => c.Type);
    }
}

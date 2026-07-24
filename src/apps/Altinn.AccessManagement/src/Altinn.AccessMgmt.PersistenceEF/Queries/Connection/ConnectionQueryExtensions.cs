using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Provides extension methods for filtering connection query records based on various criteria such as IDs and roles.
/// </summary>
internal static class ConnectionQueryExtensions
{
    internal static IQueryable<ConnectionQueryBaseRecord> ToIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ToId == id);
        }

        return query.Where(t => ids.Contains(t.ToId));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> FromIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid>? ids, bool applyFromFilter = true)
    {
        if (!applyFromFilter || ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.FromId == id);
        }

        return query.Where(t => ids.Contains(t.FromId));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> ViaIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ViaId.HasValue && t.ViaId.Value == id);
        }

        return query.Where(t => t.ViaId.HasValue && ids.Contains(t.ViaId.Value));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> RoleIdExcludes(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.RoleId != id);
        }

        return query.Where(t => !ids.Contains(t.RoleId));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> RoleIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.RoleId == id);
        }

        return query.Where(t => ids.Contains(t.RoleId));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> ViaRoleIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ViaRoleId.HasValue && t.ViaRoleId == id);
        }

        return query.Where(t => t.ViaRoleId.HasValue && ids.Contains(t.ViaRoleId.Value));
    }
}

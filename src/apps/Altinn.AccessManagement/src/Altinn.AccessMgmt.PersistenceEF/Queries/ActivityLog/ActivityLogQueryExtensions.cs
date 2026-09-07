using System.Linq.Expressions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Queries;

internal static class ActivityLogQueryExtensions
{
    internal static IQueryable<ActivityLog> InvolvedIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.FromId == id || t.ToId == id || t.ViaId == id);
        }

        return query.Where(t =>
            (t.FromId.HasValue && ids.Contains(t.FromId.Value)) ||
            (t.ToId.HasValue && ids.Contains(t.ToId.Value)) ||
            (t.ViaId.HasValue && ids.Contains(t.ViaId.Value)));
    }

    internal static IQueryable<ActivityLog> AnyPartyIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.FromId == id || t.ToId == id || t.ViaId == id || t.ById == id);
        }

        return query.Where(t =>
            (t.FromId.HasValue && ids.Contains(t.FromId.Value)) ||
            (t.ToId.HasValue && ids.Contains(t.ToId.Value)) ||
            (t.ViaId.HasValue && ids.Contains(t.ViaId.Value)) ||
            (t.ById.HasValue && ids.Contains(t.ById.Value)));
    }

    internal static IQueryable<ActivityLog> ActivityTypeKeyContains(this IQueryable<ActivityLog> query, IReadOnlyCollection<ActivityTypeKey> keys)
    {
        if (keys is null || keys.Count == 0)
        {
            return query;
        }

        // Each key is a whole conjunction and the keys are OR'ed — the independent value
        // lists cannot express this without cross-matching, hence the built expression.
        var parameter = Expression.Parameter(typeof(ActivityLog), "t");
        Expression body = null;

        foreach (var key in keys)
        {
            Expression conjunction = Expression.Equal(
                Expression.Property(parameter, nameof(ActivityLog.Type)),
                Expression.Constant(key.Type));

            conjunction = Expression.AndAlso(conjunction, Expression.Equal(
                Expression.Property(parameter, nameof(ActivityLog.Subtype)),
                Expression.Constant(key.Subtype, typeof(ActivityLogSubtype?))));

            conjunction = Expression.AndAlso(conjunction, Expression.Equal(
                Expression.Property(parameter, nameof(ActivityLog.Trigger)),
                Expression.Constant(key.Trigger)));

            if (key.Status is not null)
            {
                conjunction = Expression.AndAlso(conjunction, Expression.Equal(
                    Expression.Property(parameter, nameof(ActivityLog.Status)),
                    Expression.Constant(key.Status, typeof(RequestStatus?))));
            }

            body = body is null ? conjunction : Expression.OrElse(body, conjunction);
        }

        return query.Where(Expression.Lambda<Func<ActivityLog, bool>>(body, parameter));
    }

    internal static IQueryable<ActivityLog> TypeContains(this IQueryable<ActivityLog> query, HashSet<ActivityLogType> values)
    {
        if (values is null || values.Count == 0)
        {
            return query;
        }

        if (values.Count == 1)
        {
            var value = values.First();
            return query.Where(t => t.Type == value);
        }

        return query.Where(t => values.Contains(t.Type));
    }

    internal static IQueryable<ActivityLog> SubtypeContains(this IQueryable<ActivityLog> query, HashSet<ActivityLogSubtype> values)
    {
        if (values is null || values.Count == 0)
        {
            return query;
        }

        if (values.Count == 1)
        {
            var value = values.First();
            return query.Where(t => t.Subtype == value);
        }

        return query.Where(t => t.Subtype.HasValue && values.Contains(t.Subtype.Value));
    }

    internal static IQueryable<ActivityLog> TriggerContains(this IQueryable<ActivityLog> query, HashSet<ActivityLogTrigger> values)
    {
        if (values is null || values.Count == 0)
        {
            return query;
        }

        if (values.Count == 1)
        {
            var value = values.First();
            return query.Where(t => t.Trigger == value);
        }

        return query.Where(t => values.Contains(t.Trigger));
    }

    internal static IQueryable<ActivityLog> StatusContains(this IQueryable<ActivityLog> query, HashSet<RequestStatus> values)
    {
        if (values is null || values.Count == 0)
        {
            return query;
        }

        if (values.Count == 1)
        {
            var value = values.First();
            return query.Where(t => t.Status == value);
        }

        return query.Where(t => t.Status.HasValue && values.Contains(t.Status.Value));
    }

    internal static IQueryable<ActivityLog> ByIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ById == id);
        }

        return query.Where(t => t.ById.HasValue && ids.Contains(t.ById.Value));
    }

    internal static IQueryable<ActivityLog> SourceIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.SourceId == id);
        }

        return query.Where(t => t.SourceId.HasValue && ids.Contains(t.SourceId.Value));
    }

    internal static IQueryable<ActivityLog> FromIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.FromId == id);
        }

        return query.Where(t => t.FromId.HasValue && ids.Contains(t.FromId.Value));
    }

    internal static IQueryable<ActivityLog> ToIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
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

        return query.Where(t => t.ToId.HasValue && ids.Contains(t.ToId.Value));
    }

    internal static IQueryable<ActivityLog> ViaIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ViaId == id);
        }

        return query.Where(t => t.ViaId.HasValue && ids.Contains(t.ViaId.Value));
    }

    internal static IQueryable<ActivityLog> RoleIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
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

        return query.Where(t => t.RoleId.HasValue && ids.Contains(t.RoleId.Value));
    }

    internal static IQueryable<ActivityLog> PackageIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.PackageId == id);
        }

        return query.Where(t => t.PackageId.HasValue && ids.Contains(t.PackageId.Value));
    }

    internal static IQueryable<ActivityLog> ResourceIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ResourceId == id);
        }

        return query.Where(t => t.ResourceId.HasValue && ids.Contains(t.ResourceId.Value));
    }

    internal static IQueryable<ActivityLog> InstanceIdContains(this IQueryable<ActivityLog> query, HashSet<string> values)
    {
        if (values is null || values.Count == 0)
        {
            return query;
        }

        if (values.Count == 1)
        {
            var value = values.First();
            return query.Where(t => t.InstanceId == value);
        }

        return query.Where(t => t.InstanceId != null && values.Contains(t.InstanceId));
    }

    internal static IQueryable<ActivityLog> OperationIdContains(this IQueryable<ActivityLog> query, HashSet<string> values)
    {
        if (values is null || values.Count == 0)
        {
            return query;
        }

        if (values.Count == 1)
        {
            var value = values.First();
            return query.Where(t => t.OperationId == value);
        }

        return query.Where(t => t.OperationId != null && values.Contains(t.OperationId));
    }

    internal static IQueryable<ActivityLog> ItemIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ItemId == id);
        }

        return query.Where(t => ids.Contains(t.ItemId));
    }

    internal static IQueryable<ActivityLog> ParentIdContains(this IQueryable<ActivityLog> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ParentId == id);
        }

        return query.Where(t => t.ParentId.HasValue && ids.Contains(t.ParentId.Value));
    }
}

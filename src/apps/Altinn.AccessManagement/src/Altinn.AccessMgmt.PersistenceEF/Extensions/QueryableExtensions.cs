using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<T> WhereMatchIfSet<T>(
    this IQueryable<T> query,
    HashSet<Guid>? ids,
    Expression<Func<T, Guid>> selector)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        return ids.Count == 1
            ? query.Where(x => EF.Property<Guid>(x, ((MemberExpression)selector.Body).Member.Name) == ids.First())
            : query.Where(x => ids.Contains(EF.Property<Guid>(x, ((MemberExpression)selector.Body).Member.Name)));
    }

    public static IQueryable<T> WhereMatchIfSet<T>(
    this IQueryable<T> query,
    HashSet<Guid>? ids,
    string columnName)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        return ids.Count == 1
            ? query.Where(e => EF.Property<Guid>(e, columnName) == ids.First())
            : query.Where(e => ids.Contains(EF.Property<Guid>(e, columnName)));
    }

    public static IQueryable<T> WhereMatchIfSetExpressionCall<T>(
    this IQueryable<T> query,
    HashSet<Guid>? ids,
    Expression<Func<T, Guid>> selector)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        var parameter = selector.Parameters[0];
        var invokedSelector = Expression.Invoke(selector, parameter);

        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        var body = Expression.Call(
            containsMethod,
            Expression.Constant(ids),
            invokedSelector
        );

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return query.Where(lambda);
    }
}

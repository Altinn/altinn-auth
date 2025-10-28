using System.Linq.Expressions;

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

    public static IQueryable<T> WhereEqualsIfSet<T>(
    this IQueryable<T> query,
    Guid? id,
    Expression<Func<T, Guid>> selector)
    {
        if (id is null)
        {
            return query;
        }

        var equalExpr = Expression.Equal(selector.Body, Expression.Constant(id.Value));
        var lambda = Expression.Lambda<Func<T, bool>>(equalExpr, selector.Parameters);
        return query.Where(lambda);
    }

    public static IQueryable<T> WhereInIfSet<T>(
        this IQueryable<T> query,
        IReadOnlyCollection<Guid>? ids,
        Expression<Func<T, Guid>> selector)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        var parameter = selector.Parameters[0];
        var body = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            new[] { typeof(Guid) },
            Expression.Constant(ids),
            selector.Body
        );

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// If ids are null or empty do nothing. 
    /// If ids contains single element use Equals. 
    /// If ids contains multiple use In.
    /// </summary>
    public static IQueryable<T> WhereMatchIfSet<T>(
     this IQueryable<T> query,
     IReadOnlyCollection<Guid>? ids,
     Expression<Func<T, Guid>> selector)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            return query.WhereEqualsIfSet(ids.First(), selector);
        }

        return query.WhereInIfSet(ids, selector);
    }
}

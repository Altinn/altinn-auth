#nullable enable

using System.Diagnostics;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.Models.Versioned;

/// <summary>
/// A condition for a <see cref="IVersionEquatable{T}"/>.
/// </summary>
/// <typeparam name="T">The version type.</typeparam>
public interface IVersionedEntityCondition<T>
    where T : notnull
{
    /// <summary>
    /// Validate all request conditions against an entity,
    /// short-circuiting if any of the conditions return
    /// <see cref="VersionedEntityConditionResult.Failed"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity</param>
    /// <returns>A <see cref="VersionedEntityConditionResult"/></returns>
    VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
        where TEntity : notnull, IVersionEquatable<T>;
}

/// <summary>
/// Extension methods for <see cref="IVersionedEntityCondition{T}"/>.
/// </summary>
public static class VersionedEntityConditionExtensions
{
    /// <summary>
    /// Create a new <see cref="IVersionedEntityCondition{T}"/> over the selected property.
    /// </summary>
    /// <typeparam name="TOuter">The outer type</typeparam>
    /// <typeparam name="TInner">The inner type</typeparam>
    /// <param name="self">The original entity condition</param>
    /// <param name="converter">The converter from <typeparamref name="TOuter"/> to <typeparamref name="TInner"/></param>
    /// <returns>A new <see cref="IVersionedEntityCondition{T}"/></returns>
    public static IVersionedEntityCondition<TInner> Select<TOuter, TInner>(this IVersionedEntityCondition<TOuter> self, Func<TOuter, TInner> converter)
        where TOuter : notnull
        where TInner : notnull
        => new MappedVersionEntityCondition<TInner, TOuter>(self, converter);

    /// <summary>
    /// Validate the condition against a null/non-existing entity.
    /// </summary>
    /// <typeparam name="T">The version tag type</typeparam>
    /// <param name="self">The entity condition</param>
    /// <returns>The validation result</returns>
    public static VersionedEntityConditionResult ValidateNullEntity<T>(this IVersionedEntityCondition<T> self)
        where T : notnull
    {
        Guard.IsNotNull(self);

        return self.Validate(default(NullEntity<T>));
    }

    /// <summary>
    /// Check if the condition allows creating a new entity.
    /// </summary>
    /// <typeparam name="T">The version tag type</typeparam>
    /// <param name="self">The entity condition</param>
    /// <returns><see langword="true"/> if the condition allows for creating a new entity, otherwise <see langword="false"/>.</returns>
    public static bool AllowsCreatingNewEntity<T>(this IVersionedEntityCondition<T> self)
        where T : notnull
    {
        Guard.IsNotNull(self);

        var result = self.ValidateNullEntity();
        Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified result is not expected for a allows-creating-new-entity check");

        return result == VersionedEntityConditionResult.Succeeded;
    }

    private readonly struct NullEntity<T>
        : IVersionEquatable<T>
        where T : notnull
    {
        public bool Exists => false;

        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => false;

        public bool VersionEquals(T other)
            => false;
    }

    private sealed class MappedVersionEntityCondition<TInner, TOuter>
        : IVersionedEntityCondition<TInner>
        where TInner : notnull
        where TOuter : notnull
    {
        private readonly IVersionedEntityCondition<TOuter> _inner;
        private readonly Func<TOuter, TInner> _converter;

        public MappedVersionEntityCondition(IVersionedEntityCondition<TOuter> inner, Func<TOuter, TInner> converte)
        {
            Guard.IsNotNull(inner);
            Guard.IsNotNull(converte);

            _inner = inner;
            _converter = converte;
        }

        public VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
            where TEntity : IVersionEquatable<TInner>
        {
            return _inner.Validate(new MappedEntity<TEntity>(entity, _converter));
        }

        private readonly struct MappedEntity<TEntity>(TEntity entity, Func<TOuter, TInner> converter)
            : IVersionEquatable<TOuter>
            where TEntity : IVersionEquatable<TInner>
        {
            public bool Exists => entity.Exists;

            public bool ModifiedSince(HttpDateTimeHeaderValue other)
            {
                return entity.ModifiedSince(other);
            }

            public bool VersionEquals(TOuter other)
            {
                return entity.VersionEquals(converter(other));
            }
        }
    }
}

#nullable enable

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Helper methods for <see cref="RequestConditionCollection{T}"/>.
/// </summary>
public static partial class RequestConditionCollection
{
    /// <inheritdoc cref="RequestConditionCollection{T}.Empty"/>
    /// <typeparam name="T">The etag type</typeparam>
    public static RequestConditionCollection<T> Empty<T>()
        where T : notnull
        => RequestConditionCollection<T>.Empty;

    /// <summary>
    /// Creates a new <see cref="RequestConditionCollection{T}"/> from a set of <see cref="RequestCondition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The etag type</typeparam>
    /// <param name="conditions">The conditions</param>
    /// <returns>The new <see cref="RequestConditionCollection{T}"/></returns>
    public static RequestConditionCollection<T> Create<T>(IEnumerable<IVersionedEntityCondition<T>> conditions)
        where T : notnull
        => RequestConditionCollection<T>.Create(conditions);

    /// <summary>
    /// Creates a new <see cref="RequestConditionCollection{T}"/> from a set of <see cref="RequestCondition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The etag type</typeparam>
    /// <param name="conditions">The conditions</param>
    /// <returns>The new <see cref="RequestConditionCollection{T}"/></returns>
    public static RequestConditionCollection<T> Create<T>(ImmutableArray<IVersionedEntityCondition<T>> conditions)
        where T : notnull
        => RequestConditionCollection<T>.Create(conditions);

    /// <summary>
    /// Returns the etag type for a <see cref="RequestConditionCollection{T}"/> type.
    /// </summary>
    /// <param name="requestConditionsType">The <see cref="RequestConditionCollection{T}"/> type.</param>
    /// <returns>
    /// The type argument of the <paramref name="requestConditionsType"/> parameter, 
    /// if the <paramref name="requestConditionsType"/> is a closed generic request 
    /// conditions type, otherwise <see langword="null"/>.
    /// </returns>
    public static Type? GetETagType(Type requestConditionsType)
    {
        Guard.IsNotNull(requestConditionsType);

        if (requestConditionsType.IsGenericType && !requestConditionsType.IsGenericTypeDefinition)
        {
            var genericType = requestConditionsType.GetGenericTypeDefinition();
            if (ReferenceEquals(genericType, typeof(RequestConditionCollection<>)) || ReferenceEquals(genericType, typeof(IVersionedEntityCondition<>)))
            {
                return requestConditionsType.GetGenericArguments()[0];
            }
        }

        return null;
    }
}

/// <summary>
/// A set of <see cref="RequestCondition{T}"/>.
/// </summary>
/// <typeparam name="T">The etag type</typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RequestConditionCollection<T>
    : IReadOnlyCollection<IVersionedEntityCondition<T>>
    , IVersionedEntityCondition<T>
    where T : notnull
{
    private readonly ImmutableArray<IVersionedEntityCondition<T>> _conditions;

    /// <summary>
    /// Gets an empty <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    public static RequestConditionCollection<T> Empty { get; } = new(ImmutableArray<IVersionedEntityCondition<T>>.Empty);

    /// <summary>
    /// Constructs a new instance of <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    /// <param name="conditions">The conditions</param>
    private RequestConditionCollection(ImmutableArray<IVersionedEntityCondition<T>> conditions)
    {
        if (conditions.IsDefault)
        {
            ThrowHelper.ThrowArgumentException(nameof(conditions), "Conditions must be set");
        }

        _conditions = conditions;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    /// <param name="conditions">The conditions</param>
    public static RequestConditionCollection<T> Create(ImmutableArray<IVersionedEntityCondition<T>> conditions)
    {
        if (conditions.IsDefaultOrEmpty)
        {
            return Empty;
        }

        return new(conditions);
    }

    /// <summary>
    /// Constructs a new instance of <see cref="RequestConditionCollection{T}"/>.
    /// </summary>
    /// <param name="conditions">The conditions</param>
    public static RequestConditionCollection<T> Create(IEnumerable<IVersionedEntityCondition<T>> conditions)
    {
        if (conditions is ImmutableArray<RequestCondition<T>> immutableArray)
        {
            return Create(immutableArray);
        }

        if (conditions is ICollection collection && collection.Count == 0)
        {
            return Empty;
        }

        return Create(conditions.ToImmutableArray());
    }

    /// <inheritdoc/>
    public int Count => _conditions.Length;

    /// <inheritdoc/>
    public VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
        where TEntity : IVersionEquatable<T>
    {
        var result = VersionedEntityConditionResult.Succeeded;
        foreach (var condition in _conditions)
        {
            result = VersionedEntityConditionResult.Max(result, condition.Validate(entity));
            if (result == VersionedEntityConditionResult.Failed)
            {
                break;
            }
        }

        return result;
    }

    /// <inheritdoc cref="ImmutableArray{T}.GetEnumerator()"/>
    public ImmutableArray<IVersionedEntityCondition<T>>.Enumerator GetEnumerator()
        => _conditions.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<IVersionedEntityCondition<T>> IEnumerable<IVersionedEntityCondition<T>>.GetEnumerator()
        => ((IEnumerable<IVersionedEntityCondition<T>>)_conditions).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_conditions).GetEnumerator();

    [ExcludeFromCodeCoverage]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    private string DebuggerDisplay
        => _conditions switch
        {
            [] => "No conditions",
            [RequestCondition<T> c] => c.DebuggerDisplay,
            _ => $"Count: {_conditions.Length}",
        };
}

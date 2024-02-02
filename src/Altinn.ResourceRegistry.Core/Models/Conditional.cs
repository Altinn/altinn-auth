#nullable enable

using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// A conditional entity value.
/// </summary>
public static class Conditional
{
    /// <summary>
    /// Creates a new <see cref="Conditional{T, TTag}"/> with a value.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="value">The value</param>
    /// <returns>A new <see cref="Conditional{T, TTag}"/></returns>
    public static EntityFound<T> Succeeded<T>(T value)
        => new(value);

    /// <summary>
    /// Creates a new <see cref="Conditional{T, TTag}"/> in the not found state.
    /// </summary>
    /// <returns>A <see cref="Conditional{T, TTag}"/></returns>
    public static EntityNotFound NotFound()
        => default;

    /// <summary>
    /// Gets a <see cref="Conditional{T, TTag}"/> in the failed state.
    /// </summary>
    /// <returns>A <see cref="Conditional{T, TTag}"/></returns>
    public static EntityConditionFailed ConditionFailed()
        => default;

    /// <summary>
    /// Gets a <see cref="Conditional{T, TTag}"/> in the unmodified state.
    /// </summary>
    /// <typeparam name="TTag">The version tag type.</typeparam>
    /// <returns>A <see cref="Conditional{T, TTag}"/></returns>
    public static EntityUnmodified<TTag> Unmodified<TTag>(TTag tag, DateTimeOffset modifiedAt)
        where TTag : notnull
        => new(tag, modifiedAt);

    /// <summary>
    /// Returns the etag type for a <see cref="Conditional{T, TTag}"/> type.
    /// </summary>
    /// <param name="opaqueType">The <see cref="Conditional{T, TTag}"/> type.</param>
    /// <returns>
    /// The type argument of the <paramref name="opaqueType"/> parameter, 
    /// if the <paramref name="opaqueType"/> is a closed generic request 
    /// opaque type, otherwise <see langword="null"/>.
    /// </returns>
    public static (Type ValueType, Type TagType)? GetUnderlyingTypes(Type opaqueType)
    {
        Guard.IsNotNull(opaqueType);

        if (opaqueType.IsGenericType && !opaqueType.IsGenericTypeDefinition)
        {
            var genericType = opaqueType.GetGenericTypeDefinition();
            if (ReferenceEquals(genericType, typeof(Conditional<,>)))
            {
                var args = opaqueType.GetGenericArguments();
                return (args[0], args[1]);
            }
        }

        return null;
    }

    /// <summary>
    /// Entity found sentinel. Implicitly converts to any <see cref="Conditional{T, TTag}"/> type.
    /// </summary>
    /// <remarks>
    /// This type exists so that it's possible to call <see cref="Succeeded{T}(T)"/> without having to specify the <c>TTag</c> type argument,
    /// and allow the compiler to infer the remaining type argument.
    /// </remarks>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="value">The entity.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct EntityFound<T>(T value)
    {
        /// <summary>
        /// Gets the entity.
        /// </summary>
        public T Value { get; } = value;
    }

    /// <summary>
    /// Entity not found sentinel. Implicitly converts to any <see cref="Conditional{T, TTag}"/> type.
    /// </summary>
    /// <remarks>
    /// This type exists so that it's possible to call <see cref="NotFound()"/> without having to specify a type argument,
    /// and allow the compiler to infer the type arguments.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct EntityNotFound
    {   
    }

    /// <summary>
    /// Entity condition failed sentinel. Implicitly converts to any <see cref="Conditional{T, TTag}"/> type.
    /// </summary>
    /// <remarks>
    /// This type exists so that it's possible to call <see cref="ConditionFailed()"/> without having to specify a type argument,
    /// and allow the compiler to infer the type arguments.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct EntityConditionFailed
    {
    }

    /// <summary>
    /// Entity unmodified sentinel. Implicitly converts to any <see cref="Conditional{T, TTag}"/> type.
    /// </summary>
    /// <remarks>
    /// This type exists so that it's possible to call <see cref="Unmodified{TTag}(TTag, DateTimeOffset)"/> without having to specify the <c>TEntity</c> type argument,
    /// and allow the compiler to infer the remaining type argument.
    /// </remarks>
    /// <typeparam name="TTag">The tag type.</typeparam>
    /// <param name="tag">The tag.</param>
    /// <param name="modifiedAt">When this entity was last modified.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct EntityUnmodified<TTag>(TTag tag, DateTimeOffset modifiedAt)
        where TTag : notnull
    {
        /// <summary>
        /// Gets the tag.
        /// </summary>
        public TTag Tag { get; } = tag;

        /// <summary>
        /// Gets when this entity was last modified.
        /// </summary>
        public DateTimeOffset ModifiedAt { get; } = modifiedAt;
    }

    /// <summary>
    /// Converts a <see cref="Conditional{T, TTag}"/> from a <typeparamref name="TFrom"/> to a <typeparamref name="TTo"/> and a <typeparamref name="TTagFrom"/> to a <typeparamref name="TTagTo"/>.
    /// </summary>
    /// <typeparam name="TFrom">The value type to convert from.</typeparam>
    /// <typeparam name="TTo">The value type to convert to.</typeparam>
    /// <typeparam name="TTagFrom">The tag type to convert from.</typeparam>
    /// <typeparam name="TTagTo">The tag type to convert to.</typeparam>
    /// <param name="conditional">The conditional to convert.</param>
    /// <param name="valueConverter">The value converter.</param>
    /// <param name="tagConverter">The tag converter.</param>
    /// <returns>A new <see cref="Conditional{T, TTag}"/> with the converted data.</returns>
    public static Conditional<TTo, TTagTo> Select<TFrom, TTo, TTagFrom, TTagTo>(
        this Conditional<TFrom, TTagFrom> conditional,
        Func<TFrom, TTo> valueConverter,
        Func<TTagFrom, TTagTo> tagConverter)
        where TTagFrom : notnull
        where TTagTo : notnull
    {
        Guard.IsNotNull(conditional);
        Guard.IsNotNull(valueConverter);
        Guard.IsNotNull(tagConverter);

        return conditional switch
        {
            { IsSucceeded: true } => Conditional<TTo, TTagTo>.CreateSucceeded(valueConverter(conditional.Value)),
            { IsUnmodified: true } => Conditional<TTo, TTagTo>.CreateUnmodified(tagConverter(conditional.VersionTag), conditional.VersionModifiedAt),
            { IsNotFound: true } => Conditional<TTo, TTagTo>.NotFoundInstance,
            { IsConditionFailed: true } => Conditional<TTo, TTagTo>.FailedInstance,
            _ => throw new UnreachableException("Unhandled conditional case"),
        };
    }
}

/// <summary>
/// A conditional entity value.
/// </summary>
/// <seealso cref="IVersionedEntityCondition{T}"/>
/// <typeparam name="T">The result type.</typeparam>
/// <typeparam name="TTag">The version tag type.</typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class Conditional<T, TTag>
    where TTag : notnull
{
    private readonly T? _value;
    private readonly VersionInfo _version;
    private readonly Result _result;

    /// <summary>
    /// Creates a new <see cref="Conditional{T, TTag}"/> with a value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>A new <see cref="Conditional{T, TTag}"/></returns>
    internal static Conditional<T, TTag> CreateSucceeded(T value) 
        => new(value, default, Result.Succeeded);

    /// <summary>
    /// Gets a <see cref="Conditional{T, TTag}"/> representing an unmodified entity.
    /// </summary>
    internal static Conditional<T, TTag> CreateUnmodified(TTag tag, DateTimeOffset modifiedAt) 
        => new(default, new(tag, modifiedAt), Result.Unmodified);

    /// <summary>
    /// Gets a <see cref="Conditional{T, TTag}"/> representing a failed condition.
    /// </summary>
    internal static Conditional<T, TTag> FailedInstance { get; } = new(default, default, Result.ConditionFailed);

    /// <summary>
    /// Gets a <see cref="Conditional{T, TTag}"/> representing a not found entity.
    /// </summary>
    internal static Conditional<T, TTag> NotFoundInstance { get; } = new(default, default, Result.NotFound);

    private Conditional(T? value, VersionInfo version, Result result)
    {
        _value = value;
        _version = version;
        _result = result;
    }

    /// <summary>
    /// Gets a value indicating whether the condition was successful.
    /// </summary>
    public bool IsSucceeded => _result == Result.Succeeded;

    /// <summary>
    /// Gets a value indicating whether the entity was unmodified.
    /// </summary>
    public bool IsUnmodified => _result == Result.Unmodified;

    /// <summary>
    /// Gets a value indicating whether the condition failed.
    /// </summary>
    public bool IsConditionFailed => _result == Result.ConditionFailed;

    /// <summary>
    /// Gets a value indicating whether the entity was not found.
    /// </summary>
    public bool IsNotFound => _result == Result.NotFound;

    private VersionInfo Version
    {
        get
        {
            if (_result != Result.Unmodified)
            {
                ThrowHelper.ThrowInvalidOperationException("Conditional version is not available");
            }

            return _version;
        }
    }

    /// <summary>
    /// Gets the version tag.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsUnmodified"/> is not true.</exception>
    public TTag VersionTag => Version.Tag;

    /// <summary>
    /// Gets when this version was last modified.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsUnmodified"/> is not true.</exception>
    public DateTimeOffset VersionModifiedAt => Version.ModifiedAt;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IsSucceeded"/> is not true.</exception>
    public T Value
    {
        get
        {
            if (_result != Result.Succeeded)
            {
                ThrowHelper.ThrowInvalidOperationException("Conditional value is not available");
            }

            return _value!;
        }
    }

    public static implicit operator Conditional<T, TTag>(T value) 
        => Conditional<T, TTag>.CreateSucceeded(value);

    public static implicit operator Conditional<T, TTag>(Conditional.EntityFound<T> entity)
        => Conditional<T, TTag>.CreateSucceeded(entity.Value);

    public static implicit operator Conditional<T, TTag>(Conditional.EntityNotFound _)
        => Conditional<T, TTag>.NotFoundInstance;

    public static implicit operator Conditional<T, TTag>(Conditional.EntityConditionFailed _)
        => Conditional<T, TTag>.FailedInstance;

    public static implicit operator Conditional<T, TTag>(Conditional.EntityUnmodified<TTag> unmodified)
        => Conditional<T, TTag>.CreateUnmodified(unmodified.Tag, unmodified.ModifiedAt);

    /// <inheritdoc/>
    public override string ToString()
        => _result switch
        {
            Result.Succeeded => $"{nameof(Result.Succeeded)}({_value})",
            Result.NotFound => nameof(Result.NotFound),
            Result.Unmodified => $"{nameof(Result.Unmodified)}({_version.Tag}, {_version.ModifiedAt:s})",
            Result.ConditionFailed => nameof(Result.ConditionFailed),
            _ => throw new UnreachableException("Unhandled conditional case"),
        };

    private string DebuggerDisplay
        => _result.ToString();

    private readonly struct VersionInfo(TTag tag, DateTimeOffset modifiedAt)
    {
        public TTag Tag { get; } = tag;

        public DateTimeOffset ModifiedAt { get; } = modifiedAt;
    }

    private enum Result
    {
        Succeeded,
        NotFound,
        Unmodified,
        ConditionFailed,
    }
}

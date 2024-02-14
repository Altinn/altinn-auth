#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Models;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Altinn.ResourceRegistry.Results;

/// <summary>
/// Helpers for <see cref="Conditional{T, TTag}"/>.
/// </summary>
public static class ConditionalResult
{
    /// <summary>
    /// Gets the underlying types of a <see cref="ConditionalResult{T, TTag}"/> type.
    /// </summary>
    /// <param name="type">The conditional type</param>
    /// <returns>A tuple with the value type and tag type, or <see langword="null"/> if type is not a <see cref="ConditionalResult{TValue, TTag}"/> type.</returns>
    public static (Type ValueType, Type TagType)? GetUnderlyingTypes(Type type)
    {
        Debug.Assert(type is not null);

        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(ConditionalResult<,>))
            {
                var genericArguments = type.GetGenericArguments();
                var valueType = genericArguments[0];
                var tagType = genericArguments[1];
                return (valueType, tagType);
            }
        }

        return null;
    }
}

/// <summary>
/// A type that wraps either an <typeparamref name="TValue"/>, an unmodified entity version, a precondition failed, or an instance or an <see cref="ActionResult"/>.
/// </summary>
/// <typeparam name="TValue">The type of the result.</typeparam>
/// <typeparam name="TTag">The type of the value tag.</typeparam>
public class ConditionalResult<TValue, TTag> 
    : IConvertToActionResult
    where TValue : ITaggedEntity<TTag>
    where TTag : notnull
{
    /// <summary>
    /// Gets the <see cref="ActionResult"/>.
    /// </summary>
    public ActionResult? Result { get; }

    /// <summary>
    /// Gets the result type.
    /// </summary>
    public VersionedEntityConditionResult? ResultType { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <remarks>
    /// Only set if <see cref="ResultType"/> is <see cref="VersionedEntityConditionResult.Succeeded"/>.
    /// </remarks>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the version tag.
    /// </summary>
    /// <remarks>
    /// Only set if <see cref="ResultType"/> is <see cref="VersionedEntityConditionResult.Succeeded"/> or 
    /// <see cref="VersionedEntityConditionResult.Unmodified"/>.
    /// </remarks>
    public TTag? VersionTag { get; }

    /// <summary>
    /// Gets when this version was last modified.
    /// </summary>
    /// <remarks>
    /// Only set if <see cref="ResultType"/> is <see cref="VersionedEntityConditionResult.Succeeded"/> or 
    /// <see cref="VersionedEntityConditionResult.Unmodified"/>.
    /// </remarks>
    public HttpDateTimeHeaderValue? VersionModifiedAt { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalResult{TValue, TTag}"/> with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="versionTag">The version tag.</param>
    /// <param name="lastModified">When this version was last modified.</param>
    public ConditionalResult(TValue value, [DisallowNull] TTag versionTag, HttpDateTimeHeaderValue lastModified)
    {
        if (typeof(IActionResult).IsAssignableFrom(typeof(TValue)) ||
           typeof(IResult).IsAssignableFrom(typeof(TValue)))
        {
            ThrowHelper.ThrowArgumentException($"Invalid type parameter '{typeof(TValue)}' specified for 'ConditionalResult<>'.");
        }

        Guard.IsNotNull(versionTag);
        Guard.IsNotDefault(lastModified);

        ResultType = VersionedEntityConditionResult.Succeeded;
        Value = value;
        VersionTag = versionTag;
        VersionModifiedAt = lastModified;
    }

    private ConditionalResult(VersionedEntityConditionResult resultType, TTag? versionTag, HttpDateTimeHeaderValue? lastModified)
    {
        if (typeof(IActionResult).IsAssignableFrom(typeof(TValue)) ||
           typeof(IResult).IsAssignableFrom(typeof(TValue)))
        {
            ThrowHelper.ThrowArgumentException($"Invalid type parameter '{typeof(TValue)}' specified for 'ConditionalResult<>'.");
        }

        Debug.Assert(resultType != VersionedEntityConditionResult.Succeeded);
        if (resultType == VersionedEntityConditionResult.Unmodified)
        {
            Debug.Assert(versionTag != null);
            Debug.Assert(lastModified != null);
        }

        ResultType = resultType;
        VersionTag = versionTag;
        VersionModifiedAt = lastModified;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConditionalResult{TValue, TTag}"/> using the specified <see cref="ActionResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="ActionResult"/>.</param>
    public ConditionalResult(ActionResult result)
    {
        if (typeof(IActionResult).IsAssignableFrom(typeof(TValue)) ||
           typeof(IResult).IsAssignableFrom(typeof(TValue)))
        {
            ThrowHelper.ThrowArgumentException($"Invalid type parameter '{typeof(TValue)}' specified for 'ConditionalResult<>'.");
        }

        Result = result;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalResult{TValue, TTag}"/> with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A new <see cref="ConditionalResult{TValue, TTag}"/></returns>
    public static ConditionalResult<TValue, TTag> Succeeded([DisallowNull] TValue value)
    {
        Guard.IsNotNull(value);

        value.GetHeaderValues(out var versionTag, out var modifiedAt);
        return new(value, versionTag, modifiedAt);
    }

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalResult{TValue, TTag}"/> that returns a "304 Not Modified" result to the client.
    /// </summary>
    /// <param name="versionTag">The version tag.</param>
    /// <param name="lastModified">When this version was last modified.</param>
    /// <returns>A new <see cref="ConditionalResult{TValue, TTag}"/></returns>
    public static ConditionalResult<TValue, TTag> Unmodified(TTag versionTag, HttpDateTimeHeaderValue lastModified)
    {
        Guard.IsNotNull(versionTag);
        Guard.IsNotDefault(lastModified);

        return new(VersionedEntityConditionResult.Unmodified, versionTag, lastModified);
    }

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalResult{TValue, TTag}"/> that returns a "412 Precondition Failed" result to the client.
    /// </summary>
    /// <returns>A new <see cref="ConditionalResult{TValue, TTag}"/></returns>
    public static ConditionalResult<TValue, TTag> Failed() => new(VersionedEntityConditionResult.Failed, default(TTag?), default(HttpDateTimeHeaderValue?));

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalResult{TValue, TTag}"/> that returns a "404 Not Found" result to the client.
    /// </summary>
    /// <returns>A new <see cref="ConditionalResult{TValue, TTag}"/></returns>
    public static ConditionalResult<TValue, TTag> NotFound() => new(new NotFoundResult());

    /// <summary>
    /// Creates a new instance of <see cref="ConditionalResult{TValue, TTag}"/> from a <see cref="Conditional{TValue, TTag}"/>.
    /// </summary>
    /// <param name="conditional">The conditional.</param>
    /// <returns>A new <see cref="ConditionalResult{TValue, TTag}"/></returns>
    public static ConditionalResult<TValue, TTag> FromConditional(Conditional<TValue, TTag> conditional)
    {
        return conditional switch
        {
            { IsSucceeded: true } => Succeeded(conditional.Value),
            { IsUnmodified: true } => Unmodified(conditional.VersionTag, new(conditional.VersionModifiedAt)),
            { IsNotFound: true } => NotFound(),
            { IsConditionFailed: true } => Failed(),
            _ => throw new UnreachableException("Unhandled conditional case"),
        };
    }

    public static implicit operator ConditionalResult<TValue, TTag>(TValue value)
        => Succeeded(value);

    public static implicit operator ConditionalResult<TValue, TTag>(Conditional<TValue, TTag> conditional)
        => FromConditional(conditional);

    public static implicit operator ConditionalResult<TValue, TTag>(ActionResult result)
        => new(result);

    public static implicit operator ConditionalResult<TValue, TTag>(Conditional.EntityConditionFailed _)
        => Failed();

    public static implicit operator ConditionalResult<TValue, TTag>(Conditional.EntityNotFound _)
        => NotFound();

    public static implicit operator ConditionalResult<TValue, TTag>(Conditional.EntityUnmodified<TTag> unmodified)
        => Unmodified(unmodified.Tag, new(unmodified.ModifiedAt));

    public static implicit operator ConditionalResult<TValue, TTag>(Conditional.EntityFound<TValue> found)
        => Succeeded(found.Value);

    /// <inheritdoc/>
    IActionResult IConvertToActionResult.Convert()
    {
        if (Result is { } result)
        {
            return result;
        }

        // if we don't have a result, we should always have a result type
        var resultType = ResultType!.Value;

        if (resultType == VersionedEntityConditionResult.Failed)
        {
            return new StatusCodeResult(StatusCodes.Status412PreconditionFailed);
        }

        var etag = RequestCondition.SerializeETag(VersionTag!);
        var modifiedAt = VersionModifiedAt!.Value;
        if (resultType == VersionedEntityConditionResult.Unmodified)
        {
            return new NotModifiedResult(etag, modifiedAt);
        }

        Debug.Assert(resultType == VersionedEntityConditionResult.Succeeded);
        return new VersionedTaggedObjectResult(Value, etag, modifiedAt)
        {
            DeclaredType = typeof(TValue),
        };
    }
}

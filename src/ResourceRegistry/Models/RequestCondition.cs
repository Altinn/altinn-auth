#nullable enable

using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using CommunityToolkit.Diagnostics;
using Microsoft.Net.Http.Headers;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Helpers for <see cref="RequestCondition{T}"/>.
/// </summary>
public static class RequestCondition
{
    /// <inheritdoc cref="RequestCondition{T}.Exists"/>
    public static RequestCondition<T> Exists<T>()
        where T : notnull
        => RequestCondition<T>.Exists;

    /// <inheritdoc cref="RequestCondition{T}.NotExists(bool)"/>
    public static RequestCondition<T> NotExists<T>(bool isRead)
        where T : notnull
        => RequestCondition<T>.NotExists(isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsMatch(T)"/>
    public static RequestCondition<T> IsMatch<T>(T value)
        where T : notnull
        => RequestCondition<T>.IsMatch(value);

    /// <inheritdoc cref="RequestCondition{T}.IsMatch(ImmutableArray{T})"/>
    public static RequestCondition<T> IsMatch<T>(ImmutableArray<T> values)
        where T : notnull
        => RequestCondition<T>.IsMatch(values);

    /// <inheritdoc cref="RequestCondition{T}.IsMatch(IEnumerable{T})"/>
    public static RequestCondition<T> IsMatch<T>(IEnumerable<T> values)
        where T : notnull
        => RequestCondition<T>.IsMatch(values);

    /// <inheritdoc cref="RequestCondition{T}.IsDifferent(T, bool)"/>
    public static RequestCondition<T> IsDifferent<T>(T value, bool isRead)
        where T : notnull
        => RequestCondition<T>.IsDifferent(value, isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsDifferent(ImmutableArray{T}, bool)"/>
    public static RequestCondition<T> IsDifferent<T>(ImmutableArray<T> values, bool isRead)
        where T : notnull
        => RequestCondition<T>.IsDifferent(values, isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsDifferent(IEnumerable{T}, bool)"/>
    public static RequestCondition<T> IsDifferent<T>(IEnumerable<T> values, bool isRead)
        where T : notnull
        => RequestCondition<T>.IsDifferent(values, isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsModifiedSince(HttpDateTimeHeaderValue, bool)"/>
    public static RequestCondition<T> IsModifiedSince<T>(HttpDateTimeHeaderValue date, bool isRead)
        where T : notnull
        => RequestCondition<T>.IsModifiedSince(date, isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsUnmodifiedSince(HttpDateTimeHeaderValue)"/>
    public static RequestCondition<T> IsUnmodifiedSince<T>(HttpDateTimeHeaderValue date)
        where T : notnull
        => RequestCondition<T>.IsUnmodifiedSince(date);

    /// <summary>
    /// Serialize an entity tag to an <c>ETag</c> header value.
    /// </summary>
    /// <typeparam name="T">The tag type</typeparam>
    /// <param name="etag">The tag value</param>
    /// <returns>The entity tag header value</returns>
    public static string SerializeETag<T>(T etag)
        where T : notnull
    {
        return FormatETagValue(Opaque.Create(etag).ToString());

        static string FormatETagValue(string value)
        {
            var result = string.Create(value.Length + 4, value, static (buffer, state) =>
            {
                buffer[0] = 'W';
                buffer[1] = '/';
                buffer[2] = '"';
                buffer[^1] = '"';

                state.AsSpan().CopyTo(buffer[3..^1]);
            });

            Debug.Assert(EntityTagHeaderValue.TryParse(result, out _));
            return result;
        }
    }
}

/// <summary>
/// A condition for a request.
/// </summary>
/// <typeparam name="T">The etag type</typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RequestCondition<T>
    : IVersionedEntityCondition<T>
    where T : notnull
{
    private readonly HttpDateTimeHeaderValue? _date;
    private readonly ImmutableArray<T> _etags;
    private readonly Mode _mode;

    /// <summary>
    /// Gets a <see cref="RequestCondition{T}"/> for an <c>If-Match</c> header with a single value of <c>*</c>.
    /// </summary>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> Exists { get; }
        = new(default, default, Mode.Exists);

    private static readonly RequestCondition<T> _notExists = new(default, default, Mode.NotExists);
    private static readonly RequestCondition<T> _notExistsRead = new(default, default, Mode.NotExistsRead);

    /// <summary>
    /// Gets a <see cref="RequestCondition{T}"/> for an <c>If-None-Match</c> header with a single value of <c>*</c>.
    /// </summary>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> NotExists(bool isRead)
        => isRead ? _notExistsRead : _notExists;

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Match</c> header with a single value.
    /// </summary>
    /// <param name="value">The etag to match against.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsMatch(T value)
        => new([value], null, Mode.IsMatch);

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Match</c> header with multiple values.
    /// </summary>
    /// <param name="values">The etag values.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsMatch(ImmutableArray<T> values)
    {
        Guard.IsFalse(values.IsDefaultOrEmpty);

        return new(values, null, Mode.IsMatch);
    }

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Match</c> header with multiple values.
    /// </summary>
    /// <param name="values">The etag values.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsMatch(IEnumerable<T> values)
    {
        Guard.IsNotNull(values);

        return IsMatch(values.ToImmutableArray());
    }

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-None-Match</c> header with a single value.
    /// </summary>
    /// <param name="value">The etag to match against.</param>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsDifferent(T value, bool isRead)
        => new([value], null, isRead ? Mode.IsDifferentRead : Mode.IsDifferent);

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-None-Match</c> header with multiple values.
    /// </summary>
    /// <param name="values">The etag values.</param>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsDifferent(ImmutableArray<T> values, bool isRead)
    {
        Guard.IsFalse(values.IsDefaultOrEmpty);

        return new(values, null, isRead ? Mode.IsDifferentRead : Mode.IsDifferent);
    }

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-None-Match</c> header with multiple values.
    /// </summary>
    /// <param name="values">The etag values.</param>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsDifferent(IEnumerable<T> values, bool isRead)
    {
        Guard.IsNotNull(values);

        return IsDifferent(values.ToImmutableArray(), isRead);
    }

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Modified-Since</c> header.
    /// </summary>
    /// <param name="date">The date to match against</param>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsModifiedSince(HttpDateTimeHeaderValue date, bool isRead)
        => new(default, date, isRead ? Mode.IsModifiedSinceRead : Mode.IsModifiedSince);

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Unmodified-Since</c> header.
    /// </summary>
    /// <param name="date">The date to match against</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsUnmodifiedSince(HttpDateTimeHeaderValue date)
        => new(default, date, Mode.IsUnmodifiedSince);

    private RequestCondition(ImmutableArray<T> etags, HttpDateTimeHeaderValue? date, Mode mode)
    {
        _etags = etags;
        _date = date;
        _mode = mode;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The rules for what result to return can be found on
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/304"/> and
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/412"/>.
    /// </remarks>
    public VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
        where TEntity : notnull, IVersionEquatable<T>
    {
        return _mode switch
        {
            Mode.Exists => Check(entity.Exists, VersionedEntityConditionResult.Failed),
            Mode.NotExists => Check(!entity.Exists, VersionedEntityConditionResult.Failed),
            Mode.NotExistsRead => Check(!entity.Exists, VersionedEntityConditionResult.Unmodified),
            Mode.IsMatch => Check(VersionEquals(entity, _etags), VersionedEntityConditionResult.Failed),
            Mode.IsDifferentRead => Check(!VersionEquals(entity, _etags), VersionedEntityConditionResult.Unmodified),
            Mode.IsDifferent => Check(!VersionEquals(entity, _etags), VersionedEntityConditionResult.Failed),
            Mode.IsModifiedSinceRead => Check(entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Unmodified),
            Mode.IsModifiedSince => Check(entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Failed),
            Mode.IsUnmodifiedSince => Check(!entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Failed),
            _ => ThrowHelper.ThrowInvalidOperationException<VersionedEntityConditionResult>("Invalid mode"),
        };

        static VersionedEntityConditionResult Check(bool check, VersionedEntityConditionResult ifError)
            => check ? VersionedEntityConditionResult.Succeeded : ifError;

        static bool VersionEquals(TEntity entity, ImmutableArray<T> etags)
        {
            foreach (var etag in etags)
            {
                if (entity.VersionEquals(etag))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Debugger display string.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal string DebuggerDisplay
        => _mode switch
        {
            Mode.Exists => "If-Match: *",
            Mode.NotExists or Mode.NotExistsRead => "If-None-Match: *",
            Mode.IsMatch => $"If-Match: {ETagDebuggerDisplay}",
            Mode.IsDifferent or Mode.IsDifferentRead => $"If-None-Match: {ETagDebuggerDisplay}",
            Mode.IsModifiedSince or Mode.IsModifiedSinceRead => $"If-Modified-Since: {_date}",
            Mode.IsUnmodifiedSince => $"If-Unmodified-Since: {_date}",
            _ => "Invalid",
        };

    private string? ETagDebuggerDisplay
    {
        get
        {
            if (_etags.IsDefaultOrEmpty)
            {
                return "Invalid";
            }

            if (_etags.Length == 1)
            {
                var etag = _etags[0];
                return $"\"{etag}\"";
            }

            return string.Join(", ", _etags.Select(etag => $"\"{etag}\""));
        }
    }

    private enum Mode
    {
        Exists,
        NotExists,
        NotExistsRead,
        IsMatch,
        IsDifferentRead,
        IsDifferent,
        IsModifiedSinceRead,
        IsModifiedSince,
        IsUnmodifiedSince,
    }
}

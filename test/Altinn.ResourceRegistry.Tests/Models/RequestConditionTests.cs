#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Models;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using System;
using System.Collections.Immutable;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Models;

public class RequestConditionTests
{
    public static TheoryData<string, string, VersionedEntityConditionResult> IsMatchSingleData => new()
    {
        { "foo", "foo", VersionedEntityConditionResult.Succeeded },
        { "foo", "bar", VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IsMatchSingleData))]
    public void IsMatchSingle(string ifMatches, string etag, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsMatch(ifMatches);
        condition.Validate(Entity.Matchable(etag)).Should().Be(result);
    }

    public static TheoryData<ImmutableArray<string>, string, VersionedEntityConditionResult> IsMatchMultipleData => new()
    {
        { ["foo", "foo"], "foo", VersionedEntityConditionResult.Succeeded },
        { ["foo", "bar"], "foo", VersionedEntityConditionResult.Succeeded },
        { ["bar", "baz"], "foo", VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IsMatchMultipleData))]
    public void IsMatchMultiple(ImmutableArray<string> ifMatches, string etag, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsMatch(ifMatches);
        condition.Validate(Entity.Matchable(etag)).Should().Be(result);
    }

    public static TheoryData<string, string, bool, VersionedEntityConditionResult> IsDifferentSingleData => new()
    {
        { "foo", "foo", false, VersionedEntityConditionResult.Failed },
        { "foo", "bar", false, VersionedEntityConditionResult.Succeeded },
        { "foo", "foo", true, VersionedEntityConditionResult.Unmodified },
        { "foo", "bar", true, VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IsDifferentSingleData))]
    public void IsDifferentSingle(string ifNoneMatches, string etag, bool isRead, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsDifferent(ifNoneMatches, isRead);
        condition.Validate(Entity.Matchable(etag)).Should().Be(result);
    }

    public static TheoryData<ImmutableArray<string>, string, bool, VersionedEntityConditionResult> IsDifferentMultipleData => new()
    {
        { ["foo", "foo"], "foo", false, VersionedEntityConditionResult.Failed },
        { ["foo", "bar"], "foo", false, VersionedEntityConditionResult.Failed },
        { ["bar", "baz"], "foo", false, VersionedEntityConditionResult.Succeeded },
        { ["foo", "foo"], "foo", true, VersionedEntityConditionResult.Unmodified },
        { ["foo", "bar"], "foo", true, VersionedEntityConditionResult.Unmodified },
        { ["bar", "baz"], "foo", true, VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IsDifferentMultipleData))]
    public void IsDifferentMultiple(ImmutableArray<string> ifNoneMatches, string etag, bool isRead, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsDifferent(ifNoneMatches, isRead);
        condition.Validate(Entity.Matchable(etag)).Should().Be(result);
    }

    [Fact]
    public void Exists()
    {
        var condition = RequestCondition.Exists<string>();
        condition.Validate(Entity.Matchable("foo")).Should().Be(VersionedEntityConditionResult.Succeeded);
        condition.Validate(Entity.Matchable("bar")).Should().Be(VersionedEntityConditionResult.Succeeded);
        condition.Validate(Entity.NonExisting<string>()).Should().Be(VersionedEntityConditionResult.Failed);
    }

    [Fact]
    public void NotExists()
    {
        var condition = RequestCondition.NotExists<string>(isRead: true);
        condition.Validate(Entity.Matchable("foo")).Should().Be(VersionedEntityConditionResult.Unmodified);
        condition.Validate(Entity.Matchable("bar")).Should().Be(VersionedEntityConditionResult.Unmodified);
        condition.Validate(Entity.NonExisting<string>()).Should().Be(VersionedEntityConditionResult.Succeeded);

        condition = RequestCondition.NotExists<string>(isRead: false);
        condition.Validate(Entity.Matchable("foo")).Should().Be(VersionedEntityConditionResult.Failed);
        condition.Validate(Entity.Matchable("bar")).Should().Be(VersionedEntityConditionResult.Failed);
        condition.Validate(Entity.NonExisting<string>()).Should().Be(VersionedEntityConditionResult.Succeeded);
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, VersionedEntityConditionResult> IsUnmodifiedSinceData => new()
    {
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IsUnmodifiedSinceData))]
    public void IsUnmodifiedSince(DateTimeOffset ifUnmodifiedSince, DateTimeOffset lastModifiedAt, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsUnmodifiedSince<string>(new(ifUnmodifiedSince));
        condition.Validate(Entity.LastModified<string>(lastModifiedAt)).Should().Be(result);
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, bool, VersionedEntityConditionResult> IsModifiedSinceData => new()
    {
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), false, VersionedEntityConditionResult.Failed },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), false, VersionedEntityConditionResult.Failed },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), false, VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), true, VersionedEntityConditionResult.Unmodified },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), true, VersionedEntityConditionResult.Unmodified },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), true, VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IsModifiedSinceData))]
    public void IsModifiedSince(DateTimeOffset ifModifiedSince, DateTimeOffset lastModifiedAt, bool isRead, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsModifiedSince<string>(new(ifModifiedSince), isRead);
        condition.Validate(Entity.LastModified<string>(lastModifiedAt)).Should().Be(result);
    }

    [Fact]
    public void ConvertedCondition()
    {
        var condition = RequestCondition.Exists<byte>();
        var converted = condition.Select(v => (uint)v);
        converted.Validate(Entity.Matchable(1U)).Should().Be(VersionedEntityConditionResult.Succeeded);

        condition = RequestCondition.IsMatch((byte)2);
        converted = condition.Select(v => (uint)v);
        converted.Validate(Entity.Matchable(1U)).Should().Be(VersionedEntityConditionResult.Failed);

        condition = RequestCondition.IsUnmodifiedSince<byte>(new(new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero)));
        converted = condition.Select(v => (uint)v);
        converted.Validate(Entity.LastModified<uint>(new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero))).Should().Be(VersionedEntityConditionResult.Failed);
    }

    private static class Entity
    {
        public static MatchableEntity<T> Matchable<T>(T value)
            where T : notnull, IEquatable<T>
            => new(value);

        public static LastModifiedEntity<T> LastModified<T>(DateTimeOffset lastModified)
            where T : notnull, IEquatable<T>
            => new(lastModified);

        public static NonEntity<T> NonExisting<T>()
            where T : notnull, IEquatable<T>
            => new();
    }

    private readonly record struct MatchableEntity<T>(T Value) : IVersionEquatable<T>
        where T : notnull, IEquatable<T>
    {
        public bool Exists => true;

        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => ThrowHelper.ThrowNotSupportedException<bool>();

        public bool VersionEquals(T other)
            => Value.Equals(other);
    }

    private readonly record struct LastModifiedEntity<T>(DateTimeOffset LastModified) : IVersionEquatable<T>
        where T : notnull, IEquatable<T>
    {
        public bool Exists => true;

        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => LastModified > other;

        public bool VersionEquals(T other)
            => ThrowHelper.ThrowNotSupportedException<bool>();
    }

    private readonly record struct NonEntity<T>() : IVersionEquatable<T>
        where T : notnull, IEquatable<T>
    {
        public bool Exists => false;

        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => false;

        public bool VersionEquals(T other)
            => false;
    }
}

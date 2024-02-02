#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using FluentAssertions;
using System;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Models;

public class VersionedEntityTests
{
    [Fact]
    public void VersionedEntityConditionResult_Compares_Correctly()
    {
        VersionedEntityConditionResult.Succeeded.Should().BeLessThan(VersionedEntityConditionResult.Unmodified);
        VersionedEntityConditionResult.Unmodified.Should().BeLessThan(VersionedEntityConditionResult.Failed);
        VersionedEntityConditionResult.Succeeded.Should().BeLessThan(VersionedEntityConditionResult.Failed);

        (VersionedEntityConditionResult.Succeeded < VersionedEntityConditionResult.Unmodified).Should().BeTrue();
        (VersionedEntityConditionResult.Unmodified < VersionedEntityConditionResult.Failed).Should().BeTrue();
        (VersionedEntityConditionResult.Succeeded < VersionedEntityConditionResult.Failed).Should().BeTrue();

        (VersionedEntityConditionResult.Succeeded == VersionedEntityConditionResult.Succeeded).Should().BeTrue();
        (VersionedEntityConditionResult.Unmodified == VersionedEntityConditionResult.Unmodified).Should().BeTrue();
        (VersionedEntityConditionResult.Failed == VersionedEntityConditionResult.Failed).Should().BeTrue();

        (VersionedEntityConditionResult.Succeeded != VersionedEntityConditionResult.Unmodified).Should().BeTrue();
        (VersionedEntityConditionResult.Unmodified != VersionedEntityConditionResult.Failed).Should().BeTrue();
        (VersionedEntityConditionResult.Succeeded != VersionedEntityConditionResult.Failed).Should().BeTrue();

        (VersionedEntityConditionResult.Succeeded > VersionedEntityConditionResult.Unmodified).Should().BeFalse();
        (VersionedEntityConditionResult.Unmodified > VersionedEntityConditionResult.Failed).Should().BeFalse();
        (VersionedEntityConditionResult.Succeeded > VersionedEntityConditionResult.Failed).Should().BeFalse();

        (VersionedEntityConditionResult.Succeeded <= VersionedEntityConditionResult.Unmodified).Should().BeTrue();
        (VersionedEntityConditionResult.Unmodified <= VersionedEntityConditionResult.Failed).Should().BeTrue();
        (VersionedEntityConditionResult.Succeeded <= VersionedEntityConditionResult.Failed).Should().BeTrue();

        (VersionedEntityConditionResult.Succeeded >= VersionedEntityConditionResult.Unmodified).Should().BeFalse();
        (VersionedEntityConditionResult.Unmodified >= VersionedEntityConditionResult.Failed).Should().BeFalse();
        (VersionedEntityConditionResult.Succeeded >= VersionedEntityConditionResult.Failed).Should().BeFalse();

        VersionedEntityConditionResult.Max(VersionedEntityConditionResult.Succeeded, VersionedEntityConditionResult.Unmodified).Should().Be(VersionedEntityConditionResult.Unmodified);
        VersionedEntityConditionResult.Max(VersionedEntityConditionResult.Unmodified, VersionedEntityConditionResult.Failed).Should().Be(VersionedEntityConditionResult.Failed);
        VersionedEntityConditionResult.Max(VersionedEntityConditionResult.Succeeded, VersionedEntityConditionResult.Failed).Should().Be(VersionedEntityConditionResult.Failed);
    }

    [Fact]
    public void VersionedEntityCondition_Conversion()
    {
        var entity = new VersionedString("foo", 1, new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero));

        var check1 = new ModifiedSince(new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero));
        check1.Select(v => (byte)v).Validate(entity).Should().Be(VersionedEntityConditionResult.Succeeded);

        var check2 = new ModifiedSince(new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero));
        check2.Select(v => (byte)v).Validate(entity).Should().Be(VersionedEntityConditionResult.Unmodified);

        var check3 = new VersionEquals(1);
        check3.Select(v => (byte)v).Validate(entity).Should().Be(VersionedEntityConditionResult.Succeeded);

        var check4 = new VersionEquals(2);
        check4.Select(v => (byte)v).Validate(entity).Should().Be(VersionedEntityConditionResult.Failed);
    }

    private sealed record ModifiedSince(DateTimeOffset Value)
        : IVersionedEntityCondition<int>
    {
        public VersionedEntityConditionResult Validate<TEntity>(TEntity entity) where TEntity : notnull, IVersionEquatable<int>
            => entity.ModifiedSince(new(Value)) ? VersionedEntityConditionResult.Succeeded : VersionedEntityConditionResult.Unmodified;
    }

    private sealed record VersionEquals(int Version)
        : IVersionedEntityCondition<int>
    {
        public VersionedEntityConditionResult Validate<TEntity>(TEntity entity) where TEntity : notnull, IVersionEquatable<int>
            => entity.VersionEquals(Version) ? VersionedEntityConditionResult.Succeeded : VersionedEntityConditionResult.Failed;
    }

    private sealed record VersionedString(string Value, byte Version, DateTimeOffset ModifiedAt)
        : IVersionEquatable<byte>
    {
        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => ModifiedAt > other;

        public bool VersionEquals(byte other)
            => Version == other;
    }
}

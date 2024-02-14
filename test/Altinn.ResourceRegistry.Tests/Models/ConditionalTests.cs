#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using FluentAssertions;
using System;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Models;

public class ConditionalTests
{
    [Fact]
    public void CanConstruct_Succeeded()
    {
        Conditional<string, byte> test = Conditional.Succeeded("foo");

        test.IsSucceeded.Should().BeTrue();
        test.IsNotFound.Should().BeFalse();
        test.IsUnmodified.Should().BeFalse();
        test.IsConditionFailed.Should().BeFalse();

        test.Value.Should().Be("foo");
        test.Invoking(t => t.VersionTag).Should().Throw<InvalidOperationException>();
        test.Invoking(t => t.VersionModifiedAt).Should().Throw<InvalidOperationException>();
        test.Invoking(test => test.NotFoundType).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanConstruct_ConditionFailed()
    {
        Conditional<string, byte> test = Conditional.ConditionFailed();

        test.IsSucceeded.Should().BeFalse();
        test.IsNotFound.Should().BeFalse();
        test.IsUnmodified.Should().BeFalse();
        test.IsConditionFailed.Should().BeTrue();

        test.Invoking(t => t.Value).Should().Throw<InvalidOperationException>();
        test.Invoking(t => t.VersionTag).Should().Throw<InvalidOperationException>();
        test.Invoking(t => t.VersionModifiedAt).Should().Throw<InvalidOperationException>();
        test.Invoking(test => test.NotFoundType).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanConstruct_NotFound()
    {
        Conditional<string, byte> test = Conditional.NotFound("test");

        test.IsSucceeded.Should().BeFalse();
        test.IsNotFound.Should().BeTrue();
        test.IsUnmodified.Should().BeFalse();
        test.IsConditionFailed.Should().BeFalse();

        test.Invoking(t => t.Value).Should().Throw<InvalidOperationException>();
        test.Invoking(t => t.VersionTag).Should().Throw<InvalidOperationException>();
        test.Invoking(t => t.VersionModifiedAt).Should().Throw<InvalidOperationException>();
        test.NotFoundType.Should().Be("test");
    }

    [Fact]
    public void CanConstruct_Unmodified()
    {
        byte version = 2;
        var modifiedAt = DateTimeOffset.UtcNow;
        Conditional<string, byte> test = Conditional.Unmodified(version, modifiedAt);

        test.IsSucceeded.Should().BeFalse();
        test.IsNotFound.Should().BeFalse();
        test.IsUnmodified.Should().BeTrue();
        test.IsConditionFailed.Should().BeFalse();

        test.Invoking(t => t.Value).Should().Throw<InvalidOperationException>();
        test.VersionTag.Should().Be(version);
        test.VersionModifiedAt.Should().Be(modifiedAt);
        test.Invoking(test => test.NotFoundType).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(ConditionalData))]
    public void CanConvert(Conditional<string, byte> conditional)
    {
        var converted = conditional.Select(
            s => new WrappedString(s),
            v => (int)(v * 4));

        converted.IsSucceeded.Should().Be(conditional.IsSucceeded);
        converted.IsNotFound.Should().Be(conditional.IsNotFound);
        converted.IsUnmodified.Should().Be(conditional.IsUnmodified);
        converted.IsConditionFailed.Should().Be(conditional.IsConditionFailed);

        if (conditional.IsSucceeded)
        {
            converted.Value.Should().BeOfType<WrappedString>().Which.Value.Should().Be(conditional.Value);
        }
        else if (conditional.IsUnmodified)
        {
            converted.VersionTag.Should().Be(conditional.VersionTag * 4);
            converted.VersionModifiedAt.Should().Be(conditional.VersionModifiedAt);
        } 
        else if (conditional.IsNotFound)
        {
            converted.NotFoundType.Should().Be(conditional.NotFoundType);
        }
    }

    public static TheoryData<Conditional<string, byte>> ConditionalData => new()
    {
        Conditional.Succeeded("foo"),
        Conditional.ConditionFailed(),
        Conditional.NotFound("bar"),
        Conditional.Unmodified((byte)2, new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero)),
    };

    private record WrappedString(string Value);
}

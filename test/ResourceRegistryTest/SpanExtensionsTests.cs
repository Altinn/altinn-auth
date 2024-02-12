#nullable enable

using Altinn.ResourceRegistry.Extensions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class SpanExtensionsTests
{
    public static TheoryData<string, char, StringSplitOptions> SplitData
    {
        get
        {
            StringSplitOptions[] options = [StringSplitOptions.None, StringSplitOptions.RemoveEmptyEntries, StringSplitOptions.TrimEntries, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries];
            (string Input, char Separator)[] cases = [("foo,bar,baz", ','), ("foo,bar,baz", ';'), ("foo,bar,,baz,", ','), (",,", ','), ("", ','), ("foo", ',')];

            var data = new TheoryData<string, char, StringSplitOptions>();
            foreach (var (input, separator) in cases)
            {
                foreach (var option in options)
                {
                    data.Add(input, separator, option);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SplitData))]
    public void Split(string input, char separator, StringSplitOptions options)
    {
        var expected = ((IEnumerable<string>)input.Split(separator, options)).GetEnumerator();
        var actual = input.AsSpan().Split(separator, options).GetEnumerator();

        while (expected.MoveNext())
        {
            actual.MoveNext().Should().BeTrue();
            actual.Current.ToString().Should().Be(expected.Current);
        }

        actual.MoveNext().Should().BeFalse();
    }
}

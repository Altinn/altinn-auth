#nullable enable

using Altinn.ResourceRegistry.Utils;
using FluentAssertions;
using System;
using System.Buffers;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class Base64UrlEncoderTests
{
    [Theory]
    [InlineData("")]
    [InlineData("-w-w-w")]
    [InlineData("_w_w_w")]
    [InlineData("-_-_")]
    public void RoundTrips(string s)
    {
        var buff = ArrayPool<byte>.Shared.Rent(Base64UrlEncoder.GetMaxDecodedLength(s.Length));
        try
        {
            Base64UrlEncoder.TryDecode(s, buff, out var written).Should().BeTrue();
            var roundTripped = Base64UrlEncoder.Encode(buff.AsSpan(0, written));
            roundTripped.Should().Be(s);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buff);
        }
    }
}

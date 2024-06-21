#nullable enable

using Altinn.ResourceRegistry.Utils;
using System.Buffers;

namespace Altinn.ResourceRegistry.Tests;

public class Base64UrlEncoderTests
{
    public static TheoryData<string> EncodedData => new()
    {
        "",
        "-w-w-w",
        "_w_w_w",
        "-_-_",
    };

    [Theory]
    [MemberData(nameof(EncodedData))]
    public void RoundTripsStrings(string s)
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

    [Theory]
    [MemberData(nameof(EncodedData))]
    public void RoundTripsUtf8(string s)
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(s);

        var dataBuff = ArrayPool<byte>.Shared.Rent(Base64UrlEncoder.GetMaxDecodedLength(utf8.Length));
        byte[]? utf8Buff = null;
        try
        {
            Base64UrlEncoder.TryDecode(utf8, dataBuff, out var written).Should().BeTrue();

            utf8Buff = ArrayPool<byte>.Shared.Rent(Base64UrlEncoder.GetMaxEncodedLength(written));
            Base64UrlEncoder.TryEncode(dataBuff.AsSpan(0, written), utf8Buff, out var writtenUtf8).Should().BeTrue();

            utf8.AsSpan().SequenceEqual(utf8Buff.AsSpan(0, writtenUtf8)).Should().BeTrue();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(dataBuff);
            if (utf8Buff is not null)
            {
                ArrayPool<byte>.Shared.Return(utf8Buff);
            }
        }
    }
}

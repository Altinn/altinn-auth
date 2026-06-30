using System.Buffers;
using System.Buffers.Text;
using System.Text;

using Altinn.Authorization.Cli.Utils;

namespace Altinn.Authorization.Cli.Tests.Utils;

[UnitTest]
public class BufferExtensionsTest
{
    [Fact]
    public void Base64UrlEncodeToString_SingleSegment_MatchesBase64Url()
    {
        byte[] data = Encoding.UTF8.GetBytes("hello world");
        ReadOnlySequence<byte> seq = new(data);
        seq.IsSingleSegment.Should().BeTrue();

        string actual = BufferExtensions.Base64UrlEncodeToString(seq);

        actual.Should().Be(Base64Url.EncodeToString(data));
    }

    [Fact]
    public void Base64UrlEncodeToString_Empty_ReturnsEmpty()
    {
        ReadOnlySequence<byte> seq = new(Array.Empty<byte>());

        BufferExtensions.Base64UrlEncodeToString(seq).Should().BeEmpty();
    }

    [Fact]
    public void Base64UrlEncodeToString_MultiSegment_EqualsEncodingOfConcatenation()
    {
        // Two physically separate segments force the multi-segment slow path,
        // which rents a contiguous buffer and copies before encoding.
        byte[] first = Encoding.UTF8.GetBytes("the quick brown fox ");
        byte[] second = Encoding.UTF8.GetBytes("jumps over the lazy dog");
        ReadOnlySequence<byte> seq = MultiSegment(first, second);
        seq.IsSingleSegment.Should().BeFalse();

        string actual = BufferExtensions.Base64UrlEncodeToString(seq);

        byte[] concatenated = [.. first, .. second];
        actual.Should().Be(Base64Url.EncodeToString(concatenated));
    }

    private static ReadOnlySequence<byte> MultiSegment(byte[] first, byte[] second)
    {
        MemorySegment head = new(first);
        MemorySegment tail = head.Append(second);
        return new ReadOnlySequence<byte>(head, 0, tail, second.Length);
    }

    private sealed class MemorySegment : ReadOnlySequenceSegment<byte>
    {
        public MemorySegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public MemorySegment Append(ReadOnlyMemory<byte> memory)
        {
            MemorySegment next = new(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}

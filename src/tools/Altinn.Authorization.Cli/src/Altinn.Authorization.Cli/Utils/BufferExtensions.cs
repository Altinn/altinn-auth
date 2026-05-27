using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Altinn.Authorization.Cli.Utils;

[ExcludeFromCodeCoverage]
internal static class BufferExtensions
{
    /// <inheritdoc cref="Base64Url.EncodeToString(ReadOnlySpan{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Base64UrlEncodeToString(ReadOnlySequence<byte> source)
    {
        if (source.IsSingleSegment)
        {
            return Base64Url.EncodeToString(source.First.Span);
        }

        return EncodeToStringSlow(source);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string EncodeToStringSlow(ReadOnlySequence<byte> source)
        {
            var length = checked((int)source.Length);
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                source.CopyTo(buffer);
                return Base64Url.EncodeToString(buffer.AsSpan(0, length));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
    }
}

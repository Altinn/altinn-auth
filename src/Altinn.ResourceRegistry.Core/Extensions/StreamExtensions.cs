using System.Buffers;
using Nerdbank.Streams;

namespace Altinn.ResourceRegistry.Core.Extensions;

/// <summary>
/// Extension methods for working with streams.
/// </summary>
public static class StreamExtensions
{
    // Copied from System.Io.Stream
    private const int DefaultCopyBufferSize = 81920;

    /// <summary>
    /// Copy the contents of a stream into a <see cref="IBufferWriter{T}"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to copy.</param>
    /// <param name="bufferWriter">The destination <see cref="IBufferWriter{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public static Task CopyToAsync(
        this Stream stream,
        IBufferWriter<byte> bufferWriter,
        CancellationToken cancellationToken = default)
        => stream.CopyToAsync(bufferWriter, DefaultCopyBufferSize, cancellationToken);

    /// <summary>
    /// Copy the contents of a stream into a <see cref="IBufferWriter{T}"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to copy.</param>
    /// <param name="bufferWriter">The destination <see cref="IBufferWriter{T}"/>.</param>
    /// <param name="bufferSize">The buffer size to use for each copy iteration.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public static async Task CopyToAsync(
        this Stream stream, 
        IBufferWriter<byte> bufferWriter, 
        int bufferSize,
        CancellationToken cancellationToken = default) 
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead) 
        {
            throw new ArgumentException("Stream is not readable", nameof(stream));
        } 

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = bufferWriter.GetMemory(bufferSize);
            var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
            bufferWriter.Advance(read);

            if (read < buffer.Length)
            {
                // We've reached the end of the stream.
                return;
            }
        }
    }

    /// <summary>
    /// Read stream into a <see cref="Sequence{T}"/>. This will rent buffers from the shared dotnet memorypool.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to copy.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Sequence{T}"/> containing the data read from <paramref name="stream"/>.</returns>
    public static async Task<Sequence<byte>> ReadToSequenceAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var builder = new Sequence<byte>(MemoryPool<byte>.Shared);
        await stream.CopyToAsync(builder, cancellationToken);
        return builder;
    }
}

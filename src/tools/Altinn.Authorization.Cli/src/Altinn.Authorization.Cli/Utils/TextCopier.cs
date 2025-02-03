using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Utility class for efficiently copying text from a <see cref="TextReader"/> to a <see cref="TextWriter"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TextCopier
{
    private const int KIBIBYTE_CHAR_COUNT = 1024 / sizeof(char);
    private const int MIBIBYTE_CHAR_COUNT = 1024 * KIBIBYTE_CHAR_COUNT;

    /// <summary>
    /// Creates a new <see cref="TextCopier"/> which uses buffers of size <paramref name="mibibyteCount"/>.
    /// </summary>
    /// <param name="mibibyteCount">The size of the character buffers, in MiB.</param>
    /// <returns>A new <see cref="TextCopier"/>.</returns>
    public static TextCopier Create(int mibibyteCount)
    {
        return new(mibibyteCount);
    }

    private readonly ArrayPool<char> _pool = ArrayPool<char>.Shared;
    private readonly int _mibibyteCount;

    private TextCopier(int mibibyteCount)
    {
        _mibibyteCount = mibibyteCount;
    }

    /// <summary>
    /// Copy lines from <paramref name="reader"/> to <paramref name="writer"/> while reporting progress.
    /// </summary>
    /// <typeparam name="TProgress">The progress reporter type.</typeparam>
    /// <param name="reader">The <see cref="TextReader"/> to copy from.</param>
    /// <param name="writer">The <see cref="TextWriter"/> to copy to.</param>
    /// <param name="linesProgress">A <see cref="IProgress{T}"/> reporter to report lines copied to.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public Task CopyLinesAsync<TProgress>(
        TextReader reader,
        TextWriter writer,
        TProgress linesProgress,
        CancellationToken cancellationToken = default)
        where TProgress : IProgress<int>
    {
        var channel = Channel.CreateBounded<ArraySegment<char>>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true,
        });

        var readerTask = ReadAsync(reader, _pool, channel.Writer, _mibibyteCount, cancellationToken);
        var writerTask = WriteAsync(writer, _pool, channel.Reader, linesProgress, _mibibyteCount, cancellationToken);

        return Task.WhenAll(readerTask, writerTask);

        static async Task ReadAsync(TextReader reader, ArrayPool<char> pool, ChannelWriter<ArraySegment<char>> writer, int mibibyteCount, CancellationToken cancellationToken)
        {
            int read;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WaitToWriteAsync(cancellationToken);

                var buffer = pool.Rent(MIBIBYTE_CHAR_COUNT * mibibyteCount);
                read = await reader.ReadBlockAsync(buffer.AsMemory(), cancellationToken);
                if (read == 0)
                {
                    // end
                    break;
                }

                await writer.WriteAsync(new(buffer, 0, read), cancellationToken);
            }

            writer.Complete();
            if (reader is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                reader.Dispose();
            }
        }

        static async Task WriteAsync(TextWriter writer, ArrayPool<char> pool, ChannelReader<ArraySegment<char>> reader, TProgress linesProgress, int mibibyteCount, CancellationToken cancellationToken)
        {
            await foreach (var result in reader.ReadAllAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newLines = result.AsSpan().Count('\n');
                await writer.WriteAsync(result.AsMemory(), cancellationToken);
                linesProgress.Report(newLines);

                pool.Return(result.Array!);
            }

            if (writer is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                writer.Dispose();
            }
        }
    }
}

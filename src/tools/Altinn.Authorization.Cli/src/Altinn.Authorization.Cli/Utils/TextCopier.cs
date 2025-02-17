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

    private static int MiBCharBufferSize(int mibibyteCount)
        => MIBIBYTE_CHAR_COUNT * mibibyteCount;

    /// <summary>
    /// Creates a new <see cref="TextCopier"/> with a buffer of <paramref name="mibibyteCount"/> MiB.
    /// </summary>
    /// <param name="mibibyteCount">The size of the character buffers, in MiB.</param>
    /// <returns>A new <see cref="TextCopier"/>.</returns>
    public static TextCopier Create(int mibibyteCount)
        => new(ArrayPool<char>.Shared, MiBCharBufferSize(mibibyteCount));

    private readonly int _bufferSize;
    private readonly ArrayPool<char> _pool;

    private TextCopier(ArrayPool<char> pool, int bufferSize)
    {
        _pool = pool;
        _bufferSize = bufferSize;
    }

    /// <summary>
    /// Copy lines from <paramref name="reader"/> to <paramref name="writer"/> while reporting progress.
    /// </summary>
    /// <typeparam name="TProgress">The progress reporter type.</typeparam>
    /// <param name="reader">The <see cref="TextReader"/> to copy from.</param>
    /// <param name="writer">The <see cref="TextWriter"/> to copy to.</param>
    /// <param name="linesProgress">A <see cref="IProgress{T}"/> reporter to report lines copied to.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    public async Task CopyLinesAsync<TProgress>(
        TextReader reader,
        TextWriter writer,
        TProgress linesProgress,
        CancellationToken cancellationToken = default)
        where TProgress : IProgress<int>
    {
        var channel = Channel.CreateBounded<ArraySegment<char>>(new BoundedChannelOptions(4)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = true,
        });

        var readerTask = Task.Run(() => ReadLinesAsync(_pool, _bufferSize, channel.Writer, reader, cancellationToken), cancellationToken);
        var writerTask = Task.Run(() => WriteLinesTask(_pool, linesProgress, channel.Reader, writer, cancellationToken), cancellationToken);
        await Task.WhenAll(readerTask, writerTask);

        async static Task ReadLinesAsync(ArrayPool<char> pool, int bufferSize, ChannelWriter<ArraySegment<char>> writer, TextReader reader, CancellationToken cancellationToken)
        {
            try
            {
                while (await writer.WaitToWriteAsync(cancellationToken))
                {
                    var buffer = pool.Rent(bufferSize);
                    try
                    {
                        var count = await reader.ReadBlockAsync(buffer, cancellationToken);
                        if (count == 0)
                        {
                            break;
                        }

                        await writer.WriteAsync(new ArraySegment<char>(buffer, 0, count), cancellationToken);
                        buffer = null;
                    }
                    finally
                    {
                        if (buffer is not null)
                        {
                            pool.Return(buffer);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                writer.TryComplete(e);
                throw;
            }
            finally
            {
                writer.TryComplete();
            }
        }

        async Task WriteLinesTask(ArrayPool<char> pool, TProgress linesProgress, ChannelReader<ArraySegment<char>> reader, TextWriter writer, CancellationToken cancellationToken)
        {
            await foreach (var buffer in reader.ReadAllAsync(cancellationToken))
            {
                await writer.WriteAsync(buffer, cancellationToken);
                var lineCount = buffer.AsSpan().Count('\n');
                pool.Return(buffer.Array!);
                linesProgress.Report(lineCount);
            }

            await writer.FlushAsync(cancellationToken);
        }
    }
}

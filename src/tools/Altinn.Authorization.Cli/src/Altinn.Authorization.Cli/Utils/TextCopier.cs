using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Utility class for efficiently copying text from a <see cref="TextReader"/> to a <see cref="TextWriter"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TextCopier
{
    /// <summary>
    /// Creates a new <see cref="TextCopier"/> with a buffer of <paramref name="mibibyteCount"/> MiB.
    /// </summary>
    /// <param name="mibibyteCount">The size of the character buffer, in MiB.</param>
    /// <returns>A new <see cref="TextCopier"/>.</returns>
    public static TextCopier Create(int mibibyteCount)
        => new(CharBuffers.MiB(mibibyteCount));

    private readonly Memory<char> _buffer;

    private TextCopier(Memory<char> buffer)
    {
        _buffer = buffer;
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
        // TODO: this can probably be sped up quite a bit by reading and writing concurrently
        int read;
        while (true)
        {
            read = await reader.ReadAsync(_buffer, cancellationToken);
            if (read == 0)
            {
                // end
                break;
            }

            var data = _buffer[..read];
            var newLines = data.Span.Count('\n');
            linesProgress.Report(newLines);
            await writer.WriteAsync(data, cancellationToken);
        }
    }
}

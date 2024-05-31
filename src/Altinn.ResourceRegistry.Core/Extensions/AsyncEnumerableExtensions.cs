using System.Collections.Immutable;
using System.Threading.Channels;

namespace Altinn.ResourceRegistry.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Merges the specified <see cref="IAsyncEnumerable{T}"/> instances into a single <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements of the <paramref name="sources"/>.</typeparam>
    /// <param name="sources">The <see cref="IAsyncEnumerable{T}"/>s to merge.</param>
    /// <returns>A merged <see cref="IAsyncEnumerable{T}"/>.</returns>
    public static IAsyncEnumerable<T> Merge<T>(ReadOnlySpan<IAsyncEnumerable<T>> sources)
        => sources switch
        {
            [] => AsyncEnumerable.Empty<T>(),
            [var first] => first,
            [MergedAsyncEnumerable<T> first, .. var rest] => first.MergeWith(rest),
            _ => MergedAsyncEnumerable<T>.Create(sources),
        };

    /// <summary>
    /// Merges the specified <see cref="IAsyncEnumerable{T}"/> instances into a single <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequences.</typeparam>
    /// <param name="self">The first enumerable.</param>
    /// <param name="rest">The remaining enumerables.</param>
    /// <returns>The merged sequence.</returns>
    public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> self, ReadOnlySpan<IAsyncEnumerable<T>> rest)
        => Merge([self, .. rest]);

    /// <summary>
    /// Merges the specified <see cref="IAsyncEnumerable{T}"/> instances into a single <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequences.</typeparam>
    /// <param name="self">The first enumerable.</param>
    /// <param name="other">The second enumerable.</param>
    /// <returns>The merged sequence.</returns>
    public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> self, IAsyncEnumerable<T> other)
        => Merge([self, other]);

    /// <summary>
    /// Writes the elements of the <see cref="IAsyncEnumerable{T}"/> to the specified <see cref="ChannelWriter{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the <paramref name="source"/>.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="writer">The channel writer.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task WriteToAsync<T>(this IAsyncEnumerable<T> source, ChannelWriter<T> writer, CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (!await writer.WaitToWriteAsync(cancellationToken))
            {
                // Can't write more items, the channel is closed.
                return;
            }

            await writer.WriteAsync(item, cancellationToken);
        }
    }

    private sealed class MergedAsyncEnumerable<T>
        : IAsyncEnumerable<T>
    {
        private readonly ImmutableArray<IAsyncEnumerable<T>> _sources;

        private MergedAsyncEnumerable(ImmutableArray<IAsyncEnumerable<T>> sources)
        {
            _sources = sources;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(1)
            {
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });

            var writer = channel.Writer;
            var tasks = new Task[_sources.Length];
            for (var i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                tasks[i] = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await source.WriteToAsync(writer, cancellationToken);
                        }
                        catch (Exception e)
                        {
                            writer.TryComplete(e);
                        }
                    },
                    cancellationToken);
            }

            _ = Task.Run(
                async () =>
                {
                    await Task.WhenAll(tasks);
                    writer.TryComplete();
                },
                cancellationToken);

            return channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        public MergedAsyncEnumerable<T> MergeWith(ReadOnlySpan<IAsyncEnumerable<T>> sources)
        {
            return MergedAsyncEnumerable<T>.Create([.. _sources, .. sources]);
        }

        public static MergedAsyncEnumerable<T> Create(ReadOnlySpan<IAsyncEnumerable<T>> sources)
        {
            var length = sources.Length;
            foreach (var source in sources)
            {
                if (source is null)
                {
                    throw new ArgumentNullException(nameof(sources));
                }

                if (source is MergedAsyncEnumerable<T> merged)
                {
                    length += merged._sources.Length - 1;
                }
            }

            var builder = ImmutableArray.CreateBuilder<IAsyncEnumerable<T>>(length);
            foreach (var source in sources)
            {
                if (source is MergedAsyncEnumerable<T> merged)
                {
                    builder.AddRange(merged._sources);
                }
                else
                {
                    builder.Add(source);
                }
            }

            return new MergedAsyncEnumerable<T>(builder.DrainToImmutable());
        }
    }
}

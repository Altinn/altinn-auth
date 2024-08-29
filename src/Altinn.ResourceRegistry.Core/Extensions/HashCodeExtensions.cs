#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Altinn.ResourceRegistry.Extensions;

/// <summary>
/// Extensions for <see cref="HashCode"/>.
/// </summary>
public static class HashCodeExtensions
{
    /// <summary>
    /// Adds a sequence of items to the hash code.
    /// </summary>
    /// <remarks>
    /// The order matters.
    /// </remarks>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="hashCode">The <see cref="HashCode"/> to add items to.</param>
    /// <param name="items">A <see cref="ReadOnlySpan{T}"/> of items.</param>
    /// <param name="equalityComparer">An optional <see cref="IEqualityComparer{T}"/>.</param>
    public static void AddSequence<T>(this ref HashCode hashCode, ReadOnlySpan<T> items, IEqualityComparer<T>? equalityComparer = null)
    {
        hashCode.Add(items.Length);

        foreach (var item in items)
        {
            hashCode.Add(item, equalityComparer);
        }
    }

    /// <summary>
    /// Adds a set of items to the hash code.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="hashCode">The <see cref="HashCode"/> to add items to.</param>
    /// <param name="items">The items to add.</param>
    /// <param name="comparer">
    /// Optional <see cref="IComparer{T}"/> for the items, used to sort the items prior to adding to the hash.
    /// Defaults to <see cref="Comparer{T}.Default"/>.
    /// </param>
    /// <param name="equalityComparer">
    /// Optional <see cref="IEqualityComparer{T}"/> for the items, used to generate the hash code for the items to add.
    /// Defaults to <see langword="null"/> (which means calling <see cref="object.GetHashCode()"/> on the item itself).
    /// </param>
    public static void AddSet<T>(this ref HashCode hashCode, IReadOnlyCollection<T> items, IComparer<T>? comparer = null, IEqualityComparer<T>? equalityComparer = null)
    {
        var scratch = ArrayPool<T>.Shared.Rent(items.Count);

        try
        {
            var index = 0;
            foreach (var item in items)
            {
                scratch[index++] = item;
            }

            Debug.Assert(index == items.Count);
            var span = scratch.AsSpan(0, index);
            span.Sort(comparer);
            hashCode.Add(span.Length);
            foreach (var item in span)
            {
                hashCode.Add(item, equalityComparer);
            }
        }
        finally
        {
            ArrayPool<T>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Adds a dictionary to the hash code.
    /// </summary>
    /// <typeparam name="TKey">They dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <param name="hashCode">The <see cref="HashCode"/> to add items to.</param>
    /// <param name="dictionary">The dictionary to add.</param>
    /// <param name="keyComparer">
    /// Optional <see cref="IComparer{T}"/> for the dictionary keys, used to sort the items prior to adding to the hash.
    /// Defaults to <see cref="Comparer{T}.Default"/>.
    /// </param>
    /// <param name="keyEqualityComparer">
    /// Optional <see cref="IEqualityComparer{T}"/> for the dictionary keys, used to generate the hash code for the keys to add.
    /// Defaults to <see langword="null"/> (which means calling <see cref="object.GetHashCode()"/> on the key itself).
    /// </param>
    /// <param name="valueEqualityComparer">
    /// Optional <see cref="IEqualityComparer{T}"/> for the dictionary values, used to generate the hash code for the values to add.
    /// Defaults to <see langword="null"/> (which means calling <see cref="object.GetHashCode()"/> on the value itself).
    /// </param>
    public static void AddDictionary<TKey, TValue>(
        this ref HashCode hashCode, 
        IReadOnlyCollection<KeyValuePair<TKey, TValue>> dictionary,
        IComparer<TKey>? keyComparer = null,
        IEqualityComparer<TKey>? keyEqualityComparer = null,
        IEqualityComparer<TValue>? valueEqualityComparer = null)
    {
        var scratch = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(dictionary.Count);

        try
        {
            var index = 0;
            foreach (var kvp in dictionary)
            {
                scratch[index++] = kvp;
            }

            Debug.Assert(index == dictionary.Count);
            var span = scratch.AsSpan(0, index);
            span.Sort(new KeyComparer<TKey, TValue>(keyComparer));

            hashCode.Add(span.Length);
            foreach (var kvp in span)
            {
                hashCode.Add(kvp.Key, keyEqualityComparer);
                hashCode.Add(kvp.Value, valueEqualityComparer);
            }
        }
        finally
        {
            ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(scratch);
        }
    }

    private struct KeyComparer<TKey, TValue>
        : IComparer<KeyValuePair<TKey, TValue>>
    {
        private readonly IComparer<TKey> _comparer;

        public KeyComparer(IComparer<TKey>? keyComparer)
        {
            _comparer = keyComparer ?? Comparer<TKey>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            => _comparer.Compare(x.Key, y.Key);
    }
}

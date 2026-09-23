#nullable enable

using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Extensions;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/>.
/// </summary>
public static class AltinnEnumerableExtensions
{
    /// <summary>
    /// Returns whether the enumerable contains any of the specified items.
    /// </summary>
    /// <typeparam name="T">The type of enumerable.</typeparam>
    /// <param name="enumerable">The enumerable to check if contains items.</param>
    /// <param name="items">The items to check for.</param>
    /// <param name="comparer">Optional equality comparer.</param>
    /// <returns><see langword="true"/> if <paramref name="enumerable"/> contains any items in <paramref name="items"/>, otherwise <see langword="false"/>.</returns>
    public static bool ContainsAnyOf<T>(
        this IEnumerable<T> enumerable, 
        ReadOnlySpan<T> items,
        EqualityComparer<T>? comparer = null)
    {
        Guard.IsNotNull(enumerable);
        comparer ??= EqualityComparer<T>.Default;

        if (items.IsEmpty)
        {
            return false;
        }

        foreach (var item in enumerable)
        {
            foreach (var matchAgainst in items)
            {
                if (comparer.Equals(item, matchAgainst))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

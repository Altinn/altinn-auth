namespace Altinn.ResourceRegistry.Extensions;

/// <summary>
/// Extension methods for <see cref="IList{T}"/>
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="List{T}"/>
    /// by swapping the last element with the element to remove and then removing the last element.
    /// </summary>
    /// <remarks>
    /// This modifies the order of the elements in the list.
    /// </remarks>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="list">The list to remove an item from.</param>
    /// <param name="item">The item to remove.</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was found in the list and removed, otherwise <see langword="false"/>.</returns>
    public static bool SwapRemove<T>(this IList<T> list, T item)
    {
        ArgumentNullException.ThrowIfNull(list);

        var index = list.IndexOf(item);
        if (index >= 0)
        {
            list.SwapRemoveAt(index);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the element at the specified index by swapping the last element with the element to remove and then removing the last element.
    /// </summary>
    /// <remarks>
    /// This modifies the order of the elements in the list.
    /// </remarks>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    /// <param name="list">The list to remove an item from.</param>
    /// <param name="index">The index of the item to remove.</param>
    public static void SwapRemoveAt<T>(this IList<T> list, int index)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, list.Count);

        list[index] = list[^1];
        list.RemoveAt(list.Count - 1);
    }
}

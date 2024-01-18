#nullable enable

using System.Text.Json;

namespace Altinn.ResourceRegistry.JsonPatch;

/// <summary>
/// Extensions for simplified usage of <see cref="JsonElement"/>.
/// </summary>
internal static class JsonExtensions
{
    /// <summary>
    ///   Get a JsonElement which can be safely stored beyond the lifetime of the
    ///   original <see cref="JsonDocument"/>.
    /// </summary>
    /// <returns>
    ///   A JsonElement which can be safely stored beyond the lifetime of the
    ///   original <see cref="JsonDocument"/>.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     If this JsonElement is itself the output of a previous call to Clone, or
    ///     a value contained within another JsonElement which was the output of a previous
    ///     call to Clone, this method results in no additional memory allocation.
    ///   </para>
    /// </remarks>
    public static JsonElement? Clone(this JsonElement? jsonElement)
    {
        if (jsonElement is null)
        {
            return null;
        }

        return jsonElement.Value.Clone();
    }

    /// <summary>
    /// Checks if two <see cref="JsonElement"/>s are equivalent.
    /// </summary>
    /// <param name="self">The element to compare</param>
    /// <param name="other">The element to compare against</param>
    /// <returns><see langword="true"/> if the two elements are equivalent, otherwise <see langword="false"/>.</returns>
    public static bool IsEquivalentTo(this JsonElement? self, JsonElement? other)
    {
        return JsonEquivalenceComparer.Instance.Equals(self, other);
    }

    /// <summary>
    /// Checks if two <see cref="JsonElement"/>s are equivalent.
    /// </summary>
    /// <param name="self">The element to compare</param>
    /// <param name="other">The element to compare against</param>
    /// <returns><see langword="true"/> if the two elements are equivalent, otherwise <see langword="false"/>.</returns>
    public static bool IsEquivalentTo(this JsonElement self, JsonElement other)
    {
        return JsonEquivalenceComparer.Instance.Equals(self, other);
    }

    /// <summary>
    /// Checks if two <see cref="JsonDocument"/>s are equivalent.
    /// </summary>
    /// <param name="self">The element to compare</param>
    /// <param name="other">The element to compare against</param>
    /// <returns><see langword="true"/> if the two documents are equivalent, otherwise <see langword="false"/>.</returns>
    public static bool IsEquivalentTo(this JsonDocument self, JsonDocument other)
    {
        return JsonEquivalenceComparer.Instance.Equals(self, other);
    }

    /// <summary>
    /// Gets a stable hash code for a <see cref="JsonElement"/> that does not care about object keys ordering.
    /// </summary>
    /// <param name="self">The element to get a hash code for</param>
    /// <returns>A hash code.</returns>
    public static int GetStableHashCode(this JsonElement? self)
    {
        return self switch 
        { 
            null => 0,
            { } value => JsonEquivalenceComparer.Instance.GetHashCode(value),
        };
    }

    /// <summary>
    /// Gets a stable hash code for a <see cref="JsonElement"/> that does not care about object keys ordering.
    /// </summary>
    /// <param name="self">The element to get a hash code for</param>
    /// <returns>A hash code.</returns>
    public static int GetStableHashCode(this JsonElement self)
    {
        return JsonEquivalenceComparer.Instance.GetHashCode(self);
    }

    /// <summary>
    /// Gets a stable hash code for a <see cref="JsonDocument"/> that does not care about object keys ordering.
    /// </summary>
    /// <param name="self">The document to get a hash code for</param>
    /// <returns>A hash code.</returns>
    public static int GetStableHashCode(this JsonDocument self)
    {
        return JsonEquivalenceComparer.Instance.GetHashCode(self);
    }
}

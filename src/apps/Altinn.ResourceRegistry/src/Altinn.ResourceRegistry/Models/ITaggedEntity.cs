#nullable enable

using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// A ETag enabled entity.
/// </summary>
/// <typeparam name="T">The etag type.</typeparam>
public interface ITaggedEntity<T>
{
    /// <summary>
    /// Gets the header values.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="modifiedAt">When the entity was last modified.</param>
    /// <remarks>This method is explicitly not properties so that they don't get serialized as JSON by default.</remarks>
    void GetHeaderValues(out T version, out HttpDateTimeHeaderValue modifiedAt);
}

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Auth;

/// <summary>
/// Interface used to configure resource owner lookup.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
public interface IResourceOwnerProvider<in T>
{
    /// <summary>
    /// Try to get the resource owner for the given resource.
    /// </summary>
    /// <param name="resource">The resource</param>
    /// <param name="resourceOwner">Output resource owner.</param>
    /// <returns><see langword="true"/> if a resource owner was found, otherwise <see langword="false"/></returns>
    bool TryGetResourceOwner(T resource, [NotNullWhen(true)] out string? resourceOwner);
}

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Auth;

/// <summary>
/// Provides a <see cref="IResourceOwnerProvider{T}"/> for <see cref="HttpContext"/> and <see cref="HttpRequest"/>
/// that tries to extract the resource owner from a named route value.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ResourceOwnerFromRouteValueAttribute
    : Attribute
    , IResourceOwnerProvider<HttpContext>
    , IResourceOwnerProvider<HttpRequest>
{
    private readonly string _routeValueName;
    private readonly string _path;

    /// <summary>
    /// Creates a new instance of <see cref="ResourceOwnerFromRouteValueAttribute"/>.
    /// </summary>
    /// <param name="routeValueName">The route value name for which to extract the resource owner</param>
    public ResourceOwnerFromRouteValueAttribute(string routeValueName)
    {
        _routeValueName = routeValueName;
        _path = $"/$PATH/{routeValueName}";
    }

    /// <inheritdoc/>
    public string Path => _path;

    /// <inheritdoc/>
    public bool TryGetResourceOwner(HttpContext resource, [NotNullWhen(true)] out string? resourceOwner)
    {
        return TryGetResourceOwner(resource.Request, out resourceOwner);
    }

    /// <inheritdoc/>
    public bool TryGetResourceOwner(HttpRequest resource, [NotNullWhen(true)] out string? resourceOwner)
    {
        if (resource.RouteValues.TryGetValue(_routeValueName, out var value) && value is string v)
        {
            resourceOwner = v;
            return true;
        }

        resourceOwner = null;
        return false;
    }

    /// <inheritdoc/>
    public void SetResourceOwner(HttpContext resource, string resourceOwner)
    {
        SetResourceOwner(resource.Request, resourceOwner);
    }

    /// <inheritdoc/>
    public void SetResourceOwner(HttpRequest resource, string resourceOwner)
    {
        resource.RouteValues[_routeValueName] = resourceOwner;
    }
}

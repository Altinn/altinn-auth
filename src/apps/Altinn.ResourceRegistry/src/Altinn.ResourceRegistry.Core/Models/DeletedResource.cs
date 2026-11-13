namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// A resource that has been deleted from the resource registry
/// </summary>
/// <param name="Resource">The latest version of the deleted resource</param>
/// <param name="DeletedAt">When the resource was deleted</param>
public record DeletedResource(ServiceResource Resource, DateTimeOffset DeletedAt);

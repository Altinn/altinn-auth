namespace Altinn.Authorization.Api.Contracts.ResourceRegistry.Enums;

/// <summary>
/// Access list mode of a resource in the resource registry.
/// </summary>
/// <remarks>
/// This file is the single definition shared by the resource registry and access management.
/// The resource registry compiles it into its own assembly as a linked file, so a new value added
/// here reaches both services in the same change. Both serialize the value by name, and access
/// management stores it by name in its database, so values must never be renamed.
/// </remarks>
public enum ResourceAccessListMode
{
    /// <summary>
    /// Access lists are not used for the resource.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Access to the resource is limited to the parties on its access lists.
    /// </summary>
    Enabled = 1,
}
